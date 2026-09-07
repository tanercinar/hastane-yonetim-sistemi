using System.Text.Json.Serialization;

namespace HospitalManagement.Modules.Interoperability.Domain.Fhir;

public sealed record FhirPatient(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("identifier")] List<FhirIdentifier> Identifier,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("name")] List<FhirHumanName> Name,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("birthDate")] string? BirthDate,
    [property: JsonPropertyName("telecom")] List<FhirContactPoint> Telecom,
    [property: JsonPropertyName("address")] List<FhirAddress> Address);

public sealed record FhirPractitioner(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("identifier")] List<FhirIdentifier> Identifier,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("name")] List<FhirHumanName> Name,
    [property: JsonPropertyName("gender")] string? Gender);

public sealed record FhirAppointment(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("serviceCategory")] List<FhirCodeableConcept> ServiceCategory,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("start")] DateTime Start,
    [property: JsonPropertyName("end")] DateTime End,
    [property: JsonPropertyName("participant")] List<FhirAppointmentParticipant> Participant);

public sealed record FhirAppointmentParticipant(
    [property: JsonPropertyName("actor")] FhirReference Actor,
    [property: JsonPropertyName("status")] string Status);

public sealed record FhirEncounter(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("class")] FhirCoding Class,
    [property: JsonPropertyName("subject")] FhirReference Subject,
    [property: JsonPropertyName("period")] FhirPeriod Period,
    [property: JsonPropertyName("reasonCode")] List<FhirCodeableConcept> ReasonCode);

public sealed record FhirObservation(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("category")] List<FhirCodeableConcept> Category,
    [property: JsonPropertyName("code")] FhirCodeableConcept Code,
    [property: JsonPropertyName("subject")] FhirReference Subject,
    [property: JsonPropertyName("effectiveDateTime")] DateTime EffectiveDateTime,
    [property: JsonPropertyName("valueQuantity")] FhirQuantity? ValueQuantity,
    [property: JsonPropertyName("valueString")] string? ValueString,
    [property: JsonPropertyName("interpretation")] List<FhirCodeableConcept>? Interpretation);

public sealed record FhirDiagnosticReport(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("category")] List<FhirCodeableConcept> Category,
    [property: JsonPropertyName("code")] FhirCodeableConcept Code,
    [property: JsonPropertyName("subject")] FhirReference Subject,
    [property: JsonPropertyName("effectiveDateTime")] DateTime EffectiveDateTime,
    [property: JsonPropertyName("issued")] DateTime Issued,
    [property: JsonPropertyName("performer")] List<FhirReference> Performer,
    [property: JsonPropertyName("result")] List<FhirReference> Result,
    [property: JsonPropertyName("conclusion")] string? Conclusion);

public sealed record FhirMedicationRequest(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("intent")] string Intent,
    [property: JsonPropertyName("medicationCodeableConcept")] FhirCodeableConcept MedicationCodeableConcept,
    [property: JsonPropertyName("subject")] FhirReference Subject,
    [property: JsonPropertyName("authoredOn")] DateTime AuthoredOn,
    [property: JsonPropertyName("requester")] FhirReference? Requester,
    [property: JsonPropertyName("dosageInstruction")] List<FhirDosageInstruction> DosageInstruction);

public sealed record FhirDosageInstruction(
    [property: JsonPropertyName("text")] string Text);

public sealed record FhirBundleEntry(
    [property: JsonPropertyName("fullUrl")] string FullUrl,
    [property: JsonPropertyName("resource")] object Resource);

public sealed record FhirBundle(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("meta")] FhirMeta Meta,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("entry")] List<FhirBundleEntry> Entry);

public sealed record FhirCapabilityStatement(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("publisher")] string Publisher,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("software")] FhirSoftware Software,
    [property: JsonPropertyName("fhirVersion")] string FhirVersion,
    [property: JsonPropertyName("format")] List<string> Format);

public sealed record FhirSoftware(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);
