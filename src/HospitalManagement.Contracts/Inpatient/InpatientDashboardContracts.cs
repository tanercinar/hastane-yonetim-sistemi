namespace HospitalManagement.Contracts.Inpatient;

public sealed record WardOccupancySummaryItem(
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

public sealed record InpatientDashboardResponse(
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    double OverallOccupancyPercentage,
    int PendingAdmissionsCount,
    int PendingTransfersCount,
    int TodayDischargesCount,
    List<WardOccupancySummaryItem> Wards);

public sealed record InpatientRealtimeBedUpdate(
    Guid BedId,
    Guid WardId,
    string OldStatus,
    string NewStatus,
    Guid? AdmissionId,
    DateTime TimestampUtc);

public sealed record InpatientRealtimeAdmissionUpdate(
    Guid AdmissionId,
    Guid WardId,
    string OldStatus,
    string NewStatus,
    DateTime TimestampUtc);

public sealed record InpatientRealtimeTransferUpdate(
    Guid TransferId,
    Guid SourceWardId,
    Guid TargetWardId,
    string Status,
    DateTime TimestampUtc);

public sealed record InpatientRealtimeDischargeUpdate(
    Guid DischargeId,
    Guid AdmissionId,
    Guid WardId,
    string DischargeType,
    DateTime TimestampUtc);
