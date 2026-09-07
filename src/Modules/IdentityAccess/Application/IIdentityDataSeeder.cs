namespace HospitalManagement.Modules.IdentityAccess.Application;

public interface IIdentityDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
