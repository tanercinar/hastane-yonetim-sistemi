using System.Text.Json;
using HospitalManagement.Modules.Interoperability.Domain.Fhir;
using Xunit;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class FhirR4DomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G02")]
    public void FhirPatientInitializesAndSerializesToValidFhirR4Json()
    {
        var patient = new FhirPatient(
            ResourceType: "Patient",
            Id: "demo-pat-12345",
            Meta: new FhirMeta("1", DateTime.UtcNow),
            Identifier:
            [
                new FhirIdentifier("urn:oid:2.16.840.1.113883.2.4.6.3", "DEMO-PAT-12345")
            ],
            Active: true,
            Name:
            [
                new FhirHumanName("official", "Yilmaz", ["Ayse", "Fatma"])
            ],
            Gender: "female",
            BirthDate: "1990-01-01",
            Telecom:
            [
                new FhirContactPoint("phone", "555-1234", "mobile")
            ],
            Address:
            [
                new FhirAddress("home", ["Ornek Mah. No: 5"], "Istanbul", "Kadikoy")
            ]);

        Assert.Equal("Patient", patient.ResourceType);
        Assert.Equal("demo-pat-12345", patient.Id);
        Assert.True(patient.Active);
        Assert.Equal("female", patient.Gender);

        var json = JsonSerializer.Serialize(patient);
        Assert.Contains("\"resourceType\":\"Patient\"", json, StringComparison.Ordinal);
        Assert.Contains("\"family\":\"Yilmaz\"", json, StringComparison.Ordinal);
        Assert.Contains("\"DEMO-PAT-12345\"", json, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G02")]
    public void FhirBundlePacksResourcesWithCollectionType()
    {
        var patient = new FhirPatient(
            ResourceType: "Patient",
            Id: "pat-1",
            Meta: new FhirMeta("1", DateTime.UtcNow),
            Identifier: [new FhirIdentifier("system", "DEMO-PAT-1")],
            Active: true,
            Name: [new FhirHumanName("official", "Demir", ["Mehmet"])],
            Gender: "male",
            BirthDate: "1985-03-20",
            Telecom: [],
            Address: []);

        var bundle = new FhirBundle(
            ResourceType: "Bundle",
            Id: "bundle-123",
            Meta: new FhirMeta("1", DateTime.UtcNow),
            Type: "collection",
            Total: 1,
            Entry:
            [
                new FhirBundleEntry("https://hospital.invalid/fhir/r4/Patient/pat-1", patient)
            ]);

        Assert.Equal("Bundle", bundle.ResourceType);
        Assert.Equal("collection", bundle.Type);
        Assert.Equal(1, bundle.Total);
        Assert.Single(bundle.Entry);

        var json = JsonSerializer.Serialize(bundle);
        Assert.Contains("\"resourceType\":\"Bundle\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"collection\"", json, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G02")]
    public void FhirObservationHoldsLoincCodeAndVitalQuantity()
    {
        var now = DateTime.UtcNow;
        var obs = new FhirObservation(
            ResourceType: "Observation",
            Id: "obs-99",
            Meta: new FhirMeta("1", now),
            Status: "final",
            Category:
            [
                new FhirCodeableConcept([new FhirCoding("http://terminology.hl7.org/CodeSystem/observation-category", "vital-signs", "Vital Signs")], "Vital Signs")
            ],
            Code: new FhirCodeableConcept([new FhirCoding("http://loinc.org", "8867-4", "Heart rate")], "Kalp Hızı"),
            Subject: new FhirReference("Patient/pat-1", "Mehmet Demir"),
            EffectiveDateTime: now,
            ValueQuantity: new FhirQuantity(72, "beats/minute", "http://unitsofmeasure.org", "/min"),
            ValueString: null,
            Interpretation: null);

        Assert.Equal("Observation", obs.ResourceType);
        Assert.Equal("final", obs.Status);
        Assert.NotNull(obs.ValueQuantity);
        Assert.Equal(72, obs.ValueQuantity.Value);
        Assert.Equal("/min", obs.ValueQuantity.Code);
    }
}
