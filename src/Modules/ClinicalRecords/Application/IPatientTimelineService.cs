using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IPatientTimelineService
{
    Task<ClinicalEncounterOperationResult<PatientTimelinePagedDto>> GetPatientTimelineAsync(
        ClaimsPrincipal actor,
        GetPatientTimelineQuery query,
        CancellationToken cancellationToken = default);
}
