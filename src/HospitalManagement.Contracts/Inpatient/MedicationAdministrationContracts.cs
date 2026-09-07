namespace HospitalManagement.Contracts.Inpatient;

public sealed record MedicationAdministrationResponse(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid? PrescriptionId,
    string MedicationName,
    string Dose,
    string Route,
    DateTime ScheduledTimeUtc,
    string Status,
    Guid? AdministeredByNurseId,
    DateTime? AdministeredAtUtc,
    bool Verified5Rights,
    string? Reason,
    string? Notes,
    DateTime CreatedAtUtc,
    int Version);

public sealed record ActiveMedicationOrderResponse(
    Guid PrescriptionId,
    Guid PrescriptionItemId,
    string MedicationName,
    string Dose,
    string Route,
    DateTime? ValidUntilUtc);

public sealed record ScheduleMedicationRequest
{
    public Guid AdmissionId
    {
        get; init;
    }
    public Guid? PrescriptionId
    {
        get; init;
    }
    public string MedicationName { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Route { get; init; } = "Oral";
    public DateTime ScheduledTimeUtc
    {
        get; init;
    }
}

public sealed record AdministerMedicationRequest
{
    public bool Verified5Rights
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record SkipMedicationRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record RefuseMedicationRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record DelayMedicationRequest
{
    public DateTime NewScheduledTimeUtc
    {
        get; init;
    }
    public string Reason { get; init; } = string.Empty;
}
