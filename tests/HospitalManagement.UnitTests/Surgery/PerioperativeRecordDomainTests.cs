using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Surgery;

public sealed class PerioperativeRecordDomainTests
{
    private readonly DbContextOptions<SurgeryDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public PerioperativeRecordDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<SurgeryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    [Trait("Roadmap", "F08-G05")]
    public void PerioperativeRecordCreationAndDraftUpdateWithValidTimeSequenceSucceeds()
    {
        var recordId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var record = PerioperativeRecord.Create(
            recordId,
            bookingId,
            patientId,
            roomId,
            AnesthesiaType.General,
            PostOpDisposition.PACU,
            now);

        Assert.Equal(recordId, record.Id);
        Assert.False(record.IsSigned);
        Assert.Empty(record.Corrections);

        var t1 = now.AddHours(1);
        var t2 = now.AddHours(1).AddMinutes(10);
        var t3 = now.AddHours(1).AddMinutes(25);
        var t4 = now.AddHours(2).AddMinutes(30);
        var t5 = now.AddHours(2).AddMinutes(45);
        var t6 = now.AddHours(3);

        record.UpdateDraft(
            t1,
            t2,
            t3,
            t4,
            t5,
            t6,
            AnesthesiaType.General,
            "Propofol ve sevofluran",
            "Safra kesesi hidropik, taşlar mevcuttu",
            null,
            50,
            "Safra Kesesi",
            true,
            PostOpDisposition.PACU,
            "15 dakikada bir vital izlem",
            now);

        Assert.Equal(t3, record.IncisionTimeUtc);
        Assert.Equal(t4, record.ClosureTimeUtc);
        Assert.True(record.CountsConfirmed);
        Assert.Equal(50, record.EstimatedBloodLossMl);
    }

    [Fact]
    [Trait("Roadmap", "F08-G05")]
    public void PerioperativeRecordInvalidTimeSequenceThrowsArgumentException()
    {
        var record = PerioperativeRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnesthesiaType.General,
            PostOpDisposition.PACU,
            DateTime.UtcNow);

        var now = DateTime.UtcNow;

