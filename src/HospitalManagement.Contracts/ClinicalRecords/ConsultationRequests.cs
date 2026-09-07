namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateConsultationRequest
{
    public Guid EncounterId
    {
        get; init;
    }

    public Guid PatientId
    {
        get; init;
    }

    public Guid TargetDepartmentId
    {
        get; init;
    }

    public Guid? TargetPractitionerId
    {
        get; init;
    }

    public string Urgency { get; init; } = "Routine";

    public string ReasonForConsultation { get; init; } = string.Empty;

    public string ClinicalQuestion { get; init; } = string.Empty;
}

public sealed record AcceptConsultationRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record CompleteConsultationRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string ConsultationReport { get; init; } = string.Empty;

    public string? Recommendation
    {
        get; init;
    }
}

public sealed record DeclineConsultationRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = string.Empty;
}

public sealed record CancelConsultationRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = string.Empty;
}

public sealed record MarkConsultationEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı giriş";
}
