namespace HospitalManagement.Modules.Emergency.Application;

public interface IEmergencyDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
