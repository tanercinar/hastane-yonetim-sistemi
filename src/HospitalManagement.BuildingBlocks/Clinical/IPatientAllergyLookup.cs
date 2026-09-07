namespace HospitalManagement.BuildingBlocks.Clinical;

public interface IPatientAllergyLookup
{
    Task<IReadOnlyList<PatientAllergySummaryDto>> GetActiveAllergiesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}

public sealed record PatientAllergySummaryDto(
    Guid AllergyId,
    string Allergen,
    string? Category,
    string? Criticality,
    string? Reaction);
