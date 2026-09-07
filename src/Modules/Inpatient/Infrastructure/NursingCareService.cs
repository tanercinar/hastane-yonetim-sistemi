using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class NursingCareService : INursingCareService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;

    public NursingCareService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    }

    public async Task<InpatientOperationResult<NursingObservationDto>> RecordObservationAsync(
        RecordObservationDto request,
        Guid recordedByNurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<NursingObservationDto>("Yatış kaydı bulunamadı.");
        }

        if (admission.Status != AdmissionStatus.Admitted && admission.Status != AdmissionStatus.Transferring)
        {
            return InpatientOperationResult.Conflict<NursingObservationDto>(
                $"Yalnızca aktif yatıştaki hastalar için gözlem kaydı girilebilir. Mevcut durum: {admission.Status}");
        }

        var now = DateTime.UtcNow;
        var observedAt = request.ObservedAtUtc ?? now;

        NursingObservation observation;
        try
        {
            observation = NursingObservation.Record(
                Guid.NewGuid(),
                admission.Id,
                admission.PatientId,
                recordedByNurseId,
                observedAt,
                request.SystolicBp,
                request.DiastolicBp,
                request.HeartRate,
                request.RespiratoryRate,
                request.BodyTemperatureCelsius,
                request.OxygenSaturationPercent,
                request.PainScale,
                request.OralIntakeMl,
                request.IvIntakeMl,
                request.UrineOutputMl,
                request.DrainOutputMl,
                request.OtherOutputMl,
                request.Consciousness,
                request.ClinicalNotes,
                now);
        }
        catch (ArgumentException ex)
        {
            return InpatientOperationResult.Validation<NursingObservationDto>("Observation", ex.Message);
        }

        _dbContext.NursingObservations.Add(observation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.ObservationRecord",
            observation.Id.ToString(),
            "Hemşire vital ve klinik gözlem kaydı girildi",
            recordedByNurseId,
            JsonSerializer.Serialize(new
            {
                ObservationId = observation.Id,
                AdmissionId = admission.Id,
                ObservedAtUtc = observation.ObservedAtUtc,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToObservationDto(observation));
    }

    public async Task<InpatientOperationResult<NursingObservationDto>> RecordObservationCorrectionAsync(
        Guid observationId,
        CorrectObservationDto request,
        Guid recordedByNurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var original = await _dbContext.NursingObservations
            .FirstOrDefaultAsync(o => o.Id == observationId, cancellationToken);
        if (original is null)
        {
            return InpatientOperationResult.NotFound<NursingObservationDto>("Düzeltilecek gözlem kaydı bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.CorrectionReason))
        {
            return InpatientOperationResult.Validation<NursingObservationDto>(
                "CorrectionReason", "Klinik gözlem düzeltmesi için gerekçe belirtilmesi zorunludur.");
        }

        var now = DateTime.UtcNow;
        NursingObservation correction;
        try
        {
            correction = NursingObservation.CreateCorrection(
                Guid.NewGuid(),
                original,
                recordedByNurseId,
                request.CorrectionReason,
                request.SystolicBp,
                request.DiastolicBp,
                request.HeartRate,
                request.RespiratoryRate,
                request.BodyTemperatureCelsius,
                request.OxygenSaturationPercent,
                request.PainScale,
                request.OralIntakeMl,
                request.IvIntakeMl,
                request.UrineOutputMl,
                request.DrainOutputMl,
                request.OtherOutputMl,
                request.Consciousness,
                request.ClinicalNotes,
                now);
        }
        catch (ArgumentException ex)
        {
            return InpatientOperationResult.Validation<NursingObservationDto>("ObservationCorrection", ex.Message);
        }

        _dbContext.NursingObservations.Add(correction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.ObservationCorrection",
            correction.Id.ToString(),
            "Klinik gözlem düzeltme kaydı eklendi",
            recordedByNurseId,
            JsonSerializer.Serialize(new
            {
                CorrectionId = correction.Id,
                OriginalObservationId = original.Id,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToObservationDto(correction));
    }

    public async Task<List<NursingObservationDto>> GetObservationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var observations = await _dbContext.NursingObservations
            .AsNoTracking()
            .Where(o => o.AdmissionId == admissionId)
            .OrderByDescending(o => o.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        return observations.Select(MapToObservationDto).ToList();
    }

    public async Task<InpatientOperationResult<NursingCarePlanDto>> CreateCarePlanAsync(
        CreateCarePlanDto request,
        Guid createdByNurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<NursingCarePlanDto>("Yatış kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        NursingCarePlan plan;
        try
        {
            plan = NursingCarePlan.Create(
                Guid.NewGuid(),
                admission.Id,
                admission.PatientId,
                createdByNurseId,
                request.NursingDiagnosis,
                request.Goal,
                now);
        }
        catch (ArgumentException ex)
        {
            return InpatientOperationResult.Validation<NursingCarePlanDto>("CarePlan", ex.Message);
        }

        _dbContext.NursingCarePlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.CarePlanCreate",
            plan.Id.ToString(),
            "Hemşirelik bakım planı oluşturuldu",
            createdByNurseId,
            JsonSerializer.Serialize(new
            {
                CarePlanId = plan.Id,
                AdmissionId = admission.Id,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToCarePlanDto(plan));
    }

    public async Task<InpatientOperationResult<NursingCareTaskDto>> AddTaskToCarePlanAsync(
        Guid carePlanId,
        AddCareTaskDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await _dbContext.NursingCarePlans
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == carePlanId, cancellationToken);
        if (plan is null)
        {
            return InpatientOperationResult.NotFound<NursingCareTaskDto>("Bakım planı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        NursingCareTask task;
        try
        {
            task = plan.AddTask(
                Guid.NewGuid(),
                request.Title,
                request.Frequency,
                request.DueTimeUtc,
                now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<NursingCareTaskDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.CareTaskAdd",
            task.Id.ToString(),
            "Bakım planına görev eklendi",
            nurseId,
            JsonSerializer.Serialize(new
            {
                TaskId = task.Id,
                CarePlanId = plan.Id,
                DueTimeUtc = task.DueTimeUtc,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToCareTaskDto(task));
    }

    public async Task<InpatientOperationResult<NursingCareTaskDto>> CompleteTaskAsync(
        Guid taskId,
        CompleteCareTaskDto request,
        Guid completedByNurseId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.NursingCareTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return InpatientOperationResult.NotFound<NursingCareTaskDto>("Bakım görevi bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            task.Complete(completedByNurseId, request.Notes, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<NursingCareTaskDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<NursingCareTaskDto>(
                "Bakım görevi başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }

        await PublishAuditAsync(
            "Inpatient.CareTaskComplete",
            task.Id.ToString(),
            "Bakım görevi tamamlandı",
            completedByNurseId,
            JsonSerializer.Serialize(new
            {
                TaskId = task.Id,
                CompletedByNurseId = completedByNurseId,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToCareTaskDto(task));
    }

    public async Task<InpatientOperationResult<NursingCareTaskDto>> CancelTaskAsync(
        Guid taskId,
        CancelCareTaskDto request,
        Guid cancelledByNurseId,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.NursingCareTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return InpatientOperationResult.NotFound<NursingCareTaskDto>("Bakım görevi bulunamadı.");
        }

        var now = DateTime.UtcNow;
        try
        {
            task.Cancel(cancelledByNurseId, request.Reason, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<NursingCareTaskDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<NursingCareTaskDto>(
                "Bakım görevi başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }

        await PublishAuditAsync(
            "Inpatient.CareTaskCancel",
            task.Id.ToString(),
            "Bakım görevi iptal edildi",
            cancelledByNurseId,
            JsonSerializer.Serialize(new
            {
                TaskId = task.Id,
                CancelledByNurseId = cancelledByNurseId,
            }),
            now,
            cancellationToken);

        return InpatientOperationResult.Success(MapToCareTaskDto(task));
    }

    public async Task<List<NursingCarePlanDto>> GetCarePlansByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.NursingCarePlans
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.AdmissionId == admissionId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return plans.Select(MapToCarePlanDto).ToList();
    }

    public async Task<List<NursingCareTaskDto>> GetOverdueTasksAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.NursingCareTasks
            .AsNoTracking()
            .Where(t => t.Status == CareTaskStatus.Pending && t.DueTimeUtc < now);

        if (admissionId.HasValue && admissionId.Value != Guid.Empty)
        {
            var planIds = await _dbContext.NursingCarePlans
                .Where(p => p.AdmissionId == admissionId.Value)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(t => planIds.Contains(t.CarePlanId));
        }
        else if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            var admissionIds = await _dbContext.Admissions
                .Where(a => a.AdmittingWardId == wardId.Value &&
                    (a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Transferring))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            var planIds = await _dbContext.NursingCarePlans
                .Where(p => admissionIds.Contains(p.AdmissionId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(t => planIds.Contains(t.CarePlanId));
        }

        var overdueTasks = await query
            .OrderBy(t => t.DueTimeUtc)
            .ToListAsync(cancellationToken);

        foreach (var task in overdueTasks)
        {
            task.CheckAndMarkOverdue(now);
        }

        return overdueTasks.Select(MapToCareTaskDto).ToList();
    }

    private static NursingObservationDto MapToObservationDto(NursingObservation o) =>
        new(
            o.Id,
            o.AdmissionId,
            o.PatientId,
            o.RecordedByNurseId,
            o.ObservedAtUtc,
            o.SystolicBp,
            o.DiastolicBp,
            o.HeartRate,
            o.RespiratoryRate,
            o.BodyTemperatureCelsius,
            o.OxygenSaturationPercent,
            o.PainScale,
            o.OralIntakeMl,
            o.IvIntakeMl,
            o.UrineOutputMl,
            o.DrainOutputMl,
            o.OtherOutputMl,
            o.Consciousness,
            o.ClinicalNotes,
            o.IsCorrection,
            o.CorrectedObservationId,
            o.CorrectionReason,
            o.CreatedAtUtc,
            o.Version);

    private static NursingCarePlanDto MapToCarePlanDto(NursingCarePlan p) =>
        new(
            p.Id,
            p.AdmissionId,
            p.PatientId,
            p.CreatedByNurseId,
            p.NursingDiagnosis,
            p.Goal,
            p.Status,
            p.CreatedAtUtc,
            p.ResolvedAtUtc,
            p.ResolutionNotes,
            p.Version,
            p.Tasks.Select(MapToCareTaskDto).ToList());

    private static NursingCareTaskDto MapToCareTaskDto(NursingCareTask t) =>
        new(
            t.Id,
            t.CarePlanId,
            t.Title,
            t.Frequency,
            t.DueTimeUtc,
            t.Status,
            t.CompletedByNurseId,
            t.CompletedAtUtc,
            t.CompletionNotes,
            t.CancelledByNurseId,
            t.CancelledAtUtc,
            t.CancellationReason,
            t.CreatedAtUtc,
            t.Version);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        string reason,
        Guid actorUserId,
        string? detailsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: actorUserId != Guid.Empty ? actorUserId : null,
            ActorPersonId: null,
            ActorRole: null,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "NursingCare",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
