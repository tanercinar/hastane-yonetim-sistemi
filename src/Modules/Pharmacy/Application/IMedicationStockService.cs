using System.Security.Claims;

namespace HospitalManagement.Modules.Pharmacy.Application;

public interface IMedicationStockService
{
    Task<PrescriptionOperationResult<IReadOnlyList<MedicationStockItemDto>>> GetStockOverviewAsync(
        ClaimsPrincipal actor,
        Guid? medicationCatalogItemId = null,
        string? location = null,
        bool? onlyLowStock = null,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<IReadOnlyList<FefoCandidateDto>>> GetFefoCandidatesAsync(
        ClaimsPrincipal actor,
        Guid medicationCatalogItemId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<MedicationStockItemDto>> AdjustStockAsync(
        ClaimsPrincipal actor,
        AdjustStockCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<IReadOnlyList<MedicationStockTransactionDto>>> GetStockTransactionsAsync(
        ClaimsPrincipal actor,
        Guid stockItemId,
        CancellationToken cancellationToken = default);
}
