using HospitalManagement.Modules.Organization.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence;

public interface IOrganizationDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class OrganizationDataSeeder(
    OrganizationDbContext dbContext,
    TimeProvider timeProvider) : IOrganizationDataSeeder
{
    private static readonly Guid CardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    private static readonly Guid LaboratoryDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000005");

    private static readonly Guid RadiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000006");

    private static readonly Guid PharmacyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000007");

    private static readonly Guid IntensiveCareDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000008");

    private static readonly Guid GeneralSurgeryDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000009");

    private static readonly Guid CardiologySpecialtyId =
        Guid.Parse("40000000-0000-0000-0000-000000000002");

    private static readonly Guid GeneralSurgerySpecialtyId =
        Guid.Parse("40000000-0000-0000-0000-000000000005");

    private static readonly DemoStaffDefinition[] DemoStaff =
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000102"), "DEMO-STAFF-DOCTOR", ClinicalProfession.Physician, CardiologySpecialtyId, CardiologyDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000103"), "DEMO-STAFF-NURSE", ClinicalProfession.Nurse, null, CardiologyDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000104"), "DEMO-STAFF-PHARMACIST", ClinicalProfession.Pharmacist, null, PharmacyDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000105"), "DEMO-STAFF-LAB", ClinicalProfession.MedicalLaboratoryProfessional, null, LaboratoryDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000106"), "DEMO-STAFF-RAD", ClinicalProfession.MedicalImagingProfessional, null, RadiologyDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000108"), "DEMO-STAFF-CHIEF", ClinicalProfession.Physician, CardiologySpecialtyId, CardiologyDepartmentId),
        new(Guid.Parse("00000000-0000-0000-0000-000000000111"), "DEMO-STAFF-ANESTH", ClinicalProfession.Physician, GeneralSurgerySpecialtyId, GeneralSurgeryDepartmentId),
    ];

    private static readonly DemoAdditionalAssignment[] DemoAdditionalAssignments =
    [
        new(
            Guid.Parse("61000000-0000-0000-0000-000000000102"),
            Guid.Parse("00000000-0000-0000-0000-000000000102"),
            IntensiveCareDepartmentId),
        new(
            Guid.Parse("61000000-0000-0000-0000-000000000103"),
            Guid.Parse("00000000-0000-0000-0000-000000000103"),
            IntensiveCareDepartmentId),
        new(
            Guid.Parse("62000000-0000-0000-0000-000000000102"),
            Guid.Parse("00000000-0000-0000-0000-000000000102"),
            GeneralSurgeryDepartmentId),
        new(
            Guid.Parse("62000000-0000-0000-0000-000000000103"),
            Guid.Parse("00000000-0000-0000-0000-000000000103"),
            GeneralSurgeryDepartmentId),
    ];

    private readonly OrganizationDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var definition in DemoStaff)
        {
            var profile = await _dbContext.StaffProfiles
                .SingleOrDefaultAsync(
                    candidate => candidate.HospitalId == OrganizationDemoData.HospitalId
                        && candidate.PersonId == definition.PersonId,
                    cancellationToken);

            if (profile is null)
            {
                profile = StaffProfile.Create(
                    definition.ProfileId,
                    OrganizationDemoData.HospitalId,
                    definition.PersonId,
                    definition.StaffNumber,
                    definition.Profession,
                    definition.PrimarySpecialtyId,
                    nowUtc);
                _dbContext.StaffProfiles.Add(profile);
            }

            var hasActiveAssignment = await _dbContext.StaffDepartmentAssignments
                .AnyAsync(
                    assignment => assignment.StaffProfileId == profile.Id
                        && assignment.DepartmentId == definition.DepartmentId
                        && assignment.EndsAtUtc == null,
                    cancellationToken);

            if (!hasActiveAssignment)
            {
                _dbContext.StaffDepartmentAssignments.Add(
                    StaffDepartmentAssignment.Create(
                        definition.AssignmentId,
                        OrganizationDemoData.HospitalId,
                        profile.Id,
                        definition.DepartmentId,
                        isPrimary: true,
                        startsAtUtc: nowUtc));
            }
        }

        foreach (var definition in DemoAdditionalAssignments)
        {
            var profile = _dbContext.StaffProfiles.Local
                .SingleOrDefault(candidate => candidate.HospitalId == OrganizationDemoData.HospitalId
                    && candidate.PersonId == definition.PersonId)
                ?? await _dbContext.StaffProfiles
                    .SingleAsync(
                        candidate => candidate.HospitalId == OrganizationDemoData.HospitalId
                            && candidate.PersonId == definition.PersonId,
                        cancellationToken);

            var hasActiveAssignment = await _dbContext.StaffDepartmentAssignments
                .AnyAsync(
                    assignment => assignment.StaffProfileId == profile.Id
                        && assignment.DepartmentId == definition.DepartmentId
                        && assignment.EndsAtUtc == null,
                    cancellationToken);

            if (!hasActiveAssignment)
            {
                _dbContext.StaffDepartmentAssignments.Add(
                    StaffDepartmentAssignment.Create(
                        definition.AssignmentId,
                        OrganizationDemoData.HospitalId,
                        profile.Id,
                        definition.DepartmentId,
                        isPrimary: false,
                        startsAtUtc: nowUtc));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoStaffDefinition(
        Guid PersonId,
        string StaffNumber,
        ClinicalProfession Profession,
        Guid? PrimarySpecialtyId,
        Guid DepartmentId)
    {
        public Guid ProfileId { get; } = CreateDeterministicId(0x50, PersonId);

        public Guid AssignmentId { get; } = CreateDeterministicId(0x60, PersonId);

        private static Guid CreateDeterministicId(byte prefix, Guid personId)
        {
            var bytes = personId.ToByteArray();
            bytes[0] = prefix;
            return new Guid(bytes);
        }
    }

    private sealed record DemoAdditionalAssignment(
        Guid AssignmentId,
        Guid PersonId,
        Guid DepartmentId);
}
