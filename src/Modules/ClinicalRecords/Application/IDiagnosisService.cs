using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IDiagnosisService
{
    Task<IReadOnlyList<DiagnosisCatalogItemDto>> SearchDiagnosisCatalogAsync(
        string? query,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> RecordDiagnosisAsync(
        ClaimsPrincipal actor,
        RecordDiagnosisCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> UpdateDiagnosisAsync(
        ClaimsPrincipal actor,
        UpdateDiagnosisCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkDiagnosisEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDiagnosisDto>>> GetEncounterDiagnosesAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDiagnosisDto>>> GetPatientDiagnosesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
