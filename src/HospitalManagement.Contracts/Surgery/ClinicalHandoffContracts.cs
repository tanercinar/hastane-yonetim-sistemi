namespace HospitalManagement.Contracts.Surgery;

public sealed class InitiateClinicalHandoffRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public Guid? InpatientStayId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public string SourceArea { get; set; } = "Emergency";
    public string SourceLocationDetails { get; set; } = string.Empty;
    public string DestinationArea { get; set; } = "IntensiveCareUnit";
    public string DestinationLocationDetails { get; set; } = string.Empty;

    // ISBAR
    public string Situation { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? CriticalAlerts
    {
        get; set;
    }

    public InitiateClinicalHandoffRequest()
    {
    }

    public InitiateClinicalHandoffRequest(
        Guid patientId,
        Guid? inpatientStayId,
        Guid? encounterId,
        string sourceArea,
        string sourceLocationDetails,
        string destinationArea,
        string destinationLocationDetails,
        string situation,
        string background,
        string assessment,
        string recommendation,
        string? criticalAlerts)
    {
        PatientId = patientId;
        InpatientStayId = inpatientStayId;
        EncounterId = encounterId;
        SourceArea = sourceArea;
        SourceLocationDetails = sourceLocationDetails;
        DestinationArea = destinationArea;
        DestinationLocationDetails = destinationLocationDetails;
        Situation = situation;
        Background = background;
        Assessment = assessment;
        Recommendation = recommendation;
        CriticalAlerts = criticalAlerts;
    }
}

public sealed class AcceptClinicalHandoffRequest
{
    public string? AcceptanceNote
    {
        get; set;
    }

    public AcceptClinicalHandoffRequest()
    {
    }

    public AcceptClinicalHandoffRequest(string? acceptanceNote)
    {
        AcceptanceNote = acceptanceNote;
    }
}

public sealed class RejectClinicalHandoffRequest
{
    public string RejectionReason { get; set; } = string.Empty;

    public RejectClinicalHandoffRequest()
    {
    }

    public RejectClinicalHandoffRequest(string rejectionReason)
    {
        RejectionReason = rejectionReason;
    }
}

public sealed class CancelClinicalHandoffRequest
{
    public string CancelReason { get; set; } = string.Empty;

    public CancelClinicalHandoffRequest()
    {
    }

    public CancelClinicalHandoffRequest(string cancelReason)
    {
        CancelReason = cancelReason;
    }
}

public sealed record ClinicalHandoffResponse(
    Guid Id,
    string HandoffProtocolNumber,
    Guid PatientId,
    Guid? InpatientStayId,
    Guid? EncounterId,
    string SourceArea,
    string SourceLocationDetails,
    string DestinationArea,
    string DestinationLocationDetails,
    Guid HandingOverStaffId,
    Guid? ReceivingStaffId,
    string Situation,
    string Background,
    string Assessment,
    string Recommendation,
    string? CriticalAlerts,
    string Status,
    string? StatusReason,
    DateTime HandedOverAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
