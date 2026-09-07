using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class PatientTimelineService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IPatientTimelineService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<PatientTimelinePagedDto>> GetPatientTimelineAsync(
        ClaimsPrincipal actor,
        GetPatientTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(query);

        if (query.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<PatientTimelinePagedDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                query.PatientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<PatientTimelinePagedDto>();
        }

        var isPatientActor = ClinicalRecordAccessControl.IsPatientOwnRecord(actor, query.PatientId);
        var includeEnteredInError = query.IncludeEnteredInError && !isPatientActor;
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var allItems = new List<PatientTimelineItemDto>();

        // 1. Encounters
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Encounter))
        {
            var encounters = await _dbContext.Encounters
                .AsNoTracking()
                .Where(e => e.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var e in encounters)
            {
                var isEnteredInError = e.Status == EncounterStatus.EnteredInError;
                if (!includeEnteredInError && isEnteredInError)
                {
                    continue;
                }

                var timestamp = e.ActualStartTimeUtc ?? e.PlannedStartTimeUtc ?? e.CreatedAtUtc;
                if (IsOutsideDateRange(timestamp, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                allItems.Add(new PatientTimelineItemDto(
                    e.Id,
                    PatientTimelineEventType.Encounter,
                    timestamp,
                    "Muayene / Karşılaşma",
                    $"Karşılaşma türü: {e.EncounterType}",
                    e.Id,
                    e.Status.ToString(),
                    e.Status == EncounterStatus.Completed ? "Normal" : "Info",
                    isEnteredInError));
            }
        }

        // 2. Vital Signs
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.VitalSigns))
        {
            var vitals = await _dbContext.VitalSignObservations
                .AsNoTracking()
                .Where(v => v.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var v in vitals)
            {
                if (!includeEnteredInError && v.IsEnteredInError)
                {
                    continue;
                }

                if (IsOutsideDateRange(v.MeasuredAtUtc, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                var severity = v.Interpretation switch
                {
                    VitalInterpretation.CriticalLow or VitalInterpretation.CriticalHigh => "Critical",
                    VitalInterpretation.Low or VitalInterpretation.High => "Warning",
                    _ => "Normal",
                };

                allItems.Add(new PatientTimelineItemDto(
                    v.Id,
                    PatientTimelineEventType.VitalSigns,
                    v.MeasuredAtUtc,
                    "Vital bulgu ölçümü",
                    $"{v.MeasurementType}: {v.Value} {v.Unit} ({v.Interpretation})",
                    v.EncounterId,
                    v.Interpretation.ToString(),
                    severity,
                    v.IsEnteredInError));
            }
        }

        // 3. Clinical Notes (Draft notes hidden from patients)
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.ClinicalNote))
        {
            var notes = await _dbContext.ClinicalNotes
                .AsNoTracking()
                .Where(n => n.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var n in notes)
            {
                var isEnteredInError = n.Status == ClinicalNoteStatus.EnteredInError;
                if (!includeEnteredInError && isEnteredInError)
                {
                    continue;
                }

                if (isPatientActor && n.Status == ClinicalNoteStatus.Draft)
                {
                    continue;
                }

                var timestamp = n.SignedAtUtc ?? n.CreatedAtUtc;
                if (IsOutsideDateRange(timestamp, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                allItems.Add(new PatientTimelineItemDto(
                    n.Id,
                    PatientTimelineEventType.ClinicalNote,
                    timestamp,
                    "Klinik not",
                    $"Not türü: {n.NoteType}; durum: {n.Status}",
                    n.EncounterId,
                    n.Status.ToString(),
                    "Info",
                    isEnteredInError));
            }
        }

        // 4. Diagnoses
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Diagnosis))
        {
            var diagnoses = await _dbContext.EncounterDiagnoses
                .AsNoTracking()
                .Where(d => d.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var d in diagnoses)
            {
                if (!includeEnteredInError && d.IsEnteredInError)
                {
                    continue;
                }

                if (IsOutsideDateRange(d.DiagnosedAtUtc, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                var summary = d.IsCoded
                    ? $"Kod: {d.Icd10Code}; tür: {d.DiagnosisType}"
                    : $"Serbest metin tanı; tür: {d.DiagnosisType}";

                allItems.Add(new PatientTimelineItemDto(
                    d.Id,
                    PatientTimelineEventType.Diagnosis,
                    d.DiagnosedAtUtc,
                    $"Tanı: {d.DiagnosisTitle}",
                    summary,
                    d.EncounterId,
                    d.DiagnosisType.ToString(),
                    d.DiagnosisType == DiagnosisType.Final ? "Normal" : "Warning",
                    d.IsEnteredInError));
            }
        }

        // 5. Consultations
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Consultation))
        {
            var consultations = await _dbContext.ConsultationRequests
                .AsNoTracking()
                .Where(c => c.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var c in consultations)
            {
                var isEnteredInError = c.Status == ConsultationStatus.EnteredInError;
                if (!includeEnteredInError && isEnteredInError)
                {
                    continue;
                }

                var timestamp = c.CompletedAtUtc ?? c.AcceptedAtUtc ?? c.RequestedAtUtc;
                if (IsOutsideDateRange(timestamp, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                var severity = c.Urgency == ConsultationUrgency.Stat
                    ? "Critical"
                    : (c.Urgency == ConsultationUrgency.Urgent ? "Warning" : "Normal");

                allItems.Add(new PatientTimelineItemDto(
                    c.Id,
                    PatientTimelineEventType.Consultation,
                    timestamp,
                    "Konsültasyon",
                    $"Aciliyet: {c.Urgency}; durum: {c.Status}",
                    c.EncounterId,
                    c.Status.ToString(),
                    severity,
                    isEnteredInError));
            }
        }

        // 6. Attachments
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Attachment))
        {
            var attachments = await _dbContext.ClinicalAttachments
                .AsNoTracking()
                .Where(a => a.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var a in attachments)
            {
                if (!includeEnteredInError && a.IsEnteredInError)
                {
                    continue;
                }

                if (IsOutsideDateRange(a.UploadedAtUtc, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                allItems.Add(new PatientTimelineItemDto(
                    a.Id,
                    PatientTimelineEventType.Attachment,
                    a.UploadedAtUtc,
                    "Klinik ek",
                    $"Tür: {a.AttachmentType}; boyut: {a.ByteSize / 1024} KB",
                    a.EncounterId,
                    a.AttachmentType.ToString(),
                    "Info",
                    a.IsEnteredInError));
            }
        }

        // 7. Allergies
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Allergy))
        {
            var allergies = await _dbContext.AllergyIntolerances
                .AsNoTracking()
                .Where(a => a.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var a in allergies)
            {
                var isEnteredInError = a.VerificationStatus == AllergyVerificationStatus.EnteredInError;
                if (!includeEnteredInError && isEnteredInError)
                {
                    continue;
                }

                if (IsOutsideDateRange(a.RecordedAtUtc, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                var severity = a.Criticality == AllergyCriticality.High ? "Critical" : "Warning";

                allItems.Add(new PatientTimelineItemDto(
                    a.Id,
                    PatientTimelineEventType.Allergy,
                    a.RecordedAtUtc,
                    $"Alerji / İntolerans: {a.Substance}",
                    $"Kategori: {a.Category}; kritiklik: {a.Criticality}",
                    a.EncounterId,
                    a.Criticality.ToString(),
                    severity,
                    isEnteredInError));
            }
        }

        // 8. Problems
        if (ShouldInclude(query.EventTypes, PatientTimelineEventType.Problem))
        {
            var problems = await _dbContext.ClinicalProblems
                .AsNoTracking()
                .Where(p => p.PatientId == query.PatientId)
                .ToListAsync(cancellationToken);

            foreach (var p in problems)
            {
                var isEnteredInError = p.VerificationStatus == ProblemVerificationStatus.EnteredInError;
                if (!includeEnteredInError && isEnteredInError)
                {
                    continue;
                }

                if (IsOutsideDateRange(p.RecordedAtUtc, query.FromUtc, query.ToUtc))
                {
                    continue;
                }

                allItems.Add(new PatientTimelineItemDto(
                    p.Id,
                    PatientTimelineEventType.Problem,
                    p.RecordedAtUtc,
                    $"Klinik problem: {p.ProblemTitle}",
                    $"Kategori: {p.Category}; durum: {p.ClinicalStatus}",
                    p.EncounterId,
                    p.ClinicalStatus.ToString(),
                    p.ClinicalStatus == ProblemClinicalStatus.Active ? "Warning" : "Normal",
                    isEnteredInError));
            }
        }

        // Sorting & Pagination
        var sorted = allItems.OrderByDescending(i => i.TimestampUtc).ToList();
        var totalCount = sorted.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages == 0)
        {
            totalPages = 1;
        }

        var pagedItems = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PatientTimelinePagedDto(
            query.PatientId,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: pageNumber > 1,
            HasNextPage: pageNumber < totalPages,
            pagedItems);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.TimelineView",
            query.PatientId.ToString(),
            AuditOutcome.Success,
            "Hasta zaman çizelgesi görüntülendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(result);
    }

    private static bool ShouldInclude(
        IReadOnlyList<PatientTimelineEventType>? eventTypes,
        PatientTimelineEventType eventType)
    {
        if (eventTypes is null || eventTypes.Count == 0)
        {
            return true;
        }

        return eventTypes.Contains(eventType);
    }

    private static bool IsOutsideDateRange(DateTime timestamp, DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc.HasValue && timestamp < fromUtc.Value)
        {
            return true;
        }

        if (toUtc.HasValue && timestamp > toUtc.Value)
        {
            return true;
        }

        return false;
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string reason,
        CancellationToken cancellationToken)
    {
        Guid? actorUserId = null;
        Guid? actorPersonId = null;

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var uid))
        {
            actorUserId = uid;
        }

        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var pid))
        {
            actorPersonId = pid;
        }

        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "ClinicalRecords",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
