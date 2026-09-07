namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record PatientSpecialtyPortalDto(
    IReadOnlyList<PatientPregnancySummaryDto> Pregnancies,
    IReadOnlyList<PatientDeliverySummaryDto> Deliveries,
    IReadOnlyList<PatientDentalExaminationSummaryDto> DentalExaminations,
    IReadOnlyList<PatientDentalProcedureSummaryDto> DentalProcedures,
    IReadOnlyList<PatientHomeHealthVisitSummaryDto> HomeHealthVisits,
    DateTime GeneratedAtUtc);

public sealed record PatientPregnancySummaryDto(
    Guid Id,
    string ProtocolNumber,
    string Status,
    DateTime EstimatedDeliveryDateUtc,
    int VisitCount,
    DateTime? LastVisitDateUtc);

public sealed record PatientDeliverySummaryDto(
    Guid Id,
    string ProtocolNumber,
    string DeliveryMode,
    DateTime DeliveryTimeUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    int NewbornCount);

public sealed record PatientDentalExaminationSummaryDto(
    Guid Id,
    string ProtocolNumber,
    DateTime ExaminationDateUtc);

public sealed record PatientDentalProcedureSummaryDto(
    Guid Id,
    string ProtocolNumber,
    int? ToothNumber,
    string ProcedureName,
    string Status,
    DateTime? CompletedDateUtc);

public sealed record PatientHomeHealthVisitSummaryDto(
    Guid Id,
    string ProtocolNumber,
    string ServiceType,
    string Priority,
    string Status,
    DateTime RequestedDateUtc,
    DateTime? ScheduledDateUtc,
    DateTime? VisitCompletedAtUtc,
    string City,
    string District);
