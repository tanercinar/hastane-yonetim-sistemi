using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record BloodUnitDto(
    Guid Id,
    string UnitNumber,
    BloodProductType ProductType,
    BloodGroup BloodGroup,
    int VolumeMl,
    DateTime DonationDateUtc,
    DateTime ExpiryDateUtc,
    string StorageLocation,
    BloodUnitStatus Status,
    Guid? ReservedForPatientId,
    DateTime? ReservedUntilUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record CrossmatchDetailDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    BloodGroup PatientBloodGroup,
    BloodProductType RequestedProductType,
    int UnitsRequested,
    DateTime? RequiredByUtc,
    CrossmatchStatus Status,
    BloodCompatibilityStatus CompatibilityResult,
    string? TechnicianNotes,
    DateTime? TestedAtUtc,
    Guid? TestedByUserId,
    Guid? AllocatedBloodUnitId,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record CrossmatchSummaryDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    BloodGroup PatientBloodGroup,
    BloodProductType RequestedProductType,
    int UnitsRequested,
    CrossmatchStatus Status,
    BloodCompatibilityStatus CompatibilityResult,
    Guid? AllocatedBloodUnitId,
    DateTime CreatedAtUtc);

public sealed record BloodInventorySummaryDto(
    int TotalUnits,
    int AvailableUnits,
    int ReservedUnits,
    int IssuedUnits,
    int TransfusedUnits,
    int DiscardedUnits,
    Dictionary<string, int> UnitsByGroup,
    Dictionary<string, int> UnitsByType);
