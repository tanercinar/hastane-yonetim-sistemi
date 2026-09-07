namespace HospitalManagement.Modules.Pharmacy.Application;

public sealed record CreatePrescriptionItemCommand(
    Guid MedicationCatalogItemId,
    decimal Dose,
    string DoseUnit,
    string Frequency,
    int DurationDays,
    int Quantity,
    string QuantityUnit,
    string? Instructions);

public sealed record CreatePrescriptionDraftCommand(
    Guid EncounterId,
    Guid PatientId,
    Guid PrescribingDoctorId,
    Guid DepartmentId,
    string? DiagnosisSummary,
    string? GeneralInstructions,
    IReadOnlyList<CreatePrescriptionItemCommand> Items);

public sealed record UpdatePrescriptionDraftCommand(
    Guid PrescriptionId,
    Guid PrescribingDoctorId,
    int ExpectedVersion,
    string? DiagnosisSummary,
    string? GeneralInstructions,
    IReadOnlyList<CreatePrescriptionItemCommand> Items);

public sealed record SignPrescriptionCommand(
    Guid PrescriptionId,
    Guid PrescribingDoctorId,
    int ExpectedVersion,
    int? ValidDays,
    string? OverrideReason = null,
    IReadOnlyList<string>? AcknowledgedWarningCodes = null);

public sealed record CancelPrescriptionCommand(
    Guid PrescriptionId,
    Guid PrescribingDoctorId,
    int ExpectedVersion,
    string Reason);

public sealed record MarkPrescriptionEnteredInErrorCommand(
    Guid PrescriptionId,
    Guid PrescribingDoctorId,
    int ExpectedVersion,
    string Reason);

public sealed record DispenseItemCommand(
    Guid ItemId,
    Guid StockItemId,
    int ExpectedStockVersion,
    int Quantity,
    string? Notes = null);

public sealed record DispensePrescriptionCommand(
    Guid PrescriptionId,
    Guid IdempotencyKey,
    int ExpectedVersion,
    IReadOnlyList<DispenseItemCommand> Items);
