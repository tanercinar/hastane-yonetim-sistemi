namespace HospitalManagement.Contracts.Inpatient;

public sealed record DischargeAdmissionRequest
{
    public Guid AdmissionId
    {
        get; init;
    }
    public string DischargeType { get; init; } = "Home"; // Home, TransferToOtherFacility, AgainstMedicalAdvice, Deceased
    public string DischargeSummary { get; init; } = string.Empty;
    public string FinalDiagnosisCode { get; init; } = string.Empty;
    public string FinalDiagnosisDescription { get; init; } = string.Empty;
    public string DischargeRecommendations { get; init; } = string.Empty;
    public string? DischargePrescriptionSummary
    {
        get; init;
    }
    public DateTime? FollowUpAppointmentDateUtc
    {
        get; init;
    }
    public Guid? FollowUpDepartmentId
    {
        get; init;
    }
    public string? TransferFacilityName
    {
        get; init;
    }
    public string? TransferReason
    {
        get; init;
    }
}

public sealed record InpatientDischargeResponse(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid DischargingDoctorId,
    string DischargeType,
    string DischargeSummary,
    string FinalDiagnosisCode,
    string FinalDiagnosisDescription,
    string DischargeRecommendations,
    string? DischargePrescriptionSummary,
    DateTime? FollowUpAppointmentDateUtc,
    Guid? FollowUpDepartmentId,
    string? TransferFacilityName,
    string? TransferReason,
    DateTime DischargedAtUtc,
    DateTime CreatedAtUtc,
    int Version);
