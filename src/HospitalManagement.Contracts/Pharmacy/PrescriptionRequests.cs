namespace HospitalManagement.Contracts.Pharmacy;

public sealed record CreatePrescriptionDraftItemRequest
{
    public Guid MedicationCatalogItemId
    {
        get; init;
    }
    public decimal Dose
    {
        get; init;
    }
    public string DoseUnit { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public int DurationDays
    {
        get; init;
    }
    public int Quantity
    {
        get; init;
    }
    public string QuantityUnit { get; init; } = string.Empty;
    public string? Instructions
    {
        get; init;
    }
}

public sealed record CreatePrescriptionDraftRequest
{
    public Guid EncounterId
    {
        get; init;
    }
    public Guid PatientId
    {
        get; init;
    }
    public Guid DepartmentId
    {
        get; init;
    }
    public string? DiagnosisSummary
    {
        get; init;
    }
    public string? GeneralInstructions
    {
        get; init;
    }
    public List<CreatePrescriptionDraftItemRequest> Items { get; init; } = [];
}

public sealed record UpdatePrescriptionDraftRequest
{
    public int ExpectedVersion
    {
        get; init;
    }
    public string? DiagnosisSummary
    {
        get; init;
    }
    public string? GeneralInstructions
    {
        get; init;
    }
    public List<CreatePrescriptionDraftItemRequest> Items { get; init; } = [];
}

public sealed record SignPrescriptionRequest
{
    public int ExpectedVersion
    {
        get; init;
    }
    public int? ValidDays
    {
        get; init;
    }
    public string? OverrideReason
    {
        get; init;
    }
    public List<string>? AcknowledgedWarningCodes
    {
        get; init;
    }
}

public sealed record CancelPrescriptionRequest
{
    public int ExpectedVersion
    {
        get; init;
    }
    public string Reason { get; init; } = string.Empty;
}

public sealed record MarkPrescriptionEnteredInErrorRequest
{
    public int ExpectedVersion
    {
        get; init;
    }
    public string Reason { get; init; } = string.Empty;
}

public sealed record DispensePrescriptionItemRequest
{
    public Guid ItemId
    {
        get; init;
    }
    public Guid StockItemId
    {
        get; init;
    }
    public int ExpectedStockVersion
    {
        get; init;
    }
    public int Quantity
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record DispensePrescriptionRequest
{
    public Guid IdempotencyKey
    {
        get; init;
    }
    public int ExpectedVersion
    {
        get; init;
    }
    public List<DispensePrescriptionItemRequest> Items { get; init; } = [];
}
