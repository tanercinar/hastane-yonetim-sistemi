using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record MedicationAdministrationDto(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid? PrescriptionId,
    string MedicationName,
    string Dose,
    string Route,
    DateTime ScheduledTimeUtc,
    MedicationAdministrationStatus Status,
    Guid? AdministeredByNurseId,
    DateTime? AdministeredAtUtc,
    bool Verified5Rights,
    string? Reason,
    string? Notes,
    DateTime CreatedAtUtc,
    int Version);

public sealed record ScheduleMedicationDto(
    Guid AdmissionId,
    Guid? PrescriptionId,
    string MedicationName,
    string Dose,
    string Route,
    DateTime ScheduledTimeUtc);

public sealed record AdministerMedicationDto(
    bool Verified5Rights,
    string? Notes);

public sealed record SkipMedicationDto(
    string Reason);

public sealed record RefuseMedicationDto(
    string Reason);

public sealed record DelayMedicationDto(
    DateTime NewScheduledTimeUtc,
    string Reason);
