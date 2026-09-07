using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class PrescriptionDispenseUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void PartialDispenseTransitionsStatusToPartiallyDispensed()
    {
        var nowUtc = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
        var rx = Prescription.CreateDraft(
            Guid.NewGuid(),
            "DEMO-RX-20260829-1001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tanı",
            "Talimat",
            nowUtc);

        var itemId = Guid.NewGuid();
        rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            MedicationRoute.Oral,
            500,
            "mg",
            "2x1",
            7,
            quantity: 2,
            quantityUnit: "kutu",
            instructions: "Yemekten sonra",
            nowUtc: nowUtc);

        rx.Sign(rx.PrescribingDoctorId, nowUtc.AddDays(14), nowUtc);
        Assert.Equal(PrescriptionStatus.Signed, rx.Status);

        // Dispense 1 out of 2
        rx.RecordDispense(itemId, 1, nowUtc.AddHours(1));

        Assert.Equal(PrescriptionStatus.PartiallyDispensed, rx.Status);
        Assert.Equal(1, rx.Items.First().DispensedQuantity);
        Assert.False(rx.Items.First().IsFullyDispensed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void FullDispenseTransitionsStatusToDispensed()
    {
        var nowUtc = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
        var rx = Prescription.CreateDraft(
            Guid.NewGuid(),
            "DEMO-RX-20260829-1002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tanı",
            "Talimat",
            nowUtc);

        var itemId = Guid.NewGuid();
        rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            MedicationRoute.Oral,
            500,
            "mg",
            "2x1",
            7,
            quantity: 1,
            quantityUnit: "kutu",
            instructions: null,
            nowUtc: nowUtc);

        rx.Sign(rx.PrescribingDoctorId, nowUtc.AddDays(14), nowUtc);
        Assert.Equal(PrescriptionStatus.Signed, rx.Status);

        // Dispense full quantity
        rx.RecordDispense(itemId, 1, nowUtc.AddHours(1));

        Assert.Equal(PrescriptionStatus.Dispensed, rx.Status);
        Assert.Equal(1, rx.Items.First().DispensedQuantity);
        Assert.True(rx.Items.First().IsFullyDispensed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void OverDispenseThrowsInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
        var rx = Prescription.CreateDraft(
            Guid.NewGuid(),
            "DEMO-RX-20260829-1003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tanı",
            "Talimat",
            nowUtc);

        var itemId = Guid.NewGuid();
        rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            MedicationRoute.Oral,
            500,
            "mg",
            "2x1",
            7,
            quantity: 1,
            quantityUnit: "kutu",
            instructions: null,
            nowUtc: nowUtc);

        rx.Sign(rx.PrescribingDoctorId, nowUtc.AddDays(14), nowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() => rx.RecordDispense(itemId, 2, nowUtc));
        Assert.Contains("fazla", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void DispenseOnDraftPrescriptionThrowsInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
        var rx = Prescription.CreateDraft(
            Guid.NewGuid(),
            "DEMO-RX-20260829-1004",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tanı",
            "Talimat",
            nowUtc);

        var itemId = Guid.NewGuid();
        rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg Kapsül",
            "Amoksisilin",
            MedicationForm.Capsule,
            MedicationRoute.Oral,
            500,
            "mg",
            "2x1",
            7,
            quantity: 1,
            quantityUnit: "kutu",
            instructions: null,
            nowUtc: nowUtc);

        // Not signed
        var ex = Assert.Throws<InvalidOperationException>(() => rx.RecordDispense(itemId, 1, nowUtc));
        Assert.Contains("Yalnızca imzalı", ex.Message, StringComparison.Ordinal);
    }
}
