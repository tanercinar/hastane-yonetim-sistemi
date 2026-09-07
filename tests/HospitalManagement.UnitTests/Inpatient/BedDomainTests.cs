using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class BedDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateBedShouldInitializeCorrectly()
    {
        var wardId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var bedId = Guid.NewGuid();

        var bed = Bed.Create(
            bedId,
            wardId,
            roomId,
            "301-A",
            BedPlacementGender.MaleOnly,
            IsolationType.None,
            hasTelemetry: true,
            hasOxygen: true,
            hasVentilator: false,
            FixedNow);

        Assert.Equal(bedId, bed.Id);
        Assert.Equal(wardId, bed.WardId);
        Assert.Equal(roomId, bed.RoomId);
        Assert.Equal("301-A", bed.BedNumber);
        Assert.Equal(BedStatus.Available, bed.Status);
        Assert.Null(bed.CurrentAdmissionId);
        Assert.Null(bed.CurrentPatientId);
        Assert.Equal(1, bed.Version);
        Assert.True(bed.HasTelemetry);
        Assert.True(bed.HasOxygen);
        Assert.False(bed.HasVentilator);
    }

    [Fact]
    public void AssignAdmissionWhenAvailableShouldSetOccupiedAndIncrementVersion()
    {
        var bed = CreateSampleBed();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        bed.AssignAdmission(admissionId, patientId, FixedNow);

        Assert.Equal(BedStatus.Occupied, bed.Status);
        Assert.Equal(admissionId, bed.CurrentAdmissionId);
        Assert.Equal(patientId, bed.CurrentPatientId);
        Assert.Equal(2, bed.Version);
    }

    [Fact]
    public void AssignAdmissionWhenAlreadyOccupiedShouldThrowInvalidOperationException()
    {
        var bed = CreateSampleBed();
        var admission1 = Guid.NewGuid();
        var patient1 = Guid.NewGuid();
        bed.AssignAdmission(admission1, patient1, FixedNow);

        var admission2 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            bed.AssignAdmission(admission2, patient2, FixedNow.AddHours(1)));
        Assert.Contains("uygun değil", ex.Message);
    }

    [Fact]
    public void ReleaseBedWhenOccupiedShouldSetCleaningAndClearPatient()
    {
        var bed = CreateSampleBed();
        bed.AssignAdmission(Guid.NewGuid(), Guid.NewGuid(), FixedNow);

        bed.ReleaseBed(FixedNow.AddDays(1), requireCleaning: true);

        Assert.Equal(BedStatus.Cleaning, bed.Status);
        Assert.Null(bed.CurrentAdmissionId);
        Assert.Null(bed.CurrentPatientId);
        Assert.Equal(3, bed.Version);
    }

    [Fact]
    public void CompleteCleaningWhenCleaningShouldSetAvailable()
    {
        var bed = CreateSampleBed();
        bed.AssignAdmission(Guid.NewGuid(), Guid.NewGuid(), FixedNow);
        bed.ReleaseBed(FixedNow.AddDays(1), requireCleaning: true);

        bed.CompleteCleaning(FixedNow.AddDays(1).AddMinutes(30));

        Assert.Equal(BedStatus.Available, bed.Status);
        Assert.Equal(4, bed.Version);
    }

    [Fact]
    public void MarkUnderMaintenanceWhenOccupiedShouldThrow()
    {
        var bed = CreateSampleBed();
        bed.AssignAdmission(Guid.NewGuid(), Guid.NewGuid(), FixedNow);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            bed.MarkUnderMaintenance("Motor arızası", FixedNow));
        Assert.Contains("Dolu yatak", ex.Message);
    }

    [Fact]
    public void MarkUnderMaintenanceWhenAvailableShouldSetMaintenanceAndReason()
    {
        var bed = CreateSampleBed();

        bed.MarkUnderMaintenance("Mekanik yükseklik ayar motoru arızalı", FixedNow);

        Assert.Equal(BedStatus.Maintenance, bed.Status);
        Assert.Equal("Mekanik yükseklik ayar motoru arızalı", bed.MaintenanceReason);

        bed.RestoreFromMaintenance(FixedNow.AddDays(1));
        Assert.Equal(BedStatus.Available, bed.Status);
        Assert.Null(bed.MaintenanceReason);
    }

    [Fact]
    public void ReserveAndCancelReservationShouldWorkCorrectly()
    {
        var bed = CreateSampleBed();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        bed.Reserve(admissionId, patientId, FixedNow);
        Assert.Equal(BedStatus.Reserved, bed.Status);
        Assert.Equal(admissionId, bed.CurrentAdmissionId);

        bed.CancelReservation(FixedNow.AddHours(1));
        Assert.Equal(BedStatus.Available, bed.Status);
        Assert.Null(bed.CurrentAdmissionId);
    }

    private static Bed CreateSampleBed() =>
        Bed.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "301-A",
            BedPlacementGender.MaleOnly,
            IsolationType.None,
            hasTelemetry: true,
            hasOxygen: true,
            hasVentilator: false,
            FixedNow);
}
