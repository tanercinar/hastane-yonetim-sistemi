namespace HospitalManagement.Contracts.Diagnostics;

public sealed record CreateCrossmatchRequestDto
{
    public Guid DiagnosticOrderId
    {
        get; init;
    }
    public Guid DiagnosticOrderItemId
    {
        get; init;
    }
    public Guid PatientId
    {
        get; init;
    }
    public required string PatientBloodGroup
    {
        get; init;
    }
    public required string RequestedProductType
    {
        get; init;
    }
    public int UnitsRequested
    {
        get; init;
    }
    public DateTime? RequiredByUtc
    {
        get; init;
    }
}

public sealed record PerformCrossmatchRequestDto
{
    public Guid BloodUnitId
    {
        get; init;
    }
    public string? TechnicianNotes
    {
        get; init;
    }
}

public sealed record IssueBloodUnitRequestDto
{
    public string? Reason
    {
        get; init;
    }
}

public sealed record RecordTransfusionRequestDto
{
    public string? TransfusionNotes
    {
        get; init;
    }
}

public sealed record BloodUnitResponse(
    Guid Id,
    string UnitNumber,
    string ProductType,
    string BloodGroup,
    int VolumeMl,
    DateTime DonationDateUtc,
    DateTime ExpiryDateUtc,
    string StorageLocation,
    string Status,
    Guid? ReservedForPatientId,
    DateTime? ReservedUntilUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record CrossmatchDetailResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string PatientBloodGroup,
    string RequestedProductType,
    int UnitsRequested,
    DateTime? RequiredByUtc,
    string Status,
    string CompatibilityResult,
    string? TechnicianNotes,
    DateTime? TestedAtUtc,
    Guid? TestedByUserId,
    Guid? AllocatedBloodUnitId,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record CrossmatchSummaryResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string PatientBloodGroup,
    string RequestedProductType,
    int UnitsRequested,
    string Status,
    string CompatibilityResult,
    Guid? AllocatedBloodUnitId,
    DateTime CreatedAtUtc);

public sealed record BloodInventorySummaryResponse(
    int TotalUnits,
    int AvailableUnits,
    int ReservedUnits,
    int IssuedUnits,
    int TransfusedUnits,
    int DiscardedUnits,
    Dictionary<string, int> UnitsByGroup,
    Dictionary<string, int> UnitsByType);
