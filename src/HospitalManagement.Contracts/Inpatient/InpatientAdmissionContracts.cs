namespace HospitalManagement.Contracts.Inpatient;

public sealed record AdmissionResponse(
    Guid Id,
    string AdmissionNumber,
    Guid PatientId,
    Guid? EncounterId,
    Guid OrderingDoctorId,
    Guid AttendingDoctorId,
    Guid DepartmentId,
    Guid AdmittingWardId,
    string WardName,
    Guid? AssignedBedId,
    string? BedNumber,
    string? RoomNumber,
    string Status,
    string AdmissionReason,
    string? DiagnosisCode,
    string? DiagnosisDescription,
    string DietType,
    int FallRiskScore,
    string IsolationRequired,
    int? EstimatedStayDays,
    DateTime RequestedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? AdmittedAtUtc,
    DateTime? DischargedAtUtc,
    string? DischargeSummary,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    int Version);

public sealed record AdmissionSummaryResponse(
    Guid Id,
    string AdmissionNumber,
    Guid PatientId,
    Guid OrderingDoctorId,
    Guid AttendingDoctorId,
    Guid DepartmentId,
    Guid AdmittingWardId,
    string WardName,
    Guid? AssignedBedId,
    string? BedNumber,
    string? RoomNumber,
    string Status,
    string AdmissionReason,
    string DietType,
    int FallRiskScore,
    string IsolationRequired,
    DateTime RequestedAtUtc,
    DateTime? AdmittedAtUtc);

public sealed record CreateAdmissionRequest
{
    public required Guid PatientId
    {
        get; init;
    }
    public Guid? EncounterId
    {
        get; init;
    }
    public required Guid DepartmentId
    {
        get; init;
    }
    public required Guid AdmittingWardId
    {
        get; init;
    }
    public required Guid AttendingDoctorId
    {
        get; init;
    }
    public required string AdmissionReason
    {
        get; init;
    }
    public string? DiagnosisCode
    {
        get; init;
    }
    public string? DiagnosisDescription
    {
        get; init;
    }
    public string? DietType
    {
        get; init;
    }
    public int FallRiskScore
    {
        get; init;
    }
    public string? IsolationRequired
    {
        get; init;
    }
    public int? EstimatedStayDays
    {
        get; init;
    }
    public Guid? InitialBedId
    {
        get; init;
    }
}

public sealed record AcceptAdmissionRequest
{
    public string? Notes
    {
        get; init;
    }
}

public sealed record AdmitPatientRequest
{
    public required Guid BedId
    {
        get; init;
    }
}

public sealed record CancelAdmissionRequest
{
    public required string Reason
    {
        get; init;
    }
}

public sealed record UpdateCareDetailsRequest
{
    public required Guid AttendingDoctorId
    {
        get; init;
    }
    public required string DietType
    {
        get; init;
    }
    public required int FallRiskScore
    {
        get; init;
    }
    public required string IsolationRequired
    {
        get; init;
    }
}
