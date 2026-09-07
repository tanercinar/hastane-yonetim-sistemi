namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientDischargeService
{
    Task<InpatientOperationResult<InpatientDischargeDto>> ProcessDischargeAsync(
        DischargeAdmissionDto request,
        Guid dischargingDoctorId,
        CancellationToken cancellationToken = default);

    Task<InpatientDischargeDto?> GetDischargeByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<InpatientDischargeDto>> GetDischargesAsync(
        Guid? patientId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default);
}
