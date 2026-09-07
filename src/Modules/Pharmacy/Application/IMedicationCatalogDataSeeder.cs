namespace HospitalManagement.Modules.Pharmacy.Application;

public interface IMedicationCatalogDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
