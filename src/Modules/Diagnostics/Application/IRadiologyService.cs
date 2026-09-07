using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IRadiologyService
{
    Task<DiagnosticOrderOperationResult<IReadOnlyList<RadiologyCatalogItemDto>>> GetCatalogItemsAsync(
        ClaimsPrincipal actor,
        RadiologyModality? modality = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> EnsureStudyForOrderItemAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> ScheduleAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> CompleteAcquisitionAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string? technicianNotes,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> DraftReportAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reportText,
        string? impression,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> FinalizeReportAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reportText,
        string impression,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> AddAddendumAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string addendumText,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<RadiologyStudySummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        RadiologyModality? modality = null,
        RadiologyStudyStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);
}
