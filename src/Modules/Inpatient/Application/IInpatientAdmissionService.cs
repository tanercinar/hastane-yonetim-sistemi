namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientAdmissionService
{
    Task<InpatientOperationResult<InpatientAdmissionDto>> RequestAdmissionAsync(
        CreateAdmissionDto request,
        Guid orderingDoctorId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientAdmissionDto>> AcceptAdmissionAsync(
        Guid admissionId,
        Guid acceptedByUserId,
        AcceptAdmissionDto? request = null,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientAdmissionDto>> AdmitPatientAsync(
        Guid admissionId,
        AdmitPatientDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientAdmissionDto>> CancelAdmissionAsync(
        Guid admissionId,
        CancelAdmissionDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientAdmissionDto>> UpdateCareDetailsAsync(
        Guid admissionId,
        UpdateCareDetailsDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientAdmissionDto?> GetAdmissionByIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<InpatientAdmissionDto?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<InpatientAdmissionSummaryDto>> GetAdmissionsAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);
}
