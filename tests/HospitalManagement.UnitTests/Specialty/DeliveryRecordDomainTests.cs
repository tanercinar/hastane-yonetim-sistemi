using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Xunit;

namespace HospitalManagement.UnitTests.SpecialtyCare;

public sealed class DeliveryRecordDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G02")]
    public void CreateDeliveryRecordGeneratesValidProtocolAndProperties()
    {
        var id = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var midwifeId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var delivery = DeliveryRecord.Create(
            id,
            null,
            motherId,
            null,
            DeliveryMode.SpontaneousVaginal,
            now,
            39,
            4,
            PerinealTearDegree.FirstDegree,
            250,
            doctorId,
            midwifeId,
            null,
            null,
            "Normal spontan doğum gerçekleşti",
            now);

        Assert.Equal(id, delivery.Id);
        Assert.Equal(motherId, delivery.MotherPatientId);
        Assert.StartsWith("DEMO-DEL-", delivery.DeliveryProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(DeliveryMode.SpontaneousVaginal, delivery.DeliveryMode);
        Assert.Equal(39, delivery.GestationalAgeWeeks);
        Assert.Equal(4, delivery.GestationalAgeDays);
        Assert.Equal(PerinealTearDegree.FirstDegree, delivery.PerinealTear);
        Assert.Equal(250, delivery.EstimatedBloodLossMl);
        Assert.Equal(doctorId, delivery.AttendingDoctorId);
        Assert.Empty(delivery.Newborns);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G02")]
    public void CreateDeliveryRecordRejectsInvalidInputs()
    {
        var now = DateTime.UtcNow;
        var motherId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // Empty MotherId
        Assert.Throws<ArgumentException>(() =>
            DeliveryRecord.Create(Guid.NewGuid(), null, Guid.Empty, null, DeliveryMode.SpontaneousVaginal, now, 38, 0, PerinealTearDegree.None, 200, doctorId, null, null, null, null, now));

        // Empty DoctorId
        Assert.Throws<ArgumentException>(() =>
            DeliveryRecord.Create(Guid.NewGuid(), null, motherId, null, DeliveryMode.SpontaneousVaginal, now, 38, 0, PerinealTearDegree.None, 200, Guid.Empty, null, null, null, null, now));

        // Gestational age too low (< 20)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeliveryRecord.Create(Guid.NewGuid(), null, motherId, null, DeliveryMode.SpontaneousVaginal, now, 18, 0, PerinealTearDegree.None, 200, doctorId, null, null, null, null, now));

        // Negative blood loss
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DeliveryRecord.Create(Guid.NewGuid(), null, motherId, null, DeliveryMode.SpontaneousVaginal, now, 38, 0, PerinealTearDegree.None, -50, doctorId, null, null, null, null, now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G02")]
    public void AddNewbornValidatesPhysiologicalRangesAndStoresNewborn()
    {
        var delivery = DeliveryRecord.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            null,
            DeliveryMode.CesareanElective,
            DateTime.UtcNow,
            38,
            0,
            PerinealTearDegree.None,
            400,
            Guid.NewGuid(),
            null,
            null,
            null,
            "Elektif C/S",
            DateTime.UtcNow);

        var babyId = Guid.NewGuid();
        var babyPatientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var baby = delivery.AddNewborn(
            babyId,
            babyPatientId,
            1,
            now,
            NewbornGender.Female,
            3450,
            51.0m,
            35.5m,
            8,
            9,
            10,
            ResuscitationIntervention.None,
            "7.36",
            "Sağlıklı kız bebek",
            now);

        Assert.Single(delivery.Newborns);
        Assert.Equal(babyId, baby.Id);
        Assert.Equal(babyPatientId, baby.NewbornPatientId);
        Assert.Equal(NewbornGender.Female, baby.Gender);
        Assert.Equal(3450, baby.BirthWeightGrams);
        Assert.Equal(51.0m, baby.BirthLengthCm);
        Assert.Equal(35.5m, baby.HeadCircumferenceCm);
        Assert.Equal(8, baby.ApgarScore1Min);
        Assert.Equal(9, baby.ApgarScore5Min);
        Assert.Equal(10, baby.ApgarScore10Min);

        // Invalid weight (< 300 or > 7000)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            delivery.AddNewborn(Guid.NewGuid(), Guid.NewGuid(), 2, now, NewbornGender.Male, 200, 50, 35, 8, 9, null, ResuscitationIntervention.None, null, null, now));

        // Invalid Apgar (> 10)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            delivery.AddNewborn(Guid.NewGuid(), Guid.NewGuid(), 2, now, NewbornGender.Male, 3000, 50, 35, 12, 9, null, ResuscitationIntervention.None, null, null, now));

        Assert.Throws<InvalidOperationException>(() =>
            delivery.AddNewborn(Guid.NewGuid(), babyPatientId, 2, now, NewbornGender.Male, 3000, 50, 35, 8, 9, null, ResuscitationIntervention.None, null, null, now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G02")]
    public void NewbornPatientIdentityIsRequiredAndImmutable()
    {
        Assert.Throws<ArgumentException>(() => NewbornRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            1,
            DateTime.UtcNow,
            NewbornGender.Male,
            3100,
            49.5m,
            34.0m,
            9,
            10,
            null,
            ResuscitationIntervention.None,
            "7.38",
            null,
            DateTime.UtcNow));

        var patientId = Guid.NewGuid();
        var baby = NewbornRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            patientId,
            1,
            DateTime.UtcNow,
            NewbornGender.Male,
            3100,
            49.5m,
            34.0m,
            9,
            10,
            null,
            ResuscitationIntervention.None,
            "7.38",
            null,
            DateTime.UtcNow);

        Assert.Equal(patientId, baby.NewbornPatientId);
    }
}
