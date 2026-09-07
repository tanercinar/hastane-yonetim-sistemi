using System.Text.Json.Serialization;

namespace HospitalManagement.Modules.Interoperability.Domain.Fhir;

public sealed record FhirMeta(
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("lastUpdated")] DateTime LastUpdated);

public sealed record FhirIdentifier(
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("value")] string Value);

public sealed record FhirHumanName(
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("family")] string Family,
    [property: JsonPropertyName("given")] List<string> Given);

public sealed record FhirContactPoint(
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("use")] string Use);

public sealed record FhirAddress(
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("line")] List<string> Line,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("district")] string District);

public sealed record FhirCodeableConcept(
    [property: JsonPropertyName("coding")] List<FhirCoding> Coding,
    [property: JsonPropertyName("text")] string? Text);

public sealed record FhirCoding(
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("display")] string Display);

public sealed record FhirReference(
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("display")] string? Display);

public sealed record FhirPeriod(
    [property: JsonPropertyName("start")] DateTime? Start,
    [property: JsonPropertyName("end")] DateTime? End);

public sealed record FhirQuantity(
    [property: JsonPropertyName("value")] decimal Value,
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("code")] string Code);
