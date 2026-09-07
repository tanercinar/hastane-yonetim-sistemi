using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

/// <summary>
/// Trusted application contract for modules that must reference the canonical encounter model.
/// Authorization remains the responsibility of the calling API boundary.
/// </summary>
public interface IEncounterReferenceLookup
{
    Task<EncounterReferenceDto?> FindAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);
}

public sealed record EncounterReferenceDto(
    Guid Id,
    Guid PatientId,
    Guid? AppointmentId,
    EncounterType EncounterType,
    EncounterStatus Status);
