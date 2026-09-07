using System.Security.Claims;

using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.Modules.Pharmacy.Application;

public interface IMedicationCatalogService
{
    Task<IReadOnlyList<MedicationCatalogItemDto>> SearchMedicationsAsync(
        string? query,
        MedicationRoute? route = null,
        MedicationForm? form = null,
        bool? isActive = true,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<MedicationCatalogItemDto?> GetMedicationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MedicationCatalogItemDto?> GetMedicationByCodeAsync(
        string code,
        string? catalogVersion = null,
        CancellationToken cancellationToken = default);

    Task<MedicationCatalogImportResultDto> ImportCatalogAsync(
        ClaimsPrincipal actor,
        IEnumerable<MedicationCatalogItemImportDto> items,
        string catalogVersion,
        CancellationToken cancellationToken = default);
}
