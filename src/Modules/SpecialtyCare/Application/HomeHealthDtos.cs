using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;

namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record RequestHomeHealthVisitDto(
    Guid PatientId,
    HomeCareServiceType ServiceType,
    HomeVisitPriority Priority,
    string City,
    string District,
    string AddressDetail,
    string ContactPhone,
    string? InitialNotes);

public sealed record HomeHealthVisitDto(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProtocolNumber,
    HomeCareServiceType ServiceType,
    HomeVisitPriority Priority,
    HomeVisitStatus Status,
    DateTime RequestedDateUtc,
    DateTime? ScheduledDateUtc,
    DateTime? VisitStartedAtUtc,
    DateTime? VisitCompletedAtUtc,
    string City,
    string District,
    string AddressDetail,
    string ContactPhone,
    Guid RequestedByStaffId,
    Guid? AssignedStaffId,
    string? ClinicalNotes,
    string? VitalsSummaryNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
