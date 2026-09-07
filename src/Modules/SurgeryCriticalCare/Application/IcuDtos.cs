using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public sealed record IcuBedDto(
    Guid Id,
    string BedCode,
    string BedName,
    string UnitName,
    bool IsActive,
    bool IsOccupied,
    Guid? CurrentAdmissionId,
    string? CurrentPatientProtocolNumber);

public sealed record CreateIcuAdmissionDto(
    Guid InpatientStayId,
    Guid PatientId,
    Guid? EncounterId,
    Guid IcuBedId,
    Guid AttendingDoctorId,
    Guid? PrimaryNurseId,
    string AdmissionReason,
    IcuAcuityLevel AcuityLevel,
    int MonitoringFrequencyMinutes,
    IcuVentilationMode VentilationMode,
    string? CarePlanNotes);

public sealed record UpdateIcuCarePlanDto(
    IcuAcuityLevel AcuityLevel,
    int MonitoringFrequencyMinutes,
    IcuVentilationMode VentilationMode,
    Guid? PrimaryNurseId,
    string? CarePlanNotes);

public sealed record IcuDischargeOrTransferDto(
    IcuAdmissionStatus DestinationStatus,
    string DischargeNotes);

public sealed record IcuAdmissionDto(
    Guid Id,
    string AdmissionProtocolNumber,
    Guid InpatientStayId,
    Guid PatientId,
    Guid? EncounterId,
    Guid IcuBedId,
    string IcuBedCode,
    Guid AttendingDoctorId,
    Guid? PrimaryNurseId,
    string AdmissionReason,
    IcuAcuityLevel AcuityLevel,
    int MonitoringFrequencyMinutes,
    IcuVentilationMode VentilationMode,
    IcuAdmissionStatus Status,
    string? CarePlanNotes,
    DateTime AdmittedAtUtc,
    DateTime? DischargedAtUtc,
    string? DischargeNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
