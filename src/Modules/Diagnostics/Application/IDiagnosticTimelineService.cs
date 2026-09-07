using System.Security.Claims;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IDiagnosticTimelineService
{
    Task<DiagnosticOrderOperationResult<PatientDiagnosticTimelineDto>> GetPatientTimelineAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<PatientPortalResultSummaryDto>>> GetPatientPortalResultsAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);
}
