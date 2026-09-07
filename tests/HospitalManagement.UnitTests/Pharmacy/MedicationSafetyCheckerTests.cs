using HospitalManagement.BuildingBlocks.Clinical;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class MedicationSafetyCheckerTests
{
    private static PharmacyDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PharmacyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PharmacyDbContext(options);

        // Seed some sample medications
        var amox = MedicationCatalogItem.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000601"),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            500m,
            "mg",
            MedicationRoute.Oral,
            "J01CA04",
            "Geniş spektrumlu antibiyotik",
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        var asp = MedicationCatalogItem.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000602"),
            "DEMO-MED-ASP100",
            "DEMO-Coraspin 100mg Tablet",
            "Asetilsalisilik Asit (Aspirin)",
            MedicationForm.Tablet,
            100m,
            "mg",
            MedicationRoute.Oral,
            "B01AC06",
            "Antiagregan",
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        var ibu = MedicationCatalogItem.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000603"),
            "DEMO-MED-IBU400",
            "DEMO-İbufen 400mg Film Tablet",
            "İbuprofen",
            MedicationForm.Tablet,
            400m,
            "mg",
            MedicationRoute.Oral,
            "M01AE01",
            "NSAİİ Ağrı kesici",
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        var mtx = MedicationCatalogItem.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000605"),
            "DEMO-MED-MTX2.5",
            "DEMO-Metotreksat 2.5mg Tablet",
            "Metotreksat",
            MedicationForm.Tablet,
            2.5m,
            "mg",
            MedicationRoute.Oral,
            "L01BA01",
            "Antimetabolit",
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        var par = MedicationCatalogItem.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000604"),
            "DEMO-MED-PAR500",
            "DEMO-Parasetamol 500mg",
            "Parasetamol",
            MedicationForm.Tablet,
            500m,
            "mg",
            MedicationRoute.Oral,
            "N02BE01",
            null,
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        db.MedicationCatalogItems.AddRange(amox, asp, ibu, mtx, par);
        db.SaveChanges();

        return db;
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckWithNoItemsReturnsNoWarnings()
    {
        using var db = CreateInMemoryDbContext();
        var checker = new MedicationSafetyChecker(db);

        var result = await checker.CheckSafetyAsync(Guid.NewGuid(), []);

        Assert.False(result.HasWarnings);
        Assert.False(result.HasCriticalWarnings);
        Assert.Empty(result.Warnings);
        Assert.Contains("sentetik DEMO verilerle", result.Disclaimer, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckDetectsPatientAllergyCrossReactionAsCritical()
    {
        using var db = CreateInMemoryDbContext();
        var patientId = Guid.NewGuid();

        var fakeAllergyLookup = new FakePatientAllergyLookup(new Dictionary<Guid, List<PatientAllergySummaryDto>>
        {
            [patientId] =
            [
                new(Guid.NewGuid(), "Penisilin", "Medication", "High", "Anafilaksi"),
            ],
        });

        var checker = new MedicationSafetyChecker(db, fakeAllergyLookup);

        var items = new List<PrescriptionItemSafetyCandidate>
        {
            new(Guid.Parse("00000000-0000-0000-0000-000000000601"), 500, "mg", "2x1", 7),
        };

        var result = await checker.CheckSafetyAsync(patientId, items);

        Assert.True(result.HasWarnings);
        Assert.True(result.HasCriticalWarnings);
        var allergyWarn = Assert.Single(result.Warnings, w => w.Type == SafetyWarningType.AllergyCrossReaction);
        Assert.Equal(SafetyWarningSeverity.Critical, allergyWarn.Severity);
        Assert.Contains("Penisilin", allergyWarn.Title, StringComparison.Ordinal);
        Assert.True(allergyWarn.RequiresOverrideReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckDetectsIntraPrescriptionDuplicateTherapyAsCritical()
    {
        using var db = CreateInMemoryDbContext();
        var checker = new MedicationSafetyChecker(db);

        var items = new List<PrescriptionItemSafetyCandidate>
        {
            new(Guid.Parse("00000000-0000-0000-0000-000000000604"), 500, "mg", "3x1", 5),
            new(Guid.Parse("00000000-0000-0000-0000-000000000604"), 500, "mg", "1x1", 3),
        };

        var result = await checker.CheckSafetyAsync(Guid.NewGuid(), items);

        Assert.True(result.HasWarnings);
        Assert.True(result.HasCriticalWarnings);
        var dupWarn = Assert.Single(result.Warnings, w => w.Type == SafetyWarningType.DuplicateTherapy);
        Assert.Equal(SafetyWarningSeverity.Critical, dupWarn.Severity);
        Assert.Contains("Parasetamol", dupWarn.Title, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckDetectsDrugInteractionAspirinAndIbuprofenAsCritical()
    {
        using var db = CreateInMemoryDbContext();
        var checker = new MedicationSafetyChecker(db);

        var items = new List<PrescriptionItemSafetyCandidate>
        {
            new(Guid.Parse("00000000-0000-0000-0000-000000000602"), 100, "mg", "1x1", 30), // Aspirin
            new(Guid.Parse("00000000-0000-0000-0000-000000000603"), 400, "mg", "2x1", 7),  // İbuprofen
        };

        var result = await checker.CheckSafetyAsync(Guid.NewGuid(), items);

        Assert.True(result.HasWarnings);
        Assert.True(result.HasCriticalWarnings);
        var intWarn = Assert.Single(result.Warnings, w => w.Type == SafetyWarningType.DrugInteraction);
        Assert.Equal(SafetyWarningSeverity.Critical, intWarn.Severity);
        Assert.Contains("NSAİİ", intWarn.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G04")]
    public async Task SafetyCheckDetectsDrugInteractionAmoxicillinAndMethotrexateAsModerate()
    {
        using var db = CreateInMemoryDbContext();
        var checker = new MedicationSafetyChecker(db);

        var items = new List<PrescriptionItemSafetyCandidate>
        {
            new(Guid.Parse("00000000-0000-0000-0000-000000000601"), 500, "mg", "2x1", 7),   // Amoksisilin
            new(Guid.Parse("00000000-0000-0000-0000-000000000605"), 2.5m, "mg", "1x1", 30), // Metotreksat
        };

        var result = await checker.CheckSafetyAsync(Guid.NewGuid(), items);

        Assert.True(result.HasWarnings);
        Assert.False(result.HasCriticalWarnings);
        Assert.True(result.HasModerateWarnings);
        var intWarn = Assert.Single(result.Warnings, w => w.Type == SafetyWarningType.DrugInteraction);
        Assert.Equal(SafetyWarningSeverity.Moderate, intWarn.Severity);
        Assert.Contains("metotreksat", intWarn.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakePatientAllergyLookup : IPatientAllergyLookup
    {
        private readonly Dictionary<Guid, List<PatientAllergySummaryDto>> _allergies;

        public FakePatientAllergyLookup(Dictionary<Guid, List<PatientAllergySummaryDto>> allergies)
        {
            _allergies = allergies;
        }

        public Task<IReadOnlyList<PatientAllergySummaryDto>> GetActiveAllergiesAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            if (_allergies.TryGetValue(patientId, out var list))
            {
                return Task.FromResult<IReadOnlyList<PatientAllergySummaryDto>>(list);
            }

            return Task.FromResult<IReadOnlyList<PatientAllergySummaryDto>>([]);
        }
    }
}
