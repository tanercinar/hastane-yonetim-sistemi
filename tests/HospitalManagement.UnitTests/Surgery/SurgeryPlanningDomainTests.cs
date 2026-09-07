using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Surgery;

public sealed class SurgeryPlanningDomainTests
{
    private readonly DbContextOptions<SurgeryDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public SurgeryPlanningDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<SurgeryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    [Trait("Roadmap", "F08-G04")]
    public void SurgeryBookingCreationAndValidationSucceeds()
    {
        var bookingId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var surgeonId = Guid.NewGuid();
        var anesthesiologistId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var start = now.AddHours(2);
        var end = now.AddHours(4);

        var booking = SurgeryBooking.Create(
            bookingId,
            patientId,
            null,
            deptId,
            "Genel Cerrahi",
            "Laparoskopik Kolesistektomi",
            "DEMO-PRC-CHOLE",
            SurgeryUrgency.Elective,
            roomId,
            surgeonId,
            anesthesiologistId,
            null,
            start,
            end,
            "USG hazır",
            now);

        Assert.Equal(bookingId, booking.Id);
        Assert.StartsWith("DEMO-SURG-", booking.BookingProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(SurgeryBookingStatus.Scheduled, booking.Status);
        Assert.Null(booking.PreOpChecklist);

        // Cannot have same surgeon and anesthesiologist
        Assert.Throws<ArgumentException>(() => SurgeryBooking.Create(
            Guid.NewGuid(),
            patientId,
            null,
            deptId,
            "Genel Cerrahi",
            "Apendektomi",
            "DEMO-PRC-APP",
            SurgeryUrgency.Emergency,
            roomId,
            surgeonId,
            surgeonId, // Same ID
            null,
            start,
            end,
            null,
            now));
    }

    [Fact]
    [Trait("Roadmap", "F08-G04")]
    public void PreOpChecklistFullClearancePromotesStatusToPreOpCleared()
    {
        var booking = CreateSampleBooking();
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var partialChecklist = new PreOpChecklistInfo(
            ConsentSigned: true,
            AnesthesiaClearance: true,
            NpoConfirmed: false, // Incomplete
            BloodProductsReserved: true,
            SiteMarked: true,
            AllergyChecked: true,
            CompletedByStaffId: staffId,
            CompletedAtUtc: now,
            Notes: "Açlık süresi eksik");

        booking.RecordPreOpChecklist(partialChecklist, now);
        Assert.False(partialChecklist.IsFullyCleared);
        Assert.Equal(SurgeryBookingStatus.Scheduled, booking.Status);

        var fullChecklist = new PreOpChecklistInfo(
            ConsentSigned: true,
            AnesthesiaClearance: true,
            NpoConfirmed: true, // Complete
            BloodProductsReserved: true,
            SiteMarked: true,
            AllergyChecked: true,
            CompletedByStaffId: staffId,
            CompletedAtUtc: now,
            Notes: "Tüm kontroller eksiksiz tamamlandı");

        booking.RecordPreOpChecklist(fullChecklist, now);
        Assert.True(fullChecklist.IsFullyCleared);
        Assert.Equal(SurgeryBookingStatus.PreOpCleared, booking.Status);
    }

    [Fact]
    [Trait("Roadmap", "F08-G04")]
    public async Task SurgeryPlanningServiceDetectsRoomCollisionAndRejectsAtomically()
    {
        using var db = new SurgeryDbContext(_dbOptions);
        var roomId = Guid.NewGuid();
        var room = OperatingRoom.Create(roomId, "DEMO-OR-01", "Salon 1", 1);
        db.OperatingRooms.Add(room);
        await db.SaveChangesAsync();

        var service = new SurgeryPlanningService(db, _auditPublisher, _timeProvider);
        var baseDate = DateTime.UtcNow.Date.AddDays(1);

        // Booking 1: 09:00 - 11:00
        var dto1 = new CreateSurgeryBookingDto(
            PatientId: Guid.NewGuid(),
            EncounterId: null,
            DepartmentId: Guid.NewGuid(),
            DepartmentName: "Genel Cerrahi",
            ProcedureName: "Kolesistektomi",
            ProcedureCode: "DEMO-PRC-CHOLE",
            Urgency: SurgeryUrgency.Elective,
            OperatingRoomId: roomId,
            LeadSurgeonDoctorId: Guid.NewGuid(),
            AnesthesiologistDoctorId: Guid.NewGuid(),
            OperatingNurseStaffId: null,
            ScheduledStartTimeUtc: baseDate.AddHours(9),
            ScheduledEndTimeUtc: baseDate.AddHours(11),
            ClinicalNotes: null);

        var result1 = await service.CreateBookingAsync(dto1, Guid.NewGuid());
        Assert.True(result1.IsSuccess);

        // Booking 2: 10:00 - 12:00 in SAME ROOM -> Collides with Booking 1
        var dto2 = new CreateSurgeryBookingDto(
            PatientId: Guid.NewGuid(),
            EncounterId: null,
            DepartmentId: Guid.NewGuid(),
            DepartmentName: "Genel Cerrahi",
            ProcedureName: "Apendektomi",
            ProcedureCode: "DEMO-PRC-APP",
            Urgency: SurgeryUrgency.Expedited,
            OperatingRoomId: roomId,
            LeadSurgeonDoctorId: Guid.NewGuid(),
            AnesthesiologistDoctorId: Guid.NewGuid(),
            OperatingNurseStaffId: null,
            ScheduledStartTimeUtc: baseDate.AddHours(10),
            ScheduledEndTimeUtc: baseDate.AddHours(12),
            ClinicalNotes: null);

        var result2 = await service.CreateBookingAsync(dto2, Guid.NewGuid());
        Assert.False(result2.IsSuccess);
        Assert.Equal(SurgeryOperationStatus.Conflict, result2.Status);
        Assert.Contains("DEMO-OR-01", result2.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Roadmap", "F08-G04")]
    public async Task SurgeryPlanningServiceDetectsSurgeonCollisionAndRejectsAtomically()
    {
        using var db = new SurgeryDbContext(_dbOptions);
        var room1Id = Guid.NewGuid();
        var room2Id = Guid.NewGuid();
        var surgeonId = Guid.NewGuid();

        db.OperatingRooms.Add(OperatingRoom.Create(room1Id, "DEMO-OR-01", "Salon 1", 1));
        db.OperatingRooms.Add(OperatingRoom.Create(room2Id, "DEMO-OR-02", "Salon 2", 1));
        await db.SaveChangesAsync();

        var service = new SurgeryPlanningService(db, _auditPublisher, _timeProvider);
        var baseDate = DateTime.UtcNow.Date.AddDays(1);

        // Surgeon in Room 1: 14:00 - 16:00
        var dto1 = new CreateSurgeryBookingDto(
            PatientId: Guid.NewGuid(),
            EncounterId: null,
            DepartmentId: Guid.NewGuid(),
            DepartmentName: "Ortopedi",
            ProcedureName: "Diz Artroskopisi",
            ProcedureCode: "DEMO-PRC-ARTHRO",
            Urgency: SurgeryUrgency.Elective,
            OperatingRoomId: room1Id,
            LeadSurgeonDoctorId: surgeonId,
            AnesthesiologistDoctorId: Guid.NewGuid(),
            OperatingNurseStaffId: null,
            ScheduledStartTimeUtc: baseDate.AddHours(14),
            ScheduledEndTimeUtc: baseDate.AddHours(16),
            ClinicalNotes: null);

        var result1 = await service.CreateBookingAsync(dto1, Guid.NewGuid());
        Assert.True(result1.IsSuccess);

        // Same Surgeon in Room 2: 15:00 - 17:00 -> Collides!
        var dto2 = new CreateSurgeryBookingDto(
            PatientId: Guid.NewGuid(),
            EncounterId: null,
            DepartmentId: Guid.NewGuid(),
            DepartmentName: "Ortopedi",
            ProcedureName: "Menisektomi",
            ProcedureCode: "DEMO-PRC-MENISC",
            Urgency: SurgeryUrgency.Elective,
            OperatingRoomId: room2Id,
            LeadSurgeonDoctorId: surgeonId,
            AnesthesiologistDoctorId: Guid.NewGuid(),
            OperatingNurseStaffId: null,
            ScheduledStartTimeUtc: baseDate.AddHours(15),
            ScheduledEndTimeUtc: baseDate.AddHours(17),
            ClinicalNotes: null);

        var result2 = await service.CreateBookingAsync(dto2, Guid.NewGuid());
        Assert.False(result2.IsSuccess);
        Assert.Equal(SurgeryOperationStatus.Conflict, result2.Status);
        Assert.Contains("Sorumlu cerrah", result2.ErrorMessage, StringComparison.Ordinal);
    }

    private static SurgeryBooking CreateSampleBooking()
    {
        var now = DateTime.UtcNow;
        return SurgeryBooking.Create(
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
            now.AddHours(2),
            now.AddHours(4),
            null,
            now);
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
