namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateClinicalProblemRequest
{
    public Guid PatientId
    {
        get; init;
    }

    public Guid? EncounterId
    {
        get; init;
    }

    public string ProblemTitle { get; init; } = string.Empty;

    public string? Code
    {
        get; init;
    }

    public string Category { get; init; } = "ActiveProblem";

    public DateOnly? OnsetDate
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record UpdateClinicalProblemStatusRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string ClinicalStatus { get; init; } = "Active";

    public DateOnly? ResolvedDate
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record MarkClinicalProblemEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı giriş";
}
