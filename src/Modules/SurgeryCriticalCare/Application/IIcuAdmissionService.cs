namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public interface IIcuAdmissionService
{
    Task<List<IcuBedDto>> GetIcuBedsAsync(CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<IcuAdmissionDto>> AdmitToIcuAsync(
        CreateIcuAdmissionDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<IcuAdmissionDto>> UpdateCarePlanAsync(
        Guid admissionId,
        UpdateIcuCarePlanDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<IcuAdmissionDto>> DischargeOrTransferAsync(
        Guid admissionId,
        IcuDischargeOrTransferDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<IcuAdmissionDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<IcuAdmissionDto>> GetActiveAdmissionsAsync(
        CancellationToken cancellationToken = default);
}
