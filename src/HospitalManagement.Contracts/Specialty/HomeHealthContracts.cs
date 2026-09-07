namespace HospitalManagement.Contracts.Specialty;

public sealed record RequestHomeHealthVisitRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public string ServiceType { get; set; } = "GeneralNursing";
    public string Priority { get; set; } = "Routine";
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string AddressDetail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? InitialNotes
    {
        get; set;
    }
}

public sealed record AssignHomeHealthTeamRequest
{
    public Guid AssignedStaffId
    {
        get; set;
    }
    public DateTime ScheduledDateUtc
    {
        get; set;
    }
}

public sealed record CompleteHomeHealthVisitRequest
{
    public string ClinicalNotes { get; set; } = string.Empty;
    public string? VitalsSummaryNotes
    {
        get; set;
    }
    public Guid EncounterId
    {
        get; set;
    }
}

public sealed record CancelHomeHealthVisitRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed record HomeHealthVisitResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProtocolNumber,
    string ServiceType,
    string Priority,
    string Status,
    DateTime RequestedDateUtc,
    DateTime? ScheduledDateUtc,
    DateTime? VisitStartedAtUtc,
    DateTime? VisitCompletedAtUtc,
    string City,
    string District,
    string AddressDetail,
    string ContactPhone,
    Guid RequestedByStaffId,
    Guid? AssignedStaffId,
    string? ClinicalNotes,
    string? VitalsSummaryNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
