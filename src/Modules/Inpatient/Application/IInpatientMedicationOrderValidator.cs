namespace HospitalManagement.Modules.Inpatient.Application;

/// <summary>
/// Host-owned cross-module port used to prove that an eMAR schedule originates from an active
/// physician prescription/order without allowing Inpatient to read Pharmacy infrastructure.
/// </summary>
public interface IInpatientMedicationOrderValidator
{
    Task<bool> IsActiveOrderAsync(
        Guid prescriptionId,
        Guid patientId,
        string medicationName,
        string dose,
        string route,
        CancellationToken cancellationToken = default);
}
