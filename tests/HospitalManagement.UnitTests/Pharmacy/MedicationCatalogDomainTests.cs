using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class MedicationCatalogDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G01")]
    public void CreateMedicationCatalogItemInitializesFieldsCorrectly()
    {
        var id = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var item = MedicationCatalogItem.Create(
            id,
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            500m,
            "mg",
            MedicationRoute.Oral,
            "j01ca04",
            "Geniş spektrumlu penisilin.",
            "DEMO-MED-2026.1",
            nowUtc);

        Assert.Equal(id, item.Id);
        Assert.Equal("DEMO-MED-AMX500", item.Code);
        Assert.Equal("DEMO-Amoksilin 500mg Kapsül", item.BrandName);
        Assert.Equal("Amoksisilin", item.GenericName);
        Assert.Equal(MedicationForm.Capsule, item.Form);
        Assert.Equal(500m, item.StrengthValue);
        Assert.Equal("mg", item.StrengthUnit);
        Assert.Equal(MedicationRoute.Oral, item.Route);
        Assert.Equal("J01CA04", item.AtcCode);
        Assert.Equal("Geniş spektrumlu penisilin.", item.Description);
        Assert.Equal("DEMO-MED-2026.1", item.CatalogVersion);
        Assert.True(item.IsActive);
        Assert.Equal(nowUtc, item.CreatedAtUtc);
        Assert.Null(item.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G01")]
    public void CreateWithInvalidArgumentsThrowsArgumentException()
    {
        var nowUtc = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.Empty, "CODE", "Brand", "Generic", MedicationForm.Tablet, 100m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "", "Brand", "Generic", MedicationForm.Tablet, 100m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "", "Generic", MedicationForm.Tablet, 100m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "Brand", "", MedicationForm.Tablet, 100m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "Brand", "Generic", MedicationForm.Tablet, 0m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "Brand", "Generic", MedicationForm.Tablet, -5m, "mg", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "Brand", "Generic", MedicationForm.Tablet, 100m, "", MedicationRoute.Oral, null, null, "V1", nowUtc));

        Assert.Throws<ArgumentException>(() =>
            MedicationCatalogItem.Create(
                Guid.NewGuid(), "CODE", "Brand", "Generic", MedicationForm.Tablet, 100m, "mg", MedicationRoute.Oral, null, null, "", nowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G01")]
    public void UpdateModifiesFieldsAndSetsUpdatedAtUtc()
    {
        var item = MedicationCatalogItem.Create(
            Guid.NewGuid(),
            "DEMO-MED-PAR500",
            "DEMO-Parasetamol 500mg",
            "Parasetamol",
            MedicationForm.Tablet,
            500m,
            "mg",
            MedicationRoute.Oral,
            "N02BE01",
            "Eski açıklama",
            "DEMO-MED-2026.1",
            DateTime.UtcNow.AddDays(-10));

        var updatedUtc = DateTime.UtcNow;

        item.Update(
            "DEMO-Parasetamol 500mg Efervesan Tablet",
            "Parasetamol",
            MedicationForm.Tablet,
            500m,
            "mg",
            MedicationRoute.Oral,
            "N02BE01",
            "Yeni güncellenmiş açıklama",
            "DEMO-MED-2026.2",
            updatedUtc);

        Assert.Equal("DEMO-Parasetamol 500mg Efervesan Tablet", item.BrandName);
        Assert.Equal("DEMO-MED-2026.2", item.CatalogVersion);
        Assert.Equal("Yeni güncellenmiş açıklama", item.Description);
        Assert.Equal(updatedUtc, item.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G01")]
    public void DeactivateAndActivateTogglesIsActiveFlag()
    {
        var item = MedicationCatalogItem.Create(
            Guid.NewGuid(),
            "DEMO-MED-ASA100",
            "DEMO-Aspirin 100mg",
            "Asetilsalisilik Asit",
            MedicationForm.Tablet,
            100m,
            "mg",
            MedicationRoute.Oral,
            "B01AC06",
            null,
            "DEMO-MED-2026.1",
            DateTime.UtcNow);

        Assert.True(item.IsActive);

        var deactTime = DateTime.UtcNow;
        item.Deactivate(deactTime);
        Assert.False(item.IsActive);
        Assert.Equal(deactTime, item.UpdatedAtUtc);

        var actTime = DateTime.UtcNow.AddMinutes(5);
        item.Activate(actTime);
        Assert.True(item.IsActive);
        Assert.Equal(actTime, item.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G01")]
    public void DefaultDemoMedicationsContainDiverseClassesAndNoNullProperties()
    {
        var items = MedicationCatalogDataSeeder.GetDefaultDemoMedications(DateTime.UtcNow);

        Assert.NotEmpty(items);
        Assert.Equal(15, items.Count);

        foreach (var item in items)
        {
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.False(string.IsNullOrWhiteSpace(item.Code));
            Assert.False(string.IsNullOrWhiteSpace(item.BrandName));
            Assert.False(string.IsNullOrWhiteSpace(item.GenericName));
            Assert.True(item.StrengthValue > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.StrengthUnit));
            Assert.False(string.IsNullOrWhiteSpace(item.CatalogVersion));
            Assert.True(item.IsActive);
        }

        // Verify distinct codes
        var distinctCodes = items.Select(i => i.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(items.Count, distinctCodes);
    }
}
