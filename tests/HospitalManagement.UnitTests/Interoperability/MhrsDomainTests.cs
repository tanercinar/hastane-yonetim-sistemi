using HospitalManagement.Modules.Interoperability.Domain.Mhrs;
using Xunit;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class MhrsDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G05")]
    public void MhrsAppointmentRecordInitializesCorrectlyAndTransitionsState()
    {
        var docId = Guid.NewGuid();
        var record = new MhrsAppointmentRecord(
            slotId: "SLOT-20260901-0930",
            patientNationalId: "12345678901",
            patientFullName: "DEMO HASTA",
            doctorId: docId,
            doctorName: "Dr. Tabip",
            clinicName: "Kardiyoloji Polikliniği",
            appointmentDateTimeUtc: new DateTime(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc),
            idempotencyKey: "IDEMP-TEST-1001");

        Assert.Equal(MhrsAppointmentStatus.Booked, record.Status);
        Assert.StartsWith("MHRS-APT-", record.MhrsAppointmentId);
        Assert.Equal("SLOT-20260901-0930", record.SlotId);
        Assert.Equal("12345678901", record.PatientNationalId);
        Assert.Equal("IDEMP-TEST-1001", record.IdempotencyKey);

        // Confirm
        record.Confirm();
        Assert.Equal(MhrsAppointmentStatus.Confirmed, record.Status);

        // Cancel
        record.Cancel("Hasta randevuya gelemeyeceğini bildirdi", isDoctor: false);
        Assert.Equal(MhrsAppointmentStatus.CancelledByPatient, record.Status);
        Assert.Equal("Hasta randevuya gelemeyeceğini bildirdi", record.CancellationReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G05")]
    public void MhrsSlotInitializesWithCorrectParameters()
    {
        var slot = new MhrsSlot(
            SlotId: "SLOT-01",
            DoctorId: Guid.NewGuid(),
            DoctorName: "Dr. Test",
            ClinicCode: "KARD-01",
            ClinicName: "Kardiyoloji",
            HospitalCode: "DEMO-HOSP-01",
            SlotDateTimeUtc: DateTime.UtcNow,
            DurationMinutes: 15,
            IsAvailable: true);

        Assert.Equal("SLOT-01", slot.SlotId);
        Assert.Equal(15, slot.DurationMinutes);
        Assert.True(slot.IsAvailable);
        Assert.Equal("DEMO-HOSP-01", slot.HospitalCode);
    }
}
