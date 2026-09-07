using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record WardDto(
    Guid Id,
    string Code,
    string Name,
    Guid DepartmentId,
    string Building,
    string Floor,
    WardType WardType,
    bool IsActive,
    int RoomCount,
    int TotalBeds,
    int AvailableBeds);

public sealed record RoomDto(
    Guid Id,
    Guid WardId,
    string RoomNumber,
    BedPlacementGender GenderConstraint,
    IsolationType IsolationType,
    bool IsNegativePressure,
    bool IsActive,
    List<BedDto> Beds);

public sealed record BedDto(
    Guid Id,
    Guid WardId,
    Guid RoomId,
    string BedNumber,
    BedStatus Status,
    Guid? CurrentAdmissionId,
    Guid? CurrentPatientId,
    BedPlacementGender GenderConstraint,
    IsolationType IsolationType,
    bool HasTelemetry,
    bool HasOxygen,
    bool HasVentilator,
    string? MaintenanceReason,
    bool IsActive,
    int Version);

public sealed record BedOccupancySummaryDto(
    int TotalBeds,
    int AvailableBeds,
    int OccupiedBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    int ReservedBeds,
    double OccupancyRatePercent);
