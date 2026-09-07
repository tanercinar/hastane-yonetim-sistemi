using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.Organization.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.IntegrationTests;

public sealed class OrganizationPersistenceTests
{
    private static readonly DateTime DemoUtc = new(
        2026,
        8,
        27,
        9,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G01")]
    public async Task OrganizationMigrationSeedsDeterministicDemoHierarchyWithoutHrData()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        await using var context = CreateContext(database.ConnectionString);

        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
        Assert.Equal(4, pendingMigrations.Count);
        Assert.EndsWith("_InitialOrganization", pendingMigrations[0], StringComparison.Ordinal);
        Assert.EndsWith("_AddPharmacyDepartmentScope", pendingMigrations[1], StringComparison.Ordinal);
        Assert.EndsWith("_AddIntensiveCareDepartment", pendingMigrations[2], StringComparison.Ordinal);
        Assert.EndsWith("_AddGeneralSurgeryDepartment", pendingMigrations[3], StringComparison.Ordinal);

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        var hospital = await context.Hospitals.AsNoTracking().SingleAsync();
        var facility = await context.Facilities.AsNoTracking().SingleAsync();
        var departments = await context.Departments.AsNoTracking().ToListAsync();
        var specialties = await context.Specialties.AsNoTracking().ToListAsync();

        Assert.Equal("DEMO-HOSPITAL", hospital.Code);
        Assert.Equal(hospital.Id, facility.HospitalId);
        Assert.Equal("DEMO-CENTRAL", facility.Code);
        Assert.Equal(9, departments.Count);
        Assert.Equal(3, departments.Count(department => department.ParentDepartmentId is null));
        Assert.Equal(6, departments.Count(department => department.ParentDepartmentId is not null));
        var clinicalServices = departments.Single(department =>
            department.Code == "DEMO-CLINICAL-SERVICES");
        var intensiveCare = departments.Single(department =>
            department.Code == "DEMO-INTENSIVE-CARE");
        var generalSurgery = departments.Single(department =>
            department.Code == "DEMO-GENERAL-SURGERY");
        Assert.Equal(clinicalServices.Id, intensiveCare.ParentDepartmentId);
        Assert.Equal(clinicalServices.Id, generalSurgery.ParentDepartmentId);
        Assert.Equal(5, specialties.Count);
        Assert.All(
            departments.Cast<object>().Concat(specialties),
            item => Assert.StartsWith(
                "DEMO-",
                item switch
                {
                    Department department => department.Code,
                    Specialty specialty => specialty.Code,
                    _ => throw new InvalidOperationException("Unexpected seeded organization type."),
                },
                StringComparison.Ordinal));
        Assert.Empty(await context.StaffProfiles.AsNoTracking().ToListAsync());
        Assert.Empty(await context.StaffDepartmentAssignments.AsNoTracking().ToListAsync());

        var schema = await ReadOrganizationSchemaAsync(database.ConnectionString);
        Assert.Equal(
            [
                "departments",
                "facilities",
                "hospitals",
                "specialties",
                "staff_department_assignments",
                "staff_profiles",
            ],
            schema.Keys.Order(StringComparer.Ordinal));
        var forbiddenHrTerms = new[]
        {
            "salary",
            "payroll",
            "bank",
            "tax",
            "leave",
            "contract",
            "benefit",
        };
        Assert.DoesNotContain(
            schema.SelectMany(table => table.Value),
            column => forbiddenHrTerms.Any(term => column.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F02-G01")]
    public async Task AssignmentScopeUniquenessAndConcurrencyAreEnforcedByPostgreSql()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        await using (var setupContext = CreateContext(database.ConnectionString))
        {
            await setupContext.Database.MigrateAsync();

            var firstHospital = await setupContext.Hospitals.SingleAsync();
            var seededSpecialty = await setupContext.Specialties.FirstAsync();
            var secondHospital = Hospital.Create(
                Guid.NewGuid(),
                "DEMO-HOSPITAL-SECOND",
                "DEMO İkinci Eğitim Hastanesi",
                DemoUtc);
            var secondFacility = Facility.Create(
                Guid.NewGuid(),
                secondHospital.Id,
                "DEMO-SECOND-CENTRAL",
                "DEMO İkinci Merkez Şube",
                DemoUtc);
            var secondDepartment = Department.Create(
                Guid.NewGuid(),
                secondHospital.Id,
                secondFacility.Id,
                null,
                "DEMO-SECOND-DEPARTMENT",
                "DEMO İkinci Klinik Bölümü",
                DepartmentCategory.Clinical,
                DemoUtc);
            var profile = StaffProfile.Create(
                Guid.NewGuid(),
                firstHospital.Id,
                Guid.NewGuid(),
                "DEMO-STAFF-SCOPE-001",
                ClinicalProfession.Physician,
                seededSpecialty.Id,
                DemoUtc);

            setupContext.AddRange(secondHospital, secondFacility, secondDepartment, profile);
            await setupContext.SaveChangesAsync();

            var mismatchedAssignment = StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                firstHospital.Id,
                profile.Id,
                secondDepartment.Id,
                true,
                DemoUtc);
            setupContext.Add(mismatchedAssignment);
            await Assert.ThrowsAsync<DbUpdateException>(() => setupContext.SaveChangesAsync());
            setupContext.ChangeTracker.Clear();

            var firstDepartment = await setupContext.Departments
                .FirstAsync(department => department.HospitalId == firstHospital.Id);
            var validAssignment = StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                firstHospital.Id,
                profile.Id,
                firstDepartment.Id,
                true,
                DemoUtc);
            setupContext.Add(validAssignment);
            await setupContext.SaveChangesAsync();

            var duplicateActiveAssignment = StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                firstHospital.Id,
                profile.Id,
                firstDepartment.Id,
                false,
                DemoUtc.AddHours(1));
            setupContext.Add(duplicateActiveAssignment);
            await Assert.ThrowsAsync<DbUpdateException>(() => setupContext.SaveChangesAsync());
            setupContext.ChangeTracker.Clear();

            var anotherDepartment = await setupContext.Departments
                .FirstAsync(department =>
                    department.HospitalId == firstHospital.Id
                    && department.Id != firstDepartment.Id);
            var duplicatePrimaryAssignment = StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                firstHospital.Id,
                profile.Id,
                anotherDepartment.Id,
                true,
                DemoUtc.AddHours(1));
            setupContext.Add(duplicatePrimaryAssignment);
            await Assert.ThrowsAsync<DbUpdateException>(() => setupContext.SaveChangesAsync());
        }

        Guid assignmentId;
        await using (var lookupContext = CreateContext(database.ConnectionString))
        {
            assignmentId = await lookupContext.StaffDepartmentAssignments
                .Select(assignment => assignment.Id)
                .SingleAsync();
        }

        await using var firstWriter = CreateContext(database.ConnectionString);
        await using var staleWriter = CreateContext(database.ConnectionString);
        var firstCopy = await firstWriter.StaffDepartmentAssignments
            .SingleAsync(assignment => assignment.Id == assignmentId);
        var staleCopy = await staleWriter.StaffDepartmentAssignments
            .SingleAsync(assignment => assignment.Id == assignmentId);

        firstCopy.End(DemoUtc.AddDays(1));
        await firstWriter.SaveChangesAsync();
        Assert.Equal(2, firstCopy.Version);

        staleCopy.End(DemoUtc.AddDays(2));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleWriter.SaveChangesAsync());
    }

    private static OrganizationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable(
                OrganizationDbContext.MigrationHistoryTable,
                OrganizationDbContext.Schema))
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false)
            .Options;
        return new OrganizationDbContext(options);
    }

    private static async Task<Dictionary<string, string[]>> ReadOrganizationSchemaAsync(
        string connectionString)
    {
        const string Sql = """
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'organization'
              AND table_name <> '__ef_migrations_history'
            ORDER BY table_name, ordinal_position;
            """;
        var columns = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(Sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString(0);
            if (!columns.TryGetValue(tableName, out var tableColumns))
            {
                tableColumns = [];
                columns.Add(tableName, tableColumns);
            }

            tableColumns.Add(reader.GetString(1));
        }

        return columns.ToDictionary(
            item => item.Key,
            item => item.Value.ToArray(),
            StringComparer.Ordinal);
    }
}
