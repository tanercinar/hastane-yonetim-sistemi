using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public interface IClinicalRecordsDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class ClinicalRecordsDataSeeder(
    ClinicalRecordsDbContext dbContext) : IClinicalRecordsDataSeeder
{
    private static readonly Guid LegacyDemoDepartmentId =
        Guid.Parse("00000000-0000-0000-0000-000000000301");

    private static readonly Guid DemoCardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    private readonly ClinicalRecordsDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Encounters
            .Where(encounter => encounter.DepartmentId == LegacyDemoDepartmentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    encounter => encounter.DepartmentId,
                    DemoCardiologyDepartmentId),
                cancellationToken);

        await _dbContext.ConsultationRequests
            .Where(consultation => consultation.TargetDepartmentId == LegacyDemoDepartmentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    consultation => consultation.TargetDepartmentId,
                    DemoCardiologyDepartmentId),
                cancellationToken);
    }
}
