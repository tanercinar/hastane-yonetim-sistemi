using HospitalManagement.Modules.Interoperability.Domain.Fhir;

namespace HospitalManagement.Modules.Interoperability.Application;

public interface IFhirR4Service
{
    FhirCapabilityStatement GetCapabilityStatement();
    Task<FhirPatient?> GetPatientAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FhirPractitioner?> GetPractitionerAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FhirObservation?> GetObservationAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default);
    Task<FhirDiagnosticReport?> GetDiagnosticReportAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default);
    Task<FhirMedicationRequest?> GetMedicationRequestAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default);
    Task<FhirBundle> GetPatientExportBundleAsync(Guid patientId, CancellationToken cancellationToken = default);
}
