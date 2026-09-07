using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class EncounterReferenceLookup(ClinicalRecordsDbContext dbContext) : IEncounterReferenceLookup
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public Task<EncounterReferenceDto?> FindAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Encounters
            .AsNoTracking()
            .Where(encounter => encounter.Id == encounterId)
            .Select(encounter => new EncounterReferenceDto(
                encounter.Id,
                encounter.PatientId,
                encounter.AppointmentId,
                encounter.EncounterType,
                encounter.Status))
            .FirstOrDefaultAsync(cancellationToken);
}
