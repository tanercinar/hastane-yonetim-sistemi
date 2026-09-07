namespace HospitalManagement.Modules.Patients.Application;

public interface IPatientDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
