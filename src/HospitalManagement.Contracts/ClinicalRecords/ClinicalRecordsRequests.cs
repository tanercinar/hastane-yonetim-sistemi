namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateEncounterRequest
{
    public Guid? AppointmentId
    {
        get; init;
    }

    public Guid PatientId
    {
        get; init;
    }

    public Guid DepartmentId
    {
        get; init;
    }

    public Guid PrimaryPractitionerId
    {
        get; init;
    }

    public string EncounterType { get; init; } = "Outpatient";

    public DateTime? PlannedStartTimeUtc
    {
        get; init;
    }

    public string? ChiefComplaint
    {
        get; init;
    }
}

public sealed record StartEncounterRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public DateTime? StartTimeUtc
    {
        get; init;
    }
}

public sealed record CompleteEncounterRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public DateTime? EndTimeUtc
    {
        get; init;
    }

    public string? Summary
    {
        get; init;
    }
}

public sealed record ReopenEncounterRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = string.Empty;
}

public sealed record CancelEncounterRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hizmet verilmedi";
}

public sealed record MarkEncounterEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı kayıt";
}

public sealed record AddEncounterParticipantRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public Guid PractitionerId
    {
        get; init;
    }

    public string Role { get; init; } = "Secondary";
}
