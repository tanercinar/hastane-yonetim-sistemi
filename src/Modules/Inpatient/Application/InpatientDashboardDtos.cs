namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record WardOccupancySummaryDto(
    Guid WardId,
    string WardName,
    string WardCode,
    Guid DepartmentId,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    double OccupancyPercentage,
    int ActivePatientsCount);

public sealed record InpatientDashboardDto(
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    double OverallOccupancyPercentage,
    int PendingAdmissionsCount,
    int PendingTransfersCount,
    int TodayDischargesCount,
    List<WardOccupancySummaryDto> Wards);
