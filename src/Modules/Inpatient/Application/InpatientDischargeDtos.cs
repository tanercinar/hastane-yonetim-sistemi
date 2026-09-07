using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record DischargeAdmissionDto(
    Guid AdmissionId,
    DischargeType DischargeType,
    string DischargeSummary,
    string FinalDiagnosisCode,
    string FinalDiagnosisDescription,
    string DischargeRecommendations,
    string? DischargePrescriptionSummary,
    DateTime? FollowUpAppointmentDateUtc,
    Guid? FollowUpDepartmentId,
    string? TransferFacilityName,
    string? TransferReason);

public sealed record InpatientDischargeDto(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid DischargingDoctorId,
    DischargeType DischargeType,
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
