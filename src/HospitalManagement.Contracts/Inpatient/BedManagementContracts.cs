namespace HospitalManagement.Contracts.Inpatient;

public sealed record WardResponse(
    Guid Id,
    string Code,
    string Name,
    Guid DepartmentId,
    string Building,
    string Floor,
    string WardType,
    bool IsActive,
    int RoomCount,
    int TotalBeds,
    int AvailableBeds);

public sealed record RoomResponse(
    Guid Id,
    Guid WardId,
    string RoomNumber,
    string GenderConstraint,
    string IsolationType,
    bool IsNegativePressure,
    bool IsActive,
    List<BedResponse> Beds);

public sealed record BedResponse(
    Guid Id,
    Guid WardId,
    Guid RoomId,
    string BedNumber,
    string Status,
    Guid? CurrentAdmissionId,
    Guid? CurrentPatientId,
    string GenderConstraint,
    string IsolationType,
    bool HasTelemetry,
    bool HasOxygen,
    bool HasVentilator,
    string? MaintenanceReason,
    bool IsActive,
    int Version);

public sealed record UpdateBedStatusRequest
{
    public required string NewStatus
    {
        get; init;
    }
    public string? Reason
    {
        get; init;
    }
}

public sealed record BedOccupancySummaryResponse(
    int TotalBeds,
    int AvailableBeds,
    int OccupiedBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    int ReservedBeds,
    double OccupancyRatePercent);
