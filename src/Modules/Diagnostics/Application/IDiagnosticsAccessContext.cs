using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record DiagnosticEncounterContext(
    Guid EncounterId,
    Guid PatientId,
    Guid DepartmentId,
    Guid PrimaryPractitionerId,
    bool IsOpenForClinicalEntry);

public sealed record DiagnosticResourceContext(
    Guid EncounterId,
    Guid PatientId,
    Guid OrderingDepartmentId,
    DiagnosticOrderType OrderType);

/// <summary>
/// Host-owned authorization port. Diagnostics never reads another module's DbContext directly;
/// the composition root resolves encounter, care-relationship and organization assignment scope.
/// </summary>
public interface IDiagnosticsAccessContext
{
    Task<DiagnosticEncounterContext?> FindEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessEncounterAsync(
        ClaimsPrincipal actor,
        DiagnosticEncounterContext encounter,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessResourceAsync(
        ClaimsPrincipal actor,
        DiagnosticResourceContext resource,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessDiagnosticAreaAsync(
        ClaimsPrincipal actor,
        DiagnosticOrderType orderType,
        string permission,
        CancellationToken cancellationToken = default);
}
