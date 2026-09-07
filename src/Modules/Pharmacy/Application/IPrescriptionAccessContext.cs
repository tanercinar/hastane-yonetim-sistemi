using System.Security.Claims;

namespace HospitalManagement.Modules.Pharmacy.Application;

/// <summary>
/// Pharmacy-owned port for resolving clinical and organization scope without reaching into
/// another module's DbContext or domain model.
/// </summary>
public interface IPrescriptionAccessContext
{
    Task<PrescriptionEncounterContext?> FindEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessEncounterAsync(
        ClaimsPrincipal actor,
        PrescriptionEncounterContext encounter,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToFacilityForDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default);
}

public sealed record PrescriptionEncounterContext(
    Guid EncounterId,
    Guid PatientId,
    Guid DepartmentId,
    bool AllowsClinicalEntry);
