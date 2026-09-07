using HospitalManagement.Modules.Inpatient.Application;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

internal sealed class RejectingInpatientMedicationOrderValidator : IInpatientMedicationOrderValidator
{
    public Task<bool> IsActiveOrderAsync(
        Guid prescriptionId,
        Guid patientId,
        string medicationName,
        string dose,
        string route,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
