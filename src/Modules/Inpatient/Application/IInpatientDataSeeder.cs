namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
