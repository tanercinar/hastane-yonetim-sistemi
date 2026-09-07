namespace HospitalManagement.Modules.Diagnostics.Application;

public interface ILabCatalogDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
