namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record SpecialtyOperationalSummaryDto(
    int ActivePregnanciesCount,
    int HighRiskPregnanciesCount,
    int TotalDeliveriesCount,
    int CesareanDeliveriesCount,
    int NormalDeliveriesCount,
    int TotalDentalProceduresCount,
    int CompletedDentalProceduresCount,
    int PlannedDentalProceduresCount,
    int TotalDentalExaminationsCount,
    int ActiveHomeVisitsCount,
    int PendingHomeVisitRequestsCount,
    int AssignedHomeVisitsCount,
    int CompletedHomeVisitsCount,
    int UrgentHomeVisitsCount,
    DateTime GeneratedAtUtc);
