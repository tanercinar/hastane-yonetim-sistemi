using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.Modules.Pharmacy.Application;

public sealed record PrescriptionItemDto(
    Guid Id,
    Guid PrescriptionId,
    Guid MedicationCatalogItemId,
    string MedicationCode,
    string BrandName,
    string GenericName,
    MedicationForm Form,
    MedicationRoute Route,
    decimal Dose,
    string DoseUnit,
    string Frequency,
    int DurationDays,
    int Quantity,
    string QuantityUnit,
    int DispensedQuantity,
    bool IsFullyDispensed,
    string? Instructions,
    DateTime CreatedAtUtc);

public sealed record PrescriptionDetailDto(
    Guid Id,
    string PrescriptionNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PrescribingDoctorId,
    Guid DepartmentId,
    PrescriptionStatus Status,
    DateTime? ValidUntilUtc,
    DateTime? SignedAtUtc,
    Guid? SignedByDoctorId,
    DateTime? CancelledAtUtc,
    Guid? CancelledByDoctorId,
    string? CancellationReason,
    DateTime? EnteredInErrorAtUtc,
    Guid? EnteredInErrorByDoctorId,
    string? EnteredInErrorReason,
    string? DiagnosisSummary,
    string? GeneralInstructions,
    int Version,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<PrescriptionItemDto> Items);

public sealed record PrescriptionSummaryDto(
    Guid Id,
    string PrescriptionNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PrescribingDoctorId,
    Guid DepartmentId,
    PrescriptionStatus Status,
    DateTime? ValidUntilUtc,
    DateTime? SignedAtUtc,
    int ItemCount,
    DateTime CreatedAtUtc);
