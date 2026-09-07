using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record InpatientAdmissionDto(
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
    AdmissionStatus Status,
    string AdmissionReason,
    string? DiagnosisCode,
    string? DiagnosisDescription,
    string DietType,
    int FallRiskScore,
    IsolationType IsolationRequired,
    int? EstimatedStayDays,
    DateTime RequestedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? AdmittedAtUtc,
    DateTime? DischargedAtUtc,
    string? DischargeSummary,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    int Version);

public sealed record InpatientAdmissionSummaryDto(
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
    AdmissionStatus Status,
    string AdmissionReason,
    string DietType,
    int FallRiskScore,
    IsolationType IsolationRequired,
    DateTime RequestedAtUtc,
    DateTime? AdmittedAtUtc);

public sealed record CreateAdmissionDto(
    Guid PatientId,
    Guid? EncounterId,
    Guid DepartmentId,
    Guid AdmittingWardId,
    Guid AttendingDoctorId,
    string AdmissionReason,
    string? DiagnosisCode,
    string? DiagnosisDescription,
    string? DietType,
    int FallRiskScore,
    IsolationType IsolationRequired,
    int? EstimatedStayDays,
    Guid? InitialBedId);

public sealed record AcceptAdmissionDto(
    string? Notes);

public sealed record AdmitPatientDto(
    Guid BedId);

public sealed record CancelAdmissionDto(
    string Reason);

public sealed record UpdateCareDetailsDto(
    Guid AttendingDoctorId,
    string DietType,
    int FallRiskScore,
    IsolationType IsolationRequired);
