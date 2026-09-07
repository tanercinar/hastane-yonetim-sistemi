namespace HospitalManagement.Modules.Reporting.Application;

public sealed record DailyOutpatientMetricDto(
    Guid Id,
    DateOnly Date,
    Guid DepartmentId,
    string DepartmentName,
    Guid? DoctorId,
    string? DoctorName,
    int TotalAppointments,
    int ScheduledCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    DateTime LastUpdatedUtc);

public sealed record OutpatientDashboardSummaryDto(
    DateOnly Date,
    int TotalAppointments,
    int ScheduledCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    int WaitingQueueCount,
    DateTime LastUpdatedUtc);

public sealed record OutpatientDepartmentMetricDto(
    Guid DepartmentId,
    string DepartmentName,
    int TotalAppointments,
    int ScheduledCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount);

public sealed record OutpatientDoctorMetricDto(
    Guid DoctorId,
    string DoctorName,
    Guid DepartmentId,
    string DepartmentName,
    int TotalAppointments,
    int CompletedCount,
    int WaitingCount);

public sealed record DiagnosticWorkloadMetricDto(
    Guid Id,
    DateOnly Date,
    string ModalityOrSection,
    int TotalOrders,
    int PendingSpecimenCount,
    int ProcessingCount,
    int FinalizedCount,
    int CriticalCount,
    double AvgTurnaroundMinutes,
    DateTime LastUpdatedUtc);

public sealed record BedOccupancyMetricDto(
    Guid Id,
    DateOnly Date,
    Guid DepartmentId,
    string DepartmentName,
    string WardType,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    int PendingTransferCount,
    double OccupancyRatePercentage,
    DateTime LastUpdatedUtc);

public sealed record PharmacyDispensingMetricDto(
    Guid Id,
    DateOnly Date,
    int TotalPrescriptions,
    int PendingDispenseCount,
    int DispensedCount,
    int LowStockItemCount,
    int NearExpiryLotCount,
    DateTime LastUpdatedUtc);

public sealed record ProjectionCheckpointDto(
    string ProjectionName,
    long LastProcessedPosition,
    DateTime LastProcessedTimestampUtc,
    string Status,
    int Version,
    string? LastError);

public sealed record RebuildSummaryDto(
    bool Success,
    int TotalProjectionsRebuilt,
    IReadOnlyList<string> RebuiltProjections,
    DateTime CompletedAtUtc,
    string Message);

public sealed record DiagnosticDashboardSummaryDto(
    DateOnly Date,
    int TotalOrders,
    int PendingSpecimenCount,
    int ProcessingCount,
    int FinalizedCount,
    int CriticalCount,
    double AvgTurnaroundMinutes,
    DateTime LastUpdatedUtc);

public sealed record DiagnosticModalityMetricDto(
    string ModalityOrSection,
    int TotalOrders,
    int PendingSpecimenCount,
    int ProcessingCount,
    int FinalizedCount,
    int CriticalCount,
    double AvgTurnaroundMinutes);

public sealed record DiagnosticCriticalAlertMetricDto(
    Guid MetricId,
    DateOnly Date,
    string ModalityOrSection,
    int CriticalCount,
    DateTime LastUpdatedUtc);

public sealed record InpatientOperationsSummaryDto(
    DateOnly Date,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    double OverallOccupancyRate,
    int PendingTransfers,
    int EmergencyWaitingCount,
    int OperatingRoomsInUse,
    DateTime LastUpdatedUtc);

public sealed record WardOccupancyDetailDto(
    Guid DepartmentId,
    string DepartmentName,
    string WardType,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    double OccupancyRatePercentage,
    int PendingTransfers);

public sealed record EmergencyTriageQueueMetricDto(
    string TriageCategory,
    int WaitingCount,
    int AvgWaitMinutes);

public sealed record PharmacyDashboardSummaryDto(
    DateOnly Date,
    int TotalPrescriptions,
    int PendingDispenseCount,
    int DispensedCount,
    int LowStockItemCount,
    int NearExpiryLotCount,
    DateTime LastUpdatedUtc);

public sealed record PharmacyStockAlertMetricDto(
    string AlertType,
    string ItemName,
    int CurrentStock,
    int MinimumThreshold,
    string Unit);

public sealed record ProjectionLagDto(
    string ProjectionName,
    long LastProcessedPosition,
    DateTime LastProcessedTimestampUtc,
    double LagSeconds,
    bool IsHealthy,
    DateTime CheckedAtUtc);

