using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record InpatientBoardItemDto(
    Guid AdmissionId,
    string AdmissionNumber,
    Guid PatientId,
    string PatientIdentifier,
    string PatientFullName,
    int? PatientAge,
    string? PatientGender,
    Guid WardId,
    string WardName,
    Guid BedId,
    string BedNumber,
    string? RoomNumber,
    Guid AttendingDoctorId,
    string AttendingDoctorName,
    Guid DepartmentId,
    string DiagnosisCode,
    string DiagnosisDescription,
    string DietType,
    int FallRiskScore,
    string FallRiskLevel,
    IsolationType IsolationRequired,
    bool HasPendingTransfer,
    DateTime AdmittedAtUtc,
    int DaysInHospital,
    int? EstimatedStayDays,
    int PendingTasksCount);

public sealed record InpatientPatientSummaryDto(
    Guid AdmissionId,
    string AdmissionNumber,
    Guid PatientId,
    string PatientFullName,
    string WardName,
    string BedNumber,
    string? RoomNumber,
    string AttendingDoctorName,
    string DiagnosisDescription,
    string DietType,
    int FallRiskScore,
    string FallRiskLevel,
    IsolationType IsolationRequired,
    DateTime AdmittedAtUtc,
    int DaysInHospital,
    bool HasPendingTransfer);
