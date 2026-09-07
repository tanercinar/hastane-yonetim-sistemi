using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class DentalCareService : IDentalCareService
{
    private readonly SpecialtyCareDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public DentalCareService(
        SpecialtyCareDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SpecialtyOperationResult<ToothConditionDto>> RecordToothConditionAsync(
        RecordToothConditionDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<ToothConditionDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (!FdiToothValidator.IsValidToothNumber(dto.ToothNumber))
        {
            return SpecialtyOperationResult.Validation<ToothConditionDto>("ToothNumber", $"Geçersiz FDI diş numarası: {dto.ToothNumber}.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Calculate next version for this tooth
        var lastVersion = await _dbContext.DentalToothConditions
            .Where(t => t.PatientId == dto.PatientId && t.ToothNumber == dto.ToothNumber)
            .MaxAsync(t => (int?)t.Version, cancellationToken) ?? 0;

        DentalToothCondition condition;
        try
        {
            condition = DentalToothCondition.Record(
                Guid.NewGuid(),
                dto.PatientId,
                dto.ToothNumber,
                dto.Condition,
                dto.AffectedSurfaces,
                dto.Notes,
                staffId,
                lastVersion + 1,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<ToothConditionDto>("ToothCondition", ex.Message);
        }

        _dbContext.DentalToothConditions.Add(condition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.DentalToothConditionRecord",
            condition.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToothConditionToDto(condition));
    }

    public async Task<List<ToothConditionDto>> GetLatestOdontogramByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var allConditions = await _dbContext.DentalToothConditions
            .AsNoTracking()
            .Where(t => t.PatientId == patientId)
            .OrderBy(t => t.ToothNumber)
            .ThenByDescending(t => t.Version)
            .ToListAsync(cancellationToken);

        // Group by tooth number and pick latest version
        var latestPerTooth = allConditions
            .GroupBy(t => t.ToothNumber)
            .Select(g => g.First())
            .OrderBy(t => t.ToothNumber)
            .Select(MapToothConditionToDto)
            .ToList();

        return latestPerTooth;
    }

    public async Task<List<ToothConditionDto>> GetToothHistoryAsync(
        Guid patientId,
        int toothNumber,
        CancellationToken cancellationToken = default)
    {
        var history = await _dbContext.DentalToothConditions
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && t.ToothNumber == toothNumber)
            .OrderByDescending(t => t.Version)
            .ToListAsync(cancellationToken);

        return history.Select(MapToothConditionToDto).ToList();
    }

    public async Task<SpecialtyOperationResult<DentalProcedureDto>> PlanProcedureAsync(
        PlanDentalProcedureDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DentalProcedureDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (dto.PerformedByDoctorId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DentalProcedureDto>("PerformedByDoctorId", "Diş hekimi seçilmelidir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        DentalProcedure procedure;
        try
        {
            procedure = DentalProcedure.Plan(
                Guid.NewGuid(),
                dto.PatientId,
                dto.EncounterId,
                dto.ToothNumber,
                dto.Surfaces,
                dto.ProcedureCode,
                dto.ProcedureName,
                dto.EstimatedCost,
                dto.PerformedByDoctorId,
                dto.ScheduledDateUtc,
                dto.ClinicalNotes,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<DentalProcedureDto>("DentalProcedure", ex.Message);
        }

        _dbContext.DentalProcedures.Add(procedure);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.DentalProcedurePlan",
            procedure.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapProcedureToDto(procedure));
    }

    public async Task<SpecialtyOperationResult<DentalProcedureDto>> CompleteProcedureAsync(
        Guid procedureId,
        DateTime completedDateUtc,
        string? completionNotes,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var procedure = await _dbContext.DentalProcedures
            .FirstOrDefaultAsync(p => p.Id == procedureId, cancellationToken);

        if (procedure is null)
        {
            return SpecialtyOperationResult.NotFound<DentalProcedureDto>("Diş tedavisi işlemi bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            procedure.Complete(completedDateUtc, completionNotes, now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Conflict<DentalProcedureDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.DentalProcedureComplete",
            procedure.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapProcedureToDto(procedure));
    }

    public async Task<List<DentalProcedureDto>> GetProceduresByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var procedures = await _dbContext.DentalProcedures
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return procedures.Select(MapProcedureToDto).ToList();
    }

    public async Task<SpecialtyOperationResult<DentalExaminationDto>> CreateExaminationAsync(
        CreateDentalExaminationDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DentalExaminationDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (dto.DentistId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DentalExaminationDto>("DentistId", "Diş hekimi seçilmelidir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        DentalExaminationRecord exam;
        try
        {
            exam = DentalExaminationRecord.Create(
                Guid.NewGuid(),
                dto.PatientId,
                dto.EncounterId,
                dto.DentistId,
                dto.ExaminationDateUtc,
                dto.ChiefComplaint,
                dto.DiagnosisNotes,
                dto.TreatmentPlanSummary,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<DentalExaminationDto>("DentalExamination", ex.Message);
        }

        _dbContext.DentalExaminations.Add(exam);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.DentalExaminationCreate",
            exam.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapExaminationToDto(exam));
    }

    public async Task<List<DentalExaminationDto>> GetExaminationsByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var exams = await _dbContext.DentalExaminations
            .AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.ExaminationDateUtc)
            .ToListAsync(cancellationToken);

        return exams.Select(MapExaminationToDto).ToList();
    }

    private static ToothConditionDto MapToothConditionToDto(DentalToothCondition t) =>
        new(
            t.Id,
            t.PatientId,
            t.ToothNumber,
            t.Condition,
            t.AffectedSurfaces,
            t.Notes,
            t.RecordedAtUtc,
            t.RecordedByStaffId,
            t.Version);

    private static DentalProcedureDto MapProcedureToDto(DentalProcedure p) =>
        new(
            p.Id,
            p.PatientId,
            p.EncounterId,
            p.ProcedureProtocolNumber,
            p.ToothNumber,
            p.Surfaces,
            p.ProcedureCode,
            p.ProcedureName,
            p.Status,
            p.EstimatedCost,
            p.PerformedByDoctorId,
            p.ScheduledDateUtc,
            p.CompletedDateUtc,
            p.ClinicalNotes,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);

    private static DentalExaminationDto MapExaminationToDto(DentalExaminationRecord e) =>
        new(
            e.Id,
            e.PatientId,
            e.EncounterId,
            e.ExaminationProtocolNumber,
            e.DentistId,
            e.ExaminationDateUtc,
            e.ChiefComplaint,
            e.DiagnosisNotes,
            e.TreatmentPlanSummary,
            e.CreatedAtUtc);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        Guid actorUserId,
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
            TargetResourceType: "DentalCare",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: "Uzmanlık klinik kayıt eylemi tamamlandı.",
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
