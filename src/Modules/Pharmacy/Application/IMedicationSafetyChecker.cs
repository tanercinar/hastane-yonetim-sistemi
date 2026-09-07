namespace HospitalManagement.Modules.Pharmacy.Application;

public interface IMedicationSafetyChecker
{
    Task<MedicationSafetyCheckResult> CheckSafetyAsync(
        Guid patientId,
        IReadOnlyList<PrescriptionItemSafetyCandidate> items,
        Guid? currentPrescriptionId = null,
        CancellationToken cancellationToken = default);
}
