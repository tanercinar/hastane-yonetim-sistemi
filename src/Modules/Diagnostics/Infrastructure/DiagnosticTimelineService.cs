using System.Globalization;
using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class DiagnosticTimelineService : IDiagnosticTimelineService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public DiagnosticTimelineService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IDiagnosticsAccessContext accessContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DiagnosticOrderOperationResult<PatientDiagnosticTimelineDto>> GetPatientTimelineAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return DiagnosticOrderOperationResult.Validation<PatientDiagnosticTimelineDto>("PatientId", "Geçerli bir hasta kimliği belirtilmelidir.");
        }

        if (IsPatient(actor)
            || !await _accessContext.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                false,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PatientDiagnosticTimelineDto>(
                "Klinik zaman çizelgesi için gerekli izin, kaynak kapsamı veya bakım ilişkisi bulunmamaktadır.");
        }

        var entries = new List<TimelineEntryDto>();

        // 1. Lab Results
        var labResults = await _dbContext.LabResults
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.PatientId == patientId)
            .ToListAsync(cancellationToken);

        foreach (var lr in labResults)
        {
            var hasCritical = lr.Items.Any(i => i.Flag == LabResultInterpretation.CriticalLow || i.Flag == LabResultInterpretation.CriticalHigh);
            entries.Add(new TimelineEntryDto(
                lr.Id,
                "Laboratory",
                string.IsNullOrWhiteSpace(lr.CatalogItemName) ? "Laboratuvar Tetkik Sonucu" : lr.CatalogItemName,
                lr.Status.ToString(),
                lr.ClinicallyApprovedAtUtc ?? lr.TechnicallyApprovedAtUtc ?? lr.CreatedAtUtc,
                "Biyokimya / Mikrobiyoloji Laboratuvarı",
                hasCritical,
                lr.ClinicalNotes,
                lr.DiagnosticOrderId,
                lr.Items.Select(i => new TimelineParameterDto(
                    i.ParameterCode,
                    i.NumericValue.HasValue ? i.NumericValue.Value.ToString("F2", CultureInfo.InvariantCulture) : (i.StringValue ?? "-"),
                    i.Unit ?? "-",
                    $"{i.ReferenceRangeLow?.ToString("F1", CultureInfo.InvariantCulture) ?? "?"} - {i.ReferenceRangeHigh?.ToString("F1", CultureInfo.InvariantCulture) ?? "?"}",
                    i.Flag.ToString(),
                    i.Flag == LabResultInterpretation.CriticalLow || i.Flag == LabResultInterpretation.CriticalHigh)).ToList()));
        }

        // 2. Radiology Studies
        var radStudies = await _dbContext.RadiologyStudies
            .AsNoTracking()
            .Where(s => s.PatientId == patientId)
            .ToListAsync(cancellationToken);

        foreach (var rs in radStudies)
        {
            entries.Add(new TimelineEntryDto(
                rs.Id,
                "Radiology",
                $"{rs.ProcedureName} ({rs.Modality})",
                rs.Status.ToString(),
                rs.ReportFinalizedAtUtc ?? rs.PerformedAtUtc ?? rs.CreatedAtUtc,
                $"Radyoloji ({rs.Modality})",
                false,
                rs.ReportText ?? rs.Impression ?? rs.TechnicianNotes ?? "Görüntüleme istemi aşamasında.",
                rs.DiagnosticOrderId,
                null));
        }

        // 3. Pathology Cases
        var pathCases = await _dbContext.PathologyCases
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .ToListAsync(cancellationToken);

        foreach (var pc in pathCases)
        {
            entries.Add(new TimelineEntryDto(
                pc.Id,
                "Pathology",
                $"Patoloji İncelemesi: {pc.AnatomicSite} ({pc.SpecimenType})",
                pc.Status.ToString(),
                pc.ReportFinalizedAtUtc ?? pc.ReceivedAtUtc ?? pc.CreatedAtUtc,
                "Patoloji Laboratuvarı",
                false,
                pc.PathologicalDiagnosis ?? pc.GrossDescription ?? "Materyal incelemede.",
                pc.DiagnosticOrderId,
                null));
        }

        // 4. Blood Bank Crossmatches
        var crossmatches = await _dbContext.CrossmatchRequests
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .ToListAsync(cancellationToken);

        foreach (var cm in crossmatches)
        {
            entries.Add(new TimelineEntryDto(
                cm.Id,
                "BloodBank",
                $"Kan Bankası: {cm.RequestedProductType} ({cm.UnitsRequested} Ünite)",
                cm.Status.ToString(),
                cm.TestedAtUtc ?? cm.CreatedAtUtc,
                "Kan Merkezi & İmmünohematoloji",
                cm.CompatibilityResult == BloodCompatibilityStatus.Incompatible,
                $"Hasta Grubu: {cm.PatientBloodGroup} | Sonuç: {cm.CompatibilityResult}",
                cm.DiagnosticOrderId,
                null));
        }

        var sortedEntries = entries.OrderByDescending(e => e.EventDateUtc).ToList();
        var result = new PatientDiagnosticTimelineDto(patientId, sortedEntries.Count, sortedEntries);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await PublishAuditAsync(
            actor,
            "Diagnostics.TimelineDoctorView",
            "PatientDiagnosticTimeline",
            patientId.ToString(),
            $"Hasta klinik tanısal zaman çizelgesi görüntülendi. Toplam kayıt: {sortedEntries.Count}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(result);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<PatientPortalResultSummaryDto>>> GetPatientPortalResultsAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var patientId = GetActorPatientId(actor);
        if (!IsPatient(actor)
            || patientId == Guid.Empty
            || !actor.HasClaim(
                HospitalClaimTypes.Permission,
                HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<PatientPortalResultSummaryDto>>("Hasta kimliği bulunamadı.");
        }

        var results = new List<PatientPortalResultSummaryDto>();

        // 1. Lab Results: ONLY FinalApproved or Corrected (No Drafts / TechnicallyApproved to patients!)
        var approvedLabResults = await _dbContext.LabResults
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.PatientId == patientId &&
                       (r.Status == LabResultStatus.FinalApproved || r.Status == LabResultStatus.Corrected))
            .ToListAsync(cancellationToken);

        // Check if any critical notifications are unacknowledged (Deferred release rule)
        var unacknowledgedNotifications = await _dbContext.CriticalResultNotifications
            .AsNoTracking()
            .Where(n => n.PatientId == patientId && n.AcknowledgedAtUtc == null)
            .Select(n => n.DiagnosticOrderId)
            .ToListAsync(cancellationToken);

        foreach (var lr in approvedLabResults)
        {
            var isPendingDoctorReview = unacknowledgedNotifications.Contains(lr.DiagnosticOrderId);

            results.Add(new PatientPortalResultSummaryDto(
                lr.Id,
                "Laboratory",
                string.IsNullOrWhiteSpace(lr.CatalogItemName) ? "Laboratuvar Tetkik Raporu" : lr.CatalogItemName,
                lr.Status.ToString(),
                lr.ClinicallyApprovedAtUtc ?? lr.CreatedAtUtc,
                "Klinik Biyokimya Laboratuvarı",
                isPendingDoctorReview,
                isPendingDoctorReview ? "Kritik değer hekim değerlendirmesi aşamasındadır." : lr.ClinicalNotes,
                isPendingDoctorReview
                    ? null
                    : lr.Items.Select(i => new TimelineParameterDto(
                        i.ParameterCode,
                        i.NumericValue.HasValue ? i.NumericValue.Value.ToString("F2", CultureInfo.InvariantCulture) : (i.StringValue ?? "-"),
                        i.Unit ?? "-",
                        $"{i.ReferenceRangeLow?.ToString("F1", CultureInfo.InvariantCulture) ?? "?"} - {i.ReferenceRangeHigh?.ToString("F1", CultureInfo.InvariantCulture) ?? "?"}",
                        i.Flag.ToString(),
                        i.Flag == LabResultInterpretation.CriticalLow || i.Flag == LabResultInterpretation.CriticalHigh)).ToList()));
        }

        // 2. Radiology Studies: ONLY ReportFinalized or AddendumAdded (No Scheduled/Acquired/Draft to patients!)
        var approvedRadStudies = await _dbContext.RadiologyStudies
            .AsNoTracking()
            .Where(s => s.PatientId == patientId &&
                       (s.Status == RadiologyStudyStatus.ReportFinalized || s.Status == RadiologyStudyStatus.AddendumAdded))
            .ToListAsync(cancellationToken);

        foreach (var rs in approvedRadStudies)
        {
            results.Add(new PatientPortalResultSummaryDto(
                rs.Id,
                "Radiology",
                rs.ProcedureName,
                rs.Status.ToString(),
                rs.ReportFinalizedAtUtc ?? rs.CreatedAtUtc,
                $"Radyoloji Uzmanı ({rs.Modality})",
                false,
                rs.ReportText ?? rs.Impression,
                null));
        }

        // 3. Pathology Cases: ONLY ReportFinalized or Corrected (No Draft/Gross/Microscopic to patients!)
        var approvedPathCases = await _dbContext.PathologyCases
            .AsNoTracking()
            .Where(p => p.PatientId == patientId &&
                       (p.Status == PathologyCaseStatus.ReportFinalized || p.Status == PathologyCaseStatus.Corrected))
            .ToListAsync(cancellationToken);

        foreach (var pc in approvedPathCases)
        {
            results.Add(new PatientPortalResultSummaryDto(
                pc.Id,
                "Pathology",
                $"Patoloji Raporu: {pc.AnatomicSite}",
                pc.Status.ToString(),
                pc.ReportFinalizedAtUtc ?? pc.CreatedAtUtc,
                "Tıbbi Patoloji Uzmanı",
                false,
                pc.PathologicalDiagnosis,
                null));
        }

        var sortedResults = results.OrderByDescending(r => r.ResultDateUtc).ToList();

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await PublishAuditAsync(
            actor,
            "Diagnostics.TimelinePatientPortalView",
            "PatientPortalResults",
            patientId.ToString(),
            $"Hasta portalında onaylanmış tanısal sonuçlar listelendi. Toplam sonuç: {sortedResults.Count}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<PatientPortalResultSummaryDto>>(sortedResults);
    }

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient) || actor.IsInRole("Patient");

    private static Guid GetActorPatientId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value
            ?? actor.FindFirst("PatientId")?.Value
            ?? actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var pid) ? pid : Guid.Empty;
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceType,
        string targetResourceId,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var actorUserId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var actorPersonId = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value;

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: Guid.TryParse(actorUserId, out var uid) ? uid : null,
            ActorPersonId: Guid.TryParse(actorPersonId, out var personId) ? personId : null,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: targetResourceType,
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: null,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
