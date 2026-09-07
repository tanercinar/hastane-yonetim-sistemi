namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface IDentalCareService
{
    Task<SpecialtyOperationResult<ToothConditionDto>> RecordToothConditionAsync(
        RecordToothConditionDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<List<ToothConditionDto>> GetLatestOdontogramByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<ToothConditionDto>> GetToothHistoryAsync(
        Guid patientId,
        int toothNumber,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<DentalProcedureDto>> PlanProcedureAsync(
        PlanDentalProcedureDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<DentalProcedureDto>> CompleteProcedureAsync(
        Guid procedureId,
        DateTime completedDateUtc,
        string? completionNotes,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<List<DentalProcedureDto>> GetProceduresByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<DentalExaminationDto>> CreateExaminationAsync(
        CreateDentalExaminationDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<List<DentalExaminationDto>> GetExaminationsByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
