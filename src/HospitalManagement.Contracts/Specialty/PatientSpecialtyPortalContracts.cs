namespace HospitalManagement.Contracts.Specialty;

public sealed record PatientSpecialtyPortalResponse(
    IReadOnlyList<PatientPregnancySummaryResponse> Pregnancies,
    IReadOnlyList<PatientDeliverySummaryResponse> Deliveries,
    IReadOnlyList<PatientDentalExaminationSummaryResponse> DentalExaminations,
    IReadOnlyList<PatientDentalProcedureSummaryResponse> DentalProcedures,
    IReadOnlyList<PatientHomeHealthVisitSummaryResponse> HomeHealthVisits,
    DateTime GeneratedAtUtc);

public sealed record PatientPregnancySummaryResponse(Guid Id, string ProtocolNumber, string Status, DateTime EstimatedDeliveryDateUtc, int VisitCount, DateTime? LastVisitDateUtc);
public sealed record PatientDeliverySummaryResponse(Guid Id, string ProtocolNumber, string DeliveryMode, DateTime DeliveryTimeUtc, int GestationalAgeWeeks, int GestationalAgeDays, int NewbornCount);
public sealed record PatientDentalExaminationSummaryResponse(Guid Id, string ProtocolNumber, DateTime ExaminationDateUtc);
public sealed record PatientDentalProcedureSummaryResponse(Guid Id, string ProtocolNumber, int? ToothNumber, string ProcedureName, string Status, DateTime? CompletedDateUtc);
public sealed record PatientHomeHealthVisitSummaryResponse(Guid Id, string ProtocolNumber, string ServiceType, string Priority, string Status, DateTime RequestedDateUtc, DateTime? ScheduledDateUtc, DateTime? VisitCompletedAtUtc, string City, string District);
