namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IBloodBankDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
