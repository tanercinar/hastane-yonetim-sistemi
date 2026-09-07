namespace HospitalManagement.Contracts.Reporting;

public sealed record RebuildProjectionsRequest
{
    public string? ProjectionName
    {
        get; set;
    }
}

public sealed record RebuildProjectionsResponse(
    bool Success,
    int TotalProjectionsRebuilt,
    IReadOnlyList<string> RebuiltProjections,
    DateTime CompletedAtUtc,
    string Message);

public sealed record ProjectionCheckpointResponse(
    string ProjectionName,
    long LastProcessedPosition,
    DateTime LastProcessedTimestampUtc,
    string Status,
    int Version,
    string? LastError);

public sealed record DailyOutpatientMetricResponse(
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

public sealed record OutpatientDashboardSummaryResponse(
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

public sealed record OutpatientDepartmentMetricResponse(
    Guid DepartmentId,
    string DepartmentName,
    int TotalAppointments,
    int ScheduledCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount);

public sealed record OutpatientDoctorMetricResponse(
    Guid DoctorId,
    string DoctorName,
    Guid DepartmentId,
    string DepartmentName,
    int TotalAppointments,
    int CompletedCount,
    int WaitingCount);

public sealed record OutpatientQueueItemResponse(
    Guid AppointmentId,
    TimeOnly Time,
    string TokenNumber,
    string MaskedPatientIdentifier,
    Guid DepartmentId,
    string DepartmentName,
    Guid? DoctorId,
    string? DoctorName,
    string Status,
    int WaitMinutes);

public sealed record DiagnosticWorkloadMetricResponse(
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

public sealed record BedOccupancyMetricResponse(
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

public sealed record PharmacyDispensingMetricResponse(
    Guid Id,
    DateOnly Date,
    int TotalPrescriptions,
    int PendingDispenseCount,
    int DispensedCount,
    int LowStockItemCount,
    int NearExpiryLotCount,
    DateTime LastUpdatedUtc);

public sealed record DiagnosticDashboardSummaryResponse(
    DateOnly Date,
    int TotalOrders,
    int PendingSpecimenCount,
    int ProcessingCount,
    int FinalizedCount,
    int CriticalCount,
    double AvgTurnaroundMinutes,
    DateTime LastUpdatedUtc);

public sealed record DiagnosticModalityMetricResponse(
    string ModalityOrSection,
    int TotalOrders,
    int PendingSpecimenCount,
    int ProcessingCount,
    int FinalizedCount,
    int CriticalCount,
    double AvgTurnaroundMinutes);

public sealed record DiagnosticCriticalAlertMetricResponse(
    Guid MetricId,
    DateOnly Date,
    string ModalityOrSection,
    int CriticalCount,
    DateTime LastUpdatedUtc);

public sealed record InpatientOperationsSummaryResponse(
    DateOnly Date,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    double OverallOccupancyRate,
    int PendingTransfers,
    int EmergencyWaitingCount,
    int OperatingRoomsInUse,
    DateTime LastUpdatedUtc);

public sealed record WardOccupancyDetailResponse(
    Guid DepartmentId,
    string DepartmentName,
    string WardType,
    int TotalBeds,
    int OccupiedBeds,
    int AvailableBeds,
    double OccupancyRatePercentage,
    int PendingTransfers);

public sealed record EmergencyTriageQueueMetricResponse(
    string TriageCategory,
    int WaitingCount,
    int AvgWaitMinutes);

public sealed record PharmacyDashboardSummaryResponse(
    DateOnly Date,
    int TotalPrescriptions,
    int PendingDispenseCount,
    int DispensedCount,
    int LowStockItemCount,
    int NearExpiryLotCount,
    DateTime LastUpdatedUtc);

public sealed record PharmacyStockAlertMetricResponse(
    string AlertType,
    string ItemName,
    int CurrentStock,
    int MinimumThreshold,
    string Unit);

public sealed record ExportAuditResponse(
    Guid ExportId,
    string ReportType,
    int RowCount,
    DateTime ExportedAtUtc);

public sealed record ReportingDashboardRealtimeUpdate(
    string DashboardType,
    long SequenceNumber,
    DateOnly MetricDate,
    Guid? DepartmentId,
    double ProjectionLagSeconds,
    DateTime EmittedAtUtc);

public sealed record ProjectionLagInfo(
    string ProjectionName,
    long LastProcessedPosition,
    DateTime LastProcessedTimestampUtc,
    double LagSeconds,
    bool IsHealthy,
    DateTime CheckedAtUtc);

