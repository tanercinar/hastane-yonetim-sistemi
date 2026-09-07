namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientBoardService
{
    Task<List<InpatientBoardItemDto>> GetInpatientBoardAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? riskLevel = null,
        bool? isolationOnly = null,
        CancellationToken cancellationToken = default);

    Task<InpatientPatientSummaryDto?> GetPatientSummaryAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);
}
