namespace HospitalManagement.Modules.Inpatient.Application;

public interface IMedicationAdministrationService
{
    Task<InpatientOperationResult<MedicationAdministrationDto>> ScheduleDoseAsync(
        ScheduleMedicationDto request,
        Guid scheduledByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<MedicationAdministrationDto>> AdministerMedicationAsync(
        Guid administrationId,
        AdministerMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<MedicationAdministrationDto>> SkipMedicationAsync(
        Guid administrationId,
        SkipMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<MedicationAdministrationDto>> RefuseMedicationAsync(
        Guid administrationId,
        RefuseMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<MedicationAdministrationDto>> DelayMedicationAsync(
        Guid administrationId,
        DelayMedicationDto request,
        Guid nurseId,
        CancellationToken cancellationToken = default);

    Task<List<MedicationAdministrationDto>> GetAdministrationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<MedicationAdministrationDto>> GetDueAdministrationsAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default);
}
