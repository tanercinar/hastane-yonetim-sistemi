namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public interface ISurgeryDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
