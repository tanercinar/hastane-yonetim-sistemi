namespace HospitalManagement.Contracts.Emergency;

public sealed class CreateEmergencyOrderRequest
{
    public Guid AdmissionId
    {
        get; set;
    }
    public string OrderType { get; set; } = "Laboratory";
    public string OrderCatalogCode { get; set; } = string.Empty;
    public string OrderCatalogName { get; set; } = string.Empty;
    public string Priority { get; set; } = "Stat";
    public string? ClinicalInstructions
    {
        get; set;
    }

    public CreateEmergencyOrderRequest()
    {
    }

    public CreateEmergencyOrderRequest(
        Guid admissionId,
        string orderType,
        string orderCatalogCode,
        string orderCatalogName,
        string priority,
        string? clinicalInstructions)
    {
        AdmissionId = admissionId;
        OrderType = orderType;
        OrderCatalogCode = orderCatalogCode;
        OrderCatalogName = orderCatalogName;
        Priority = priority;
        ClinicalInstructions = clinicalInstructions;
    }
}

public sealed class CompleteEmergencyOrderRequest
{
    public string? ResultSummary
    {
        get; set;
    }

    public CompleteEmergencyOrderRequest()
    {
    }

    public CompleteEmergencyOrderRequest(string? resultSummary)
    {
        ResultSummary = resultSummary;
    }
}

public sealed class CancelEmergencyOrderRequest
{
    public string Reason { get; set; } = string.Empty;

    public CancelEmergencyOrderRequest()
    {
    }

    public CancelEmergencyOrderRequest(string reason)
    {
        Reason = reason;
    }
}

public sealed record EmergencyOrderResponse(
    Guid Id,
    Guid AdmissionId,
    string OrderType,
    string OrderCatalogCode,
    string OrderCatalogName,
    string Priority,
    Guid OrderedByDoctorId,
    DateTime OrderedAtUtc,
    string Status,
    string? ClinicalInstructions,
    string? ResultSummary,
    DateTime? CompletedAtUtc,
    string? CancellationReason,
    uint Version);

public sealed class RequestEmergencyConsultationRequest
{
    public Guid AdmissionId
    {
        get; set;
    }
    public Guid DepartmentId
    {
        get; set;
    }
    public string DepartmentName { get; set; } = string.Empty;
    public string Urgency { get; set; } = "Immediate15Min";
    public string ClinicalReason { get; set; } = string.Empty;

    public RequestEmergencyConsultationRequest()
    {
    }

    public RequestEmergencyConsultationRequest(
        Guid admissionId,
        Guid departmentId,
        string departmentName,
        string urgency,
        string clinicalReason)
    {
        AdmissionId = admissionId;
        DepartmentId = departmentId;
        DepartmentName = departmentName;
        Urgency = urgency;
        ClinicalReason = clinicalReason;
    }
}

public sealed class RespondEmergencyConsultationRequest
{
    public string ResponseNotes { get; set; } = string.Empty;

    public RespondEmergencyConsultationRequest()
    {
    }

    public RespondEmergencyConsultationRequest(string responseNotes)
    {
        ResponseNotes = responseNotes;
    }
}

public sealed class CancelEmergencyConsultationRequest
{
    public string Reason { get; set; } = string.Empty;

    public CancelEmergencyConsultationRequest()
    {
    }

    public CancelEmergencyConsultationRequest(string reason)
    {
        Reason = reason;
    }
}

public sealed record EmergencyConsultationResponse(
    Guid Id,
    Guid AdmissionId,
    Guid DepartmentId,
    string DepartmentName,
    Guid RequestedByDoctorId,
    DateTime RequestedAtUtc,
    string Urgency,
    string ClinicalReason,
    string Status,
    Guid? ConsultantDoctorId,
    string? ConsultationResponseNotes,
    DateTime? RespondedAtUtc,
    string? CancellationReason,
    uint Version);

public sealed class RecordEmergencyDispositionRequest
{
    public string DispositionType { get; set; } = "DischargeHome";
    public Guid? TargetWardOrIcuId
    {
        get; set;
    }
    public string? TargetDepartmentName
    {
        get; set;
    }
    public string DispositionSummaryNotes { get; set; } = string.Empty;
    public string? FollowUpInstructions
    {
        get; set;
    }

    public RecordEmergencyDispositionRequest()
    {
    }

    public RecordEmergencyDispositionRequest(
        string dispositionType,
        Guid? targetWardOrIcuId,
        string? targetDepartmentName,
        string dispositionSummaryNotes,
        string? followUpInstructions)
    {
        DispositionType = dispositionType;
        TargetWardOrIcuId = targetWardOrIcuId;
        TargetDepartmentName = targetDepartmentName;
        DispositionSummaryNotes = dispositionSummaryNotes;
        FollowUpInstructions = followUpInstructions;
    }
}

public sealed record EmergencyDispositionResponse(
    string DispositionType,
    Guid DecidedByDoctorId,
    DateTime DecidedAtUtc,
    Guid? TargetWardOrIcuId,
    string? TargetDepartmentName,
    string DispositionSummaryNotes,
    string? FollowUpInstructions);
