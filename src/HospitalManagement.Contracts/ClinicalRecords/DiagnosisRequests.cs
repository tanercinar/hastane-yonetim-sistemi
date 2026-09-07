namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateDiagnosisRequest
{
    public Guid EncounterId
    {
        get; init;
    }

    public Guid PatientId
    {
        get; init;
    }

    public string DiagnosisType { get; init; } = "Preliminary";

    public bool IsCoded
    {
        get; init;
    }

    public string? Icd10Code
    {
        get; init;
    }

    public string DiagnosisTitle { get; init; } = string.Empty;

    public string? Notes
    {
        get; init;
    }
}

public sealed record UpdateDiagnosisRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string DiagnosisType { get; init; } = "Preliminary";

    public bool IsCoded
    {
        get; init;
    }

    public string? Icd10Code
    {
        get; init;
    }

    public string DiagnosisTitle { get; init; } = string.Empty;

    public string? Notes
    {
        get; init;
    }
}

public sealed record MarkDiagnosisEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı giriş";
}
