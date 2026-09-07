namespace HospitalManagement.Contracts.Specialty;

public sealed record SpecialtyOperationalSummaryResponse(
    // Obstetrics
    int ActivePregnanciesCount,
    int HighRiskPregnanciesCount,
    int TotalDeliveriesCount,
    int CesareanDeliveriesCount,
    int NormalDeliveriesCount,

    // Odontology
    int TotalDentalProceduresCount,
    int CompletedDentalProceduresCount,
    int PlannedDentalProceduresCount,
    int TotalDentalExaminationsCount,

    // Home Health
    int ActiveHomeVisitsCount,
    int PendingHomeVisitRequestsCount,
    int AssignedHomeVisitsCount,
    int CompletedHomeVisitsCount,
    int UrgentHomeVisitsCount,

    // Timestamp
    DateTime GeneratedAtUtc);
