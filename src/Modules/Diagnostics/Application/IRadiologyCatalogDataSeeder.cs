namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IRadiologyCatalogDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
