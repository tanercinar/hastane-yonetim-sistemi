using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class PathologyService : IPathologyService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public PathologyService(
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

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> EnsureCaseForOrderItemAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        PathologySpecimenType specimenType,
        string anatomicSite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Tanısal istem kaydı bulunamadı.");
        }

        if (order.OrderType != DiagnosticOrderType.Pathology)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>(
                "orderId",
                "Patoloji vakası yalnızca patoloji isteminden oluşturulabilir.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.LaboratoryWorklistView, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var existing = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.DiagnosticOrderId == orderId && c.DiagnosticOrderItemId == orderItemId, cancellationToken);
        if (existing != null)
        {
            return DiagnosticOrderOperationResult.Success(MapToDetailDto(existing));
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("İstem kalemi bulunamadı.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var pathologyNumber = GeneratePathologyNumber(nowUtc);

        var pathCase = PathologyCase.Create(
            Guid.NewGuid(),
            orderId,
            orderItemId,
            order.PatientId,
            pathologyNumber,
            specimenType,
            string.IsNullOrWhiteSpace(anatomicSite) ? "Biyopsi / Doku Materyali" : anatomicSite.Trim(),
            order.ClinicalIndication,
            nowUtc);

        _dbContext.PathologyCases.Add(pathCase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyCaseCreate",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Patoloji vaka kaydı oluşturuldu: {pathCase.PathologyNumber} - {pathCase.AnatomicSite}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> ReceiveSpecimenAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string fixativeUsed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(fixativeUsed))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("FixativeUsed", "Tespit solüsyonu / fiksatif bilgisi zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratorySpecimenTransition, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.ReceiveSpecimen(actorUserId, fixativeUsed, nowUtc);

            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == pathCase.DiagnosticOrderId, cancellationToken);

            var orderItem = order?.Items.FirstOrDefault(i => i.Id == pathCase.DiagnosticOrderItemId);
            if (orderItem != null && orderItem.Status == DiagnosticOrderItemStatus.Pending)
            {
                orderItem.UpdateStatus(DiagnosticOrderItemStatus.SampleReceived, nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologySpecimenReceive",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Patoloji doku materyali laboratuvara kabul edildi: {pathCase.PathologyNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> RecordGrossExamAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string grossDescription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(grossDescription))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("GrossDescription", "Makroskopi inceleme bulguları zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryResultEditDraft, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.RecordGrossExam(actorUserId, grossDescription, nowUtc);

            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == pathCase.DiagnosticOrderId, cancellationToken);

            var orderItem = order?.Items.FirstOrDefault(i => i.Id == pathCase.DiagnosticOrderItemId);
            if (orderItem != null && orderItem.Status != DiagnosticOrderItemStatus.InAnalysis && orderItem.Status != DiagnosticOrderItemStatus.Reported)
            {
                orderItem.UpdateStatus(DiagnosticOrderItemStatus.InAnalysis, nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyGrossExamRecord",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Makroskopi bulguları kaydedildi: {pathCase.PathologyNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> RecordMicroscopicExamAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string microscopicDescription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(microscopicDescription))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("MicroscopicDescription", "Mikroskopi inceleme bulguları zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryResultEditDraft, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.RecordMicroscopicExam(actorUserId, microscopicDescription, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyMicroscopicExamRecord",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Mikroskopi bulguları kaydedildi: {pathCase.PathologyNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> DraftReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string pathologicalDiagnosis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(pathologicalDiagnosis))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("PathologicalDiagnosis", "Patolojik tanı metni zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryResultEditDraft, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.DraftReport(pathologicalDiagnosis, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyReportDraft",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Patoloji rapor taslağı kaydedildi: {pathCase.PathologyNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> FinalizeReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string pathologicalDiagnosis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(pathologicalDiagnosis))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("PathologicalDiagnosis", "Patolojik tanı metni zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.FinalizeReport(actorUserId, pathologicalDiagnosis, nowUtc);

            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == pathCase.DiagnosticOrderId, cancellationToken);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == pathCase.DiagnosticOrderItemId);
                if (orderItem != null && orderItem.Status != DiagnosticOrderItemStatus.Reported)
                {
                    orderItem.UpdateStatus(DiagnosticOrderItemStatus.Reported, nowUtc);
                }

                if (order.Items.All(i => i.Status == DiagnosticOrderItemStatus.Reported || i.Status == DiagnosticOrderItemStatus.Cancelled))
                {
                    order.Complete(nowUtc);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyReportFinalize",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Patoloji raporu uzman patolog tarafından kesinleştirildi: {pathCase.PathologyNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> CorrectReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string correctionReason,
        string newDiagnosis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(correctionReason))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("CorrectionReason", "Düzeltme gerekçesi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(newDiagnosis))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("NewDiagnosis", "Düzeltilmiş tanı metni zorunludur.");
        }

        var original = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (original is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, original, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var corrected = PathologyCase.CreateCorrected(
                Guid.NewGuid(),
                original,
                correctionReason,
                newDiagnosis,
                actorUserId,
                nowUtc);

            _dbContext.PathologyCases.Add(corrected);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Diagnostics.PathologyReportCorrect",
                "PathologyCase",
                corrected.Id.ToString(),
                $"Kesinleşmiş patoloji raporu gerekçeyle düzeltildi: {corrected.PathologyNumber}. Gerekçe: {correctionReason}",
                nowUtc,
                cancellationToken);

            return DiagnosticOrderOperationResult.Success(MapToDetailDto(corrected));
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Reason", "İptal gerekçesi zorunludur.");
        }

        var pathCase = await _dbContext.PathologyCases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        if (!await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            pathCase.Cancel(reason, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<PathologyCaseDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<PathologyCaseDetailDto>("Vaka kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.PathologyCaseCancel",
            "PathologyCase",
            pathCase.Id.ToString(),
            $"Patoloji vakası iptal edildi: {pathCase.PathologyNumber}. Gerekçe: {reason}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var pathCase = await _dbContext.PathologyCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (pathCase is null)
        {
            return DiagnosticOrderOperationResult.NotFound<PathologyCaseDetailDto>("Patoloji vaka kaydı bulunamadı.");
        }

        var readPermission = IsPatient(actor)
            ? HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn
            : actor.IsInRole(HospitalRoles.LaboratoryStaff)
                ? HospitalPermissions.Diagnostics.LaboratoryWorklistView
                : HospitalPermissions.ClinicalRecords.EncounterView;
        if (!await CanAccessCaseAsync(actor, pathCase, readPermission, IsPatient(actor), cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>();
        }

        if (IsPatient(actor))
        {
            var patientId = GetActorPatientId(actor);
            if (pathCase.PatientId != patientId)
            {
                return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>("Yalnızca kendi patoloji sonuçlarınıza erişebilirsiniz.");
            }

            if (pathCase.Status != PathologyCaseStatus.ReportFinalized && pathCase.Status != PathologyCaseStatus.Corrected)
            {
                return DiagnosticOrderOperationResult.Forbidden<PathologyCaseDetailDto>("Hastalar yalnızca onaylanmış veya düzeltilmiş patoloji raporlarını görüntüleyebilir.");
            }
        }

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(pathCase));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<PathologyCaseSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        PathologyCaseStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var query = _dbContext.PathologyCases
            .AsNoTracking();

        if (IsPatient(actor))
        {
            var actorPatientId = GetActorPatientId(actor);
            query = query.Where(c => c.PatientId == actorPatientId &&
                (c.Status == PathologyCaseStatus.ReportFinalized || c.Status == PathologyCaseStatus.Corrected));
        }
        else
        {
            if (patientId.HasValue && patientId.Value != Guid.Empty)
            {
                query = query.Where(c => c.PatientId == patientId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }
        }

        var list = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (IsPatient(actor))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<PathologyCaseSummaryDto>>();
        }

        var dtos = new List<PathologyCaseSummaryDto>();
        foreach (var pathCase in list)
        {
            if (await CanAccessCaseAsync(actor, pathCase, HospitalPermissions.Diagnostics.LaboratoryWorklistView, false, cancellationToken))
            {
                dtos.Add(MapToSummaryDto(pathCase));
            }
        }
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<PathologyCaseSummaryDto>>(dtos);
    }

    private static PathologyCaseDetailDto MapToDetailDto(PathologyCase c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.DiagnosticOrderItemId,
            c.PatientId,
            c.PathologyNumber,
            c.SpecimenType,
            c.AnatomicSite,
            c.ClinicalHistoryAndDiagnosis,
            c.FixativeUsed,
            c.Status,
            c.ReceivedAtUtc,
            c.ReceivedByUserId,
            c.GrossDescription,
            c.GrossExamAtUtc,
            c.GrossExamByUserId,
            c.MicroscopicDescription,
            c.MicroscopicExamAtUtc,
            c.MicroscopicExamByUserId,
            c.PathologicalDiagnosis,
            c.ReportDraftedAtUtc,
            c.ReportFinalizedAtUtc,
            c.PathologistUserId,
            c.CorrectionReason,
            c.PreviousCaseId,
            c.CancellationReason,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.Version);

    private static PathologyCaseSummaryDto MapToSummaryDto(PathologyCase c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.PatientId,
            c.PathologyNumber,
            c.SpecimenType,
            c.AnatomicSite,
            c.Status,
            c.ReceivedAtUtc,
            c.ReportFinalizedAtUtc,
            c.CreatedAtUtc);

    private static string GeneratePathologyNumber(DateTime nowUtc)
    {
        var randomSuffix = Random.Shared.Next(100000, 999999);
        return $"DEMO-PAT-{nowUtc:yyyy}-{randomSuffix}";
    }

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private async Task<bool> CanAccessCaseAsync(
        ClaimsPrincipal actor,
        PathologyCase pathCase,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == pathCase.DiagnosticOrderId, cancellationToken);
        return order is not null
            && await CanAccessOrderAsync(actor, order, permission, allowPatientOwnFinalResult, cancellationToken);
    }

    private Task<bool> CanAccessOrderAsync(
        ClaimsPrincipal actor,
        DiagnosticOrder order,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken) =>
        _accessContext.CanAccessResourceAsync(
            actor,
            new DiagnosticResourceContext(order.EncounterId, order.PatientId, order.DepartmentId, order.OrderType),
            permission,
            allowPatientOwnFinalResult,
            cancellationToken);

    private static Guid GetActorUserId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

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
