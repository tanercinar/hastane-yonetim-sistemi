using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IClinicalNoteService
{
    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> CreateDraftNoteAsync(
        ClaimsPrincipal actor,
        CreateDraftNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> UpdateDraftNoteAsync(
        ClaimsPrincipal actor,
        UpdateDraftNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> SignNoteAsync(
        ClaimsPrincipal actor,
        SignClinicalNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> AddAddendumAsync(
        ClaimsPrincipal actor,
        AddClinicalNoteAddendumCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkClinicalNoteEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> GetNoteByIdAsync(
        ClaimsPrincipal actor,
        Guid noteId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalNoteDto>>> GetEncounterNotesAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalNoteDto>>> GetPatientNotesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