        // Incision BEFORE Anesthesia Start -> Invalid!
        Assert.Throws<ArgumentException>(() => record.UpdateDraft(
            now,
            now.AddMinutes(20), // Anesthesia Start
            now.AddMinutes(10), // Incision BEFORE Anesthesia Start
            now.AddMinutes(40),
            now.AddMinutes(50),
            now.AddMinutes(60),
            AnesthesiaType.General,
            null,
            null,
            null,
            null,
            null,
            true,
            PostOpDisposition.PACU,
            null,
            now));
    }

    [Fact]
    [Trait("Roadmap", "F08-G05")]
    public void PerioperativeRecordSigningLocksRecordAndEnforcesImmutability()
    {
        var record = PerioperativeRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnesthesiaType.General,
            PostOpDisposition.PACU,
            DateTime.UtcNow);

        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        record.Sign(doctorId, now);
        Assert.True(record.IsSigned);
        Assert.Equal(doctorId, record.SignedByDoctorId);

        // Attempting direct edit after signing throws InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => record.UpdateDraft(
            now,
            null,
            null,
            null,
            null,
            null,
            AnesthesiaType.General,
            "Yeni not",
            null,
            null,
            null,
            null,
            true,
            PostOpDisposition.PACU,
            null,
            now));

        // Adding correction to signed record succeeds
        var correction = record.AddCorrection(
            Guid.NewGuid(),
            doctorId,
            "Kan kaybı revizyonu",
            "Gerçek kan kaybı 100 mL olarak güncellendi.",
            now);

        Assert.NotNull(correction);
        Assert.Single(record.Corrections);
        Assert.Equal("Kan kaybı revizyonu", record.Corrections.First().ReasonForCorrection);
    }

    [Fact]
    [Trait("Roadmap", "F08-G05")]
    public async Task PerioperativeRecordServiceFullFlowDraftSaveSignAndCorrectionSucceeds()
    {
        var recordId = Guid.Empty;
        var bookingId = Guid.Empty;
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var booking = SurgeryBooking.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                "Genel Cerrahi",
                "Kolesistektomi",
                "DEMO-PRC-CHOLE",
                SurgeryUrgency.Elective,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(3),
                null,
                DateTime.UtcNow);

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            var service = new PerioperativeRecordService(db, _auditPublisher, _timeProvider);

            // 1. Save Draft
            var saveDto = new SavePerioperativeRecordDto(
                SurgeryBookingId: booking.Id,
                RoomEntryTimeUtc: now,
                AnesthesiaStartTimeUtc: now.AddMinutes(10),
                IncisionTimeUtc: now.AddMinutes(20),
                ClosureTimeUtc: now.AddMinutes(60),
                AnesthesiaEndTimeUtc: now.AddMinutes(70),
                RoomExitTimeUtc: now.AddMinutes(80),
                AnesthesiaType: AnesthesiaType.General,
                AnesthesiaNotes: "Sorunsuz indüksiyon",
                IntraoperativeFindings: "Kolesistit bulguları",
                IntraoperativeComplications: null,
                EstimatedBloodLossMl: 30,
                SpecimensCollected: "Safra Kesesi Patoloji",
                CountsConfirmed: true,
                PostOpDisposition: PostOpDisposition.PACU,
                PostOpInstructions: "Oral stop, vital takip");

            var saveResult = await service.SaveRecordAsync(saveDto, staffId);
            Assert.True(saveResult.IsSuccess);
            Assert.NotNull(saveResult.Value);
            Assert.False(saveResult.Value.IsSigned);

            // Booking status automatically changed to InProgress
            var updatedBooking = await db.Bookings.FirstAsync(b => b.Id == booking.Id);
            Assert.Equal(SurgeryBookingStatus.InProgress, updatedBooking.Status);

            recordId = saveResult.Value.Id;
            bookingId = booking.Id;
        }

        // 2. Sign Record
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new PerioperativeRecordService(db, _auditPublisher, _timeProvider);
            var signResult = await service.SignRecordAsync(recordId, staffId);
            Assert.True(signResult.IsSuccess);
            Assert.True(signResult.Value!.IsSigned);

            // 3. Attempt direct update -> Fails with Conflict
            var saveDto = new SavePerioperativeRecordDto(
                SurgeryBookingId: bookingId,
                RoomEntryTimeUtc: now,
                AnesthesiaStartTimeUtc: now.AddMinutes(10),
                IncisionTimeUtc: now.AddMinutes(20),
                ClosureTimeUtc: now.AddMinutes(60),
                AnesthesiaEndTimeUtc: now.AddMinutes(70),
                RoomExitTimeUtc: now.AddMinutes(80),
                AnesthesiaType: AnesthesiaType.General,
                AnesthesiaNotes: "Sorunsuz indüksiyon",
                IntraoperativeFindings: "Kolesistit bulguları",
                IntraoperativeComplications: null,
                EstimatedBloodLossMl: 30,
                SpecimensCollected: "Safra Kesesi Patoloji",
                CountsConfirmed: true,
                PostOpDisposition: PostOpDisposition.PACU,
                PostOpInstructions: "Oral stop, vital takip");

            var editAttempt = await service.SaveRecordAsync(saveDto, staffId);
            Assert.False(editAttempt.IsSuccess);
            Assert.Equal(SurgeryOperationStatus.Conflict, editAttempt.Status);
        }

        // 4. Add Correction
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new PerioperativeRecordService(db, _auditPublisher, _timeProvider);
            var corrDto = new AddPerioperativeCorrectionDto(
                ReasonForCorrection: "Drenaj miktarı eklendi",
                CorrectionNote: "Jackson-Pratt dren yerleştirildi, 20 cc seröz vasıfta.");

            var corrResult = await service.AddCorrectionAsync(recordId, corrDto, staffId);
            Assert.True(corrResult.IsSuccess);
            Assert.Equal("Drenaj miktarı eklendi", corrResult.Value!.ReasonForCorrection);

            // Verify history via query
            var finalRecord = await service.GetByIdAsync(recordId);
            Assert.NotNull(finalRecord);
            Assert.Single(finalRecord.Corrections);
        }
    }

    private sealed class FakeAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
