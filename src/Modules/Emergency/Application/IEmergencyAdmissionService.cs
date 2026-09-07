using HospitalManagement.Modules.Emergency.Domain;

namespace HospitalManagement.Modules.Emergency.Application;

public interface IEmergencyAdmissionService
{
    Task<EmergencyOperationResult<EmergencyAdmissionDto>> CreateAdmissionAsync(
        CreateEmergencyAdmissionDto request,
        Guid admittingStaffId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyAdmissionDto>> RecordTriageAsync(
        Guid id,
        RecordTriageDto request,
        Guid triageNurseId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyAdmissionDto>> AssignDoctorAsync(
        Guid id,
        AssignDoctorDto request,
        Guid assignedByStaffId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyAdmissionDto>> UpdateStatusAsync(
        Guid id,
        UpdateEmergencyStatusDto request,
        Guid updatedByStaffId,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionDto?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionDto?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyAdmissionSummaryDto>> GetAdmissionsAsync(
        EmergencyAdmissionStatus? status = null,
        TriageLevel? triageLevel = null,
        Guid? patientId = null,
        Guid? assignedDoctorId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default);
}
