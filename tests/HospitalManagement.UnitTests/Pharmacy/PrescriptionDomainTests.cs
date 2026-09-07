using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class PrescriptionDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void CreateDraftInitializesStateCorrectly()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var rx = Prescription.CreateDraft(
            id,
            "DEMO-RX-20260829-0001",
            patientId,
            encounterId,
            doctorId,
            deptId,
            "Akut Faranjit",
            "Bol sıvı tüketiniz.",
            nowUtc);

        Assert.Equal(id, rx.Id);
        Assert.Equal("DEMO-RX-20260829-0001", rx.PrescriptionNumber);
        Assert.Equal(patientId, rx.PatientId);
        Assert.Equal(encounterId, rx.EncounterId);
        Assert.Equal(doctorId, rx.PrescribingDoctorId);
        Assert.Equal(deptId, rx.DepartmentId);
        Assert.Equal(PrescriptionStatus.Draft, rx.Status);
        Assert.Equal("Akut Faranjit", rx.DiagnosisSummary);
        Assert.Equal("Bol sıvı tüketiniz.", rx.GeneralInstructions);
        Assert.Equal(1, rx.Version);
        Assert.Equal(nowUtc, rx.CreatedAtUtc);
        Assert.Empty(rx.Items);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void AddItemAndRemoveItemInDraftWorksProperly()
    {
        var rx = CreateTestDraft();
        var itemId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var item = rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "DEMO-MED-AMX500",
            "DEMO-Amoksilin 500mg",
            "Amoksisilin",
            MedicationForm.Capsule,
            MedicationRoute.Oral,
            500m,
            "mg",
            "2x1",
            7,
            1,
            "kutu",
            "Yemekten sonra",
            nowUtc);

        Assert.Single(rx.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal("DEMO-MED-AMX500", item.MedicationCode);
        Assert.Equal(500m, item.Dose);
        Assert.Equal("2x1", item.Frequency);
        Assert.Equal(7, item.DurationDays);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(0, item.DispensedQuantity);
        Assert.False(item.IsFullyDispensed);

        rx.RemoveItem(itemId, nowUtc.AddMinutes(1));
        Assert.Empty(rx.Items);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void SignPrescriptionTransitionsToSignedAndIncrementsVersion()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;

        rx.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-MED-PAR500",
            "DEMO-Parasetamol 500mg",
            "Parasetamol",
            MedicationForm.Tablet,
            MedicationRoute.Oral,
            500m,
            "mg",
            "3x1",
            5,
            1,
            "kutu",
            null,
            nowUtc);

        var doctorId = Guid.NewGuid();
        var validUntil = nowUtc.AddDays(14);

        rx.Sign(doctorId, validUntil, nowUtc);

        Assert.Equal(PrescriptionStatus.Signed, rx.Status);
        Assert.Equal(doctorId, rx.SignedByDoctorId);
        Assert.Equal(nowUtc, rx.SignedAtUtc);
        Assert.Equal(validUntil, rx.ValidUntilUtc);
        Assert.Equal(2, rx.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void SignWithoutItemsThrowsInvalidOperationException()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            rx.Sign(Guid.NewGuid(), nowUtc.AddDays(7), nowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void ModifyingSignedPrescriptionThrowsInvalidOperationException()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;

        rx.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-MED-PAR500",
            "DEMO-Parasetamol 500mg",
            "Parasetamol",
            MedicationForm.Tablet,
            MedicationRoute.Oral,
            500m,
            "mg",
            "3x1",
            5,
            1,
            "kutu",
            null,
            nowUtc);

        rx.Sign(Guid.NewGuid(), nowUtc.AddDays(7), nowUtc);

        Assert.Throws<InvalidOperationException>(() =>
            rx.AddItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEMO-MED-AMX500",
                "DEMO-Amoksilin",
                "Amoksisilin",
                MedicationForm.Capsule,
                MedicationRoute.Oral,
                500m,
                "mg",
                "2x1",
                7,
                1,
                "kutu",
                null,
                nowUtc));

        Assert.Throws<InvalidOperationException>(() =>
            rx.UpdateDetails("Yeni Tanı", null, nowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void RecordDispenseLifecyclePartiallyAndFullyDispensed()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;
        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();

        rx.AddItem(itemId1, Guid.NewGuid(), "MED-1", "İlaç 1", "Etken 1", MedicationForm.Tablet, MedicationRoute.Oral, 100m, "mg", "1x1", 30, 2, "kutu", null, nowUtc);
        rx.AddItem(itemId2, Guid.NewGuid(), "MED-2", "İlaç 2", "Etken 2", MedicationForm.Tablet, MedicationRoute.Oral, 50m, "mg", "1x1", 30, 1, "kutu", null, nowUtc);

        rx.Sign(Guid.NewGuid(), nowUtc.AddDays(14), nowUtc);

        // 1. Partial dispense item 1 (1 of 2 kutu)
        rx.RecordDispense(itemId1, 1, nowUtc.AddHours(1));
        Assert.Equal(PrescriptionStatus.PartiallyDispensed, rx.Status);
        Assert.Equal(1, rx.Items.First(i => i.Id == itemId1).DispensedQuantity);
        Assert.False(rx.Items.First(i => i.Id == itemId1).IsFullyDispensed);

        // 2. Dispense remaining item 1 (1 kutu)
        rx.RecordDispense(itemId1, 1, nowUtc.AddHours(2));
        Assert.Equal(PrescriptionStatus.PartiallyDispensed, rx.Status); // itemId2 is not dispensed yet
        Assert.Equal(2, rx.Items.First(i => i.Id == itemId1).DispensedQuantity);
        Assert.True(rx.Items.First(i => i.Id == itemId1).IsFullyDispensed);

        // 3. Dispense item 2 (1 of 1 kutu) -> Now all items fully dispensed
        rx.RecordDispense(itemId2, 1, nowUtc.AddHours(3));
        Assert.Equal(PrescriptionStatus.Dispensed, rx.Status);
        Assert.True(rx.Items.All(i => i.IsFullyDispensed));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void DispenseOverQuantityThrowsInvalidOperationException()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;
        var itemId = Guid.NewGuid();

        rx.AddItem(itemId, Guid.NewGuid(), "MED-1", "İlaç 1", "Etken 1", MedicationForm.Tablet, MedicationRoute.Oral, 100m, "mg", "1x1", 30, 1, "kutu", null, nowUtc);
        rx.Sign(Guid.NewGuid(), nowUtc.AddDays(14), nowUtc);

        Assert.Throws<InvalidOperationException>(() =>
            rx.RecordDispense(itemId, 2, nowUtc.AddHours(1)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void DispenseAfterExpirationTransitionsToExpiredAndThrows()
    {
        var rx = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;
        var itemId = Guid.NewGuid();

        rx.AddItem(itemId, Guid.NewGuid(), "MED-1", "İlaç 1", "Etken 1", MedicationForm.Tablet, MedicationRoute.Oral, 100m, "mg", "1x1", 30, 1, "kutu", null, nowUtc);
        rx.Sign(Guid.NewGuid(), nowUtc.AddDays(7), nowUtc);

        Assert.Throws<InvalidOperationException>(() =>
            rx.RecordDispense(itemId, 1, nowUtc.AddDays(8)));

        Assert.Equal(PrescriptionStatus.Expired, rx.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void CancelAndMarkEnteredInErrorTransitionsCorrectly()
    {
        var rx1 = CreateTestDraft();
        var nowUtc = DateTime.UtcNow;
        var doctorId = Guid.NewGuid();

        rx1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "MED-1", "İlaç 1", "Etken 1", MedicationForm.Tablet, MedicationRoute.Oral, 100m, "mg", "1x1", 30, 1, "kutu", null, nowUtc);
        rx1.Sign(doctorId, nowUtc.AddDays(14), nowUtc);

        rx1.Cancel(doctorId, "Hasta ilacı tolere edemedi", nowUtc.AddHours(2));
        Assert.Equal(PrescriptionStatus.Cancelled, rx1.Status);
        Assert.Equal("Hasta ilacı tolere edemedi", rx1.CancellationReason);
        Assert.Equal(doctorId, rx1.CancelledByDoctorId);

        var rx2 = CreateTestDraft();
        rx2.AddItem(Guid.NewGuid(), Guid.NewGuid(), "MED-1", "İlaç 1", "Etken 1", MedicationForm.Tablet, MedicationRoute.Oral, 100m, "mg", "1x1", 30, 1, "kutu", null, nowUtc);
        rx2.Sign(doctorId, nowUtc.AddDays(14), nowUtc);

        rx2.MarkEnteredInError(doctorId, "Yanlış hasta seçimi", nowUtc.AddHours(1));
        Assert.Equal(PrescriptionStatus.EnteredInError, rx2.Status);
        Assert.Equal("Yanlış hasta seçimi", rx2.EnteredInErrorReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G02")]
    public void DispensedPrescriptionCannotBeCancelledOrMarkedEnteredInError()
    {
        var nowUtc = DateTime.UtcNow;
        var doctorId = Guid.NewGuid();
        var rx = CreateTestDraft();
        var itemId = Guid.NewGuid();

        rx.AddItem(
            itemId,
            Guid.NewGuid(),
            "MED-1",
            "İlaç 1",
            "Etken 1",
            MedicationForm.Tablet,
            MedicationRoute.Oral,
            100m,
            "mg",
            "1x1",
            1,
            1,
            "kutu",
            null,
            nowUtc);
        rx.Sign(doctorId, nowUtc.AddDays(7), nowUtc);
        rx.RecordDispense(itemId, 1, nowUtc.AddHours(1));

        Assert.Equal(PrescriptionStatus.Dispensed, rx.Status);
        Assert.Throws<InvalidOperationException>(() =>
            rx.Cancel(doctorId, "Terminal durum iptali", nowUtc.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() =>
            rx.MarkEnteredInError(doctorId, "Terminal durum düzeltmesi", nowUtc.AddHours(2)));
        Assert.Equal(PrescriptionStatus.Dispensed, rx.Status);
    }

    private static Prescription CreateTestDraft() =>
        Prescription.CreateDraft(
            Guid.NewGuid(),
            $"DEMO-RX-TEST-{Guid.NewGuid():N}"[..20],
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tanı Özeti",
            "Genel talimat",
            DateTime.UtcNow);
}
