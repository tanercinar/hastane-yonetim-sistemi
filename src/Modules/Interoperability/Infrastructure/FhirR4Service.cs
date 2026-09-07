using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Fhir;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class FhirR4Service : IFhirR4Service
{
    private readonly IIntegrationMockEngine _mockEngine;
    private readonly TimeProvider _timeProvider;

    public FhirR4Service(
        IIntegrationMockEngine mockEngine,
        TimeProvider timeProvider)
    {
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public FhirCapabilityStatement GetCapabilityStatement()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return new FhirCapabilityStatement(
            ResourceType: "CapabilityStatement",
            Id: "hms-demo-fhir-capability",
            Status: "active",
            Date: now,
            Publisher: "Hastane Yönetim Sistemi — Interoperability Mock Core",
            Kind: "capability",
            Software: new FhirSoftware("HMS FHIR R4 Mock Adapter", "1.0.0"),
            FhirVersion: "4.0.1",
            Format: ["json"]);
    }

    public async Task<FhirPatient?> GetPatientAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "GetPatient",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var patient = new FhirPatient(
                    ResourceType: "Patient",
                    Id: id.ToString(),
                    Meta: new FhirMeta("1", now),
                    Identifier:
                    [
                        new FhirIdentifier("urn:oid:2.16.840.1.113883.2.4.6.3", $"DEMO-PAT-{id.ToString()[..8]}")
                    ],
                    Active: true,
                    Name:
                    [
                        new FhirHumanName("official", "Hasta", ["Demo", "Kullanıcı"])
                    ],
                    Gender: "female",
                    BirthDate: "1992-05-15",
                    Telecom:
                    [
                        new FhirContactPoint("phone", "555-0199", "mobile"),
                        new FhirContactPoint("email", "demo.patient@hospital.invalid", "home")
                    ],
                    Address:
                    [
                        new FhirAddress("home", ["Demo Mahallesi, No: 12"], "İstanbul", "Kadıköy")
                    ]);

                return Task.FromResult<FhirPatient?>(patient);
            },
            payloadSummary: $"{{\"patientId\": \"{id}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR Patient okunamadı.");
        }

        return result.Value;
    }

    public async Task<FhirPractitioner?> GetPractitionerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "GetPractitioner",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var doc = new FhirPractitioner(
                    ResourceType: "Practitioner",
                    Id: id.ToString(),
                    Meta: new FhirMeta("1", now),
                    Identifier:
                    [
                        new FhirIdentifier("urn:oid:2.16.840.1.113883.4.6", $"DEMO-DOC-{id.ToString()[..8]}")
                    ],
                    Active: true,
                    Name:
                    [
                        new FhirHumanName("official", "Hekim", ["Uzm.", "Dr."])
                    ],
                    Gender: "female");

                return Task.FromResult<FhirPractitioner?>(doc);
            },
            payloadSummary: $"{{\"practitionerId\": \"{id}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR Practitioner okunamadı.");
        }

        return result.Value;
    }

    public async Task<FhirObservation?> GetObservationAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "GetObservation",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var obs = new FhirObservation(
                    ResourceType: "Observation",
                    Id: id.ToString(),
                    Meta: new FhirMeta("1", now),
                    Status: "final",
                    Category:
                    [
                        new FhirCodeableConcept(
                        [
                            new FhirCoding("http://terminology.hl7.org/CodeSystem/observation-category", "vital-signs", "Vital Signs")
                        ], "Vital Signs")
                    ],
                    Code: new FhirCodeableConcept(
                    [
                        new FhirCoding("http://loinc.org", "8867-4", "Heart rate")
                    ], "Kalp Atım Hızı"),
                    Subject: new FhirReference($"Patient/{patientId}", "Demo Hasta"),
                    EffectiveDateTime: now,
                    ValueQuantity: new FhirQuantity(78, "beats/minute", "http://unitsofmeasure.org", "/min"),
                    ValueString: null,
                    Interpretation:
                    [
                        new FhirCodeableConcept(
                        [
                            new FhirCoding("http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation", "N", "Normal")
                        ], "Normal")
                    ]);

                return Task.FromResult<FhirObservation?>(obs);
            },
            payloadSummary: $"{{\"observationId\": \"{id}\", \"patientId\": \"{patientId}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR Observation okunamadı.");
        }

        return result.Value;
    }

    public async Task<FhirDiagnosticReport?> GetDiagnosticReportAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "GetDiagnosticReport",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var report = new FhirDiagnosticReport(
                    ResourceType: "DiagnosticReport",
                    Id: id.ToString(),
                    Meta: new FhirMeta("1", now),
                    Status: "final",
                    Category:
                    [
                        new FhirCodeableConcept(
                        [
                            new FhirCoding("http://terminology.hl7.org/CodeSystem/v2-0074", "LAB", "Laboratory")
                        ], "Laboratuvar")
                    ],
                    Code: new FhirCodeableConcept(
                    [
                        new FhirCoding("http://loinc.org", "58410-2", "Complete blood count panel")
                    ], "Tam Kan Sayımı (Hemogram)"),
                    Subject: new FhirReference($"Patient/{patientId}", "Demo Hasta"),
                    EffectiveDateTime: now,
                    Issued: now,
                    Performer:
                    [
                        new FhirReference($"Practitioner/{Guid.NewGuid()}", "Dr. Laboratuvar Uzmanı")
                    ],
                    Result:
                    [
                        new FhirReference($"Observation/{Guid.NewGuid()}", "Hemoglobin Sonucu")
                    ],
                    Conclusion: "Tüm parametreler referans aralığındadır.");

                return Task.FromResult<FhirDiagnosticReport?>(report);
            },
            payloadSummary: $"{{\"reportId\": \"{id}\", \"patientId\": \"{patientId}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR DiagnosticReport okunamadı.");
        }

        return result.Value;
    }

    public async Task<FhirMedicationRequest?> GetMedicationRequestAsync(Guid id, Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "GetMedicationRequest",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var med = new FhirMedicationRequest(
                    ResourceType: "MedicationRequest",
                    Id: id.ToString(),
                    Meta: new FhirMeta("1", now),
                    Status: "active",
                    Intent: "order",
                    MedicationCodeableConcept: new FhirCodeableConcept(
                    [
                        new FhirCoding("http://www.whocc.no/atc", "J01CA04", "Amoxicillin 500mg")
                    ], "Amoksisilin 500mg Tablet"),
                    Subject: new FhirReference($"Patient/{patientId}", "Demo Hasta"),
                    AuthoredOn: now,
                    Requester: new FhirReference($"Practitioner/{Guid.NewGuid()}", "Dr. Tabip"),
                    DosageInstruction:
                    [
                        new FhirDosageInstruction("Günde 2 defa 1 tablet, 12 saatte bir, tok karnına")
                    ]);

                return Task.FromResult<FhirMedicationRequest?>(med);
            },
            payloadSummary: $"{{\"medicationRequestId\": \"{id}\", \"patientId\": \"{patientId}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR MedicationRequest okunamadı.");
        }

        return result.Value;
    }

    public async Task<FhirBundle> GetPatientExportBundleAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Fhir,
            "PatientExportBundle",
            async () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                var patient = await GetPatientAsync(patientId, cancellationToken);
                var obs = await GetObservationAsync(Guid.NewGuid(), patientId, cancellationToken);
                var report = await GetDiagnosticReportAsync(Guid.NewGuid(), patientId, cancellationToken);
                var med = await GetMedicationRequestAsync(Guid.NewGuid(), patientId, cancellationToken);

                var entries = new List<FhirBundleEntry>();

                if (patient is not null)
                {
                    entries.Add(new FhirBundleEntry($"https://hospital.invalid/fhir/r4/Patient/{patient.Id}", patient));
                }

                if (obs is not null)
                {
                    entries.Add(new FhirBundleEntry($"https://hospital.invalid/fhir/r4/Observation/{obs.Id}", obs));
                }

                if (report is not null)
                {
                    entries.Add(new FhirBundleEntry($"https://hospital.invalid/fhir/r4/DiagnosticReport/{report.Id}", report));
                }

                if (med is not null)
                {
                    entries.Add(new FhirBundleEntry($"https://hospital.invalid/fhir/r4/MedicationRequest/{med.Id}", med));
                }

                return new FhirBundle(
                    ResourceType: "Bundle",
                    Id: $"bundle-{Guid.NewGuid():N}",
                    Meta: new FhirMeta("1", now),
                    Type: "collection",
                    Total: entries.Count,
                    Entry: entries);
            },
            payloadSummary: $"{{\"exportPatientId\": \"{patientId}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "FHIR Hasta Export paketi oluşturulamadı.");
        }

        return result.Value ?? new FhirBundle("Bundle", "empty", new FhirMeta("1", _timeProvider.GetUtcNow().UtcDateTime), "collection", 0, []);
    }
}
