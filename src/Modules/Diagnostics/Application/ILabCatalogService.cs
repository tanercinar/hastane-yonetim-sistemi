using System.Security.Claims;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface ILabCatalogService
{
    Task<IReadOnlyList<LabCatalogSummaryDto>> SearchAsync(
        string? query = null,
        string? category = null,
        bool? isActive = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<LabCatalogItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LabCatalogItemDto?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<ImportLabCatalogResultDto> ImportCatalogAsync(
        ClaimsPrincipal actor,
        ImportLabCatalogCommand command,
        CancellationToken cancellationToken = default);
}
