namespace HospitalManagement.Modules.Inpatient.Application;

public interface INursingCareService
{
    Task<InpatientOperationResult<NursingObservationDto>> RecordObservationAsync(
        RecordObservationDto request,
        Guid recordedByNurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<NursingObservationDto>> RecordObservationCorrectionAsync(
        Guid observationId,
        CorrectObservationDto request,
        Guid recordedByNurseId,
        CancellationToken cancellationToken = default);

    Task<List<NursingObservationDto>> GetObservationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<NursingCarePlanDto>> CreateCarePlanAsync(
        CreateCarePlanDto request,
        Guid createdByNurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<NursingCareTaskDto>> AddTaskToCarePlanAsync(
        Guid carePlanId,
        AddCareTaskDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<NursingCareTaskDto>> CompleteTaskAsync(
        Guid taskId,
        CompleteCareTaskDto request,
        Guid completedByNurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<NursingCareTaskDto>> CancelTaskAsync(
        Guid taskId,
        CancelCareTaskDto request,
        Guid cancelledByNurseId,
        CancellationToken cancellationToken = default);

    Task<List<NursingCarePlanDto>> GetCarePlansByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<NursingCareTaskDto>> GetOverdueTasksAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default);
}
