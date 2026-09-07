namespace HospitalManagement.Modules.Scheduling.Application;

public interface ISchedulingDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
