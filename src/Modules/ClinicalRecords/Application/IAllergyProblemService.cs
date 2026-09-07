using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IAllergyProblemService
{
    Task<ClinicalEncounterOperationResult<AllergyDto>> CreateAllergyAsync(
        ClaimsPrincipal actor,
        CreateAllergyCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<AllergyDto>> UpdateAllergyStatusAsync(
        ClaimsPrincipal actor,
        UpdateAllergyStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<AllergyDto>> MarkAllergyEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkAllergyEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<AllergyDto>>> GetPatientAllergiesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> CreateProblemAsync(
        ClaimsPrincipal actor,
        CreateClinicalProblemCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> UpdateProblemStatusAsync(
        ClaimsPrincipal actor,
        UpdateClinicalProblemStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> MarkProblemEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkClinicalProblemEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalProblemDto>>> GetPatientProblemsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
