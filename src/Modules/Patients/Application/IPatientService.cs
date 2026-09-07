using System.Security.Claims;

namespace HospitalManagement.Modules.Patients.Application;

public interface IPatientService
{
    Task<PatientOperationResult<PatientDetailDto>> RegisterPatientAsync(
        ClaimsPrincipal actor,
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientOperationResult<PatientDetailDto>> UpdatePatientAsync(
        ClaimsPrincipal actor,
        UpdatePatientCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientOperationResult<PatientDetailDto>> GetPatientByIdAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientOperationResult<PatientDetailDto>> GetPatientByPersonIdAsync(
        ClaimsPrincipal actor,
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<PatientOperationResult<PatientListResult>> SearchPatientsAsync(
        ClaimsPrincipal actor,
        PatientSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<PatientOperationResult<DuplicateCheckResult>> CheckDuplicateAsync(
        DuplicateCheckQuery query,
        CancellationToken cancellationToken = default);
}
