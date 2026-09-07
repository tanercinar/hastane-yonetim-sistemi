using HospitalManagement.BuildingBlocks.Clinical;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class PatientAllergyLookup(ClinicalRecordsDbContext dbContext) : IPatientAllergyLookup
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyList<PatientAllergySummaryDto>> GetActiveAllergiesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            return Array.Empty<PatientAllergySummaryDto>();
        }

        var allergies = await _dbContext.AllergyIntolerances
            .AsNoTracking()
            .Where(a => a.PatientId == patientId &&
                        a.ClinicalStatus == AllergyClinicalStatus.Active &&
                        a.VerificationStatus != AllergyVerificationStatus.EnteredInError &&
                        a.VerificationStatus != AllergyVerificationStatus.Refuted)
            .OrderByDescending(a => a.RecordedAtUtc)
            .Select(a => new PatientAllergySummaryDto(
                a.Id,
                a.Substance,
                a.Category.ToString(),
                a.Criticality.ToString(),
                a.Manifestation))
            .ToListAsync(cancellationToken);

        return allergies;
    }
}
