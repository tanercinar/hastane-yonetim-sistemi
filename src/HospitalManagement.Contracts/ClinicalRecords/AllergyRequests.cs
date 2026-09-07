namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateAllergyRequest
{
    public Guid PatientId
    {
        get; init;
    }

    public Guid? EncounterId
    {
        get; init;
    }

    public string Substance { get; init; } = string.Empty;

    public string Category { get; init; } = "Medication";

    public string Criticality { get; init; } = "Low";

    public string? Manifestation
    {
        get; init;
    }

    public DateTime? OnsetDateTimeUtc
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record UpdateAllergyStatusRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string ClinicalStatus { get; init; } = "Active";

    public string? Notes
    {
        get; init;
    }
}

public sealed record MarkAllergyEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı giriş";
}
