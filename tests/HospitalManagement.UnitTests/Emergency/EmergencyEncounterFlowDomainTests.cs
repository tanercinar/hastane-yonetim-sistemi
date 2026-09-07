using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Emergency;

public sealed class EmergencyEncounterFlowDomainTests
{
    private readonly DbContextOptions<EmergencyDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher;
    private readonly FakeEmergencyNotifier _notifier;
    private readonly TestTimeProvider _timeProvider;

    public EmergencyEncounterFlowDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<EmergencyDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmergencyEncounterTests_{Guid.NewGuid()}")
            .Options;

        _auditPublisher = new FakeAuditPublisher();
        _notifier = new FakeEmergencyNotifier();
        _timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Roadmap", "F08-G03")]
    public void EmergencyCareOrderCreationAndStateTransitionsWorkAsExpected()
    {
        var orderId = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var order = EmergencyCareOrder.Create(
            orderId,
            admissionId,
            EmergencyOrderType.Laboratory,
            "LAB-TROP",
            "Troponin I",
            EmergencyOrderPriority.Stat,
            doctorId,
            "Acil göğüs ağrısı",
            now);

        Assert.Equal(orderId, order.Id);
        Assert.Equal(EmergencyOrderStatus.Ordered, order.Status);
        Assert.Equal(EmergencyOrderPriority.Stat, order.Priority);

        order.MarkInProgress(now.AddMinutes(5));
        Assert.Equal(EmergencyOrderStatus.InProgress, order.Status);

        order.Complete("Troponin 0.01 ng/mL (Normal)", now.AddMinutes(20));
        Assert.Equal(EmergencyOrderStatus.Completed, order.Status);
        Assert.Equal("Troponin 0.01 ng/mL (Normal)", order.ResultSummary);
        Assert.NotNull(order.CompletedAtUtc);

        Assert.Throws<InvalidOperationException>(() => order.Cancel("Gerek kalmadı", now.AddMinutes(25)));
    }

    [Fact]
    [Trait("Roadmap", "F08-G03")]
    public void EmergencyConsultationCreationAcceptanceAndResponseWorkAsExpected()
    {
        var consultationId = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var consultation = EmergencyConsultation.Create(
            consultationId,
            admissionId,
            deptId,
            "Kardiyoloji",
            doctorId,
            EmergencyConsultationUrgency.Immediate15Min,
            "Akut koroner sendrom şüphesi",
            now);

        Assert.Equal(EmergencyConsultationStatus.Requested, consultation.Status);
        Assert.Equal(EmergencyConsultationUrgency.Immediate15Min, consultation.Urgency);

        consultation.Accept(consultantId, now.AddMinutes(5));
        Assert.Equal(EmergencyConsultationStatus.Accepted, consultation.Status);
        Assert.Equal(consultantId, consultation.ConsultantDoctorId);

        consultation.Respond(consultantId, "Anjiyografi endikasyonu mevcut, acil kateter laboratuvarına alınıyor.", now.AddMinutes(15));
        Assert.Equal(EmergencyConsultationStatus.Completed, consultation.Status);
        Assert.Equal("Anjiyografi endikasyonu mevcut, acil kateter laboratuvarına alınıyor.", consultation.ConsultationResponseNotes);
        Assert.NotNull(consultation.RespondedAtUtc);
    }

    [Fact]
    [Trait("Roadmap", "F08-G03")]
    public void EmergencyAdmissionRecordDispositionSetsStatusAndDetails()
    {
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyArrivalType.Ambulance,
            "Göğüs ağrısı",
            "112 ile getirildi",
            Guid.NewGuid(),
            DateTime.UtcNow);

        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        admission.RecordDisposition(
            EmergencyDispositionType.AdmitToIcu,
            doctorId,
            Guid.NewGuid(),
            "Koroner Yoğun Bakım",
            "Akut STEMI tanısı ile KBYÜ yatışı planlandı.",
            "Acil koroner anjiyografi sonrası takip",
            now);

        Assert.NotNull(admission.Disposition);
        Assert.Equal(EmergencyDispositionType.AdmitToIcu, admission.Disposition.DispositionType);
        Assert.Equal(EmergencyAdmissionStatus.AdmittedToIcu, admission.Status);
        Assert.Equal("Akut STEMI tanısı ile KBYÜ yatışı planlandı.", admission.DischargeOrDispositionNotes);
        Assert.NotNull(admission.CompletedAtUtc);
    }

    [Fact]
    [Trait("Roadmap", "F08-G03")]
    public async Task EmergencyEncounterServiceOrdersAndConsultationsLifecycleSucceeds()
    {
        using var dbContext = new EmergencyDbContext(_dbOptions);
        var service = new EmergencyEncounterService(dbContext, _auditPublisher, _notifier, _timeProvider);

        var doctorId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            patientId,
            EmergencyArrivalType.WalkIn,
            "Nefes darlığı",
            null,
            staffId,
            _timeProvider.GetUtcNow().UtcDateTime);

        dbContext.Admissions.Add(admission);
        await dbContext.SaveChangesAsync();

        // 1. Create Order
        var orderResult = await service.CreateOrderAsync(
            new CreateEmergencyOrderDto(
                admission.Id,
                EmergencyOrderType.Laboratory,
                "LAB-D-DIMER",
                "D-Dimer",
                EmergencyOrderPriority.Stat,
                "Pulmoner emboli şüphesi"),
            doctorId);

        Assert.Equal(EmergencyOperationStatus.Success, orderResult.Status);
        Assert.NotNull(orderResult.Value);
        Assert.Equal("LAB-D-DIMER", orderResult.Value.OrderCatalogCode);

        // 2. Complete Order
        var completeResult = await service.CompleteOrderAsync(
            orderResult.Value.Id,
            "D-Dimer: 1200 ng/mL (Yüksek)",
            staffId);

        Assert.Equal(EmergencyOperationStatus.Success, completeResult.Status);
        Assert.Equal(EmergencyOrderStatus.Completed, completeResult.Value!.Status);

        // 3. Request Consultation
        var consultResult = await service.RequestConsultationAsync(
            new RequestEmergencyConsultationDto(
                admission.Id,
                Guid.NewGuid(),
                "Göğüs Hastalıkları",
                EmergencyConsultationUrgency.Immediate15Min,
                "Yüksek D-dimer, toraks BT anjiyo değerlendirmesi"),
            doctorId);

        Assert.Equal(EmergencyOperationStatus.Success, consultResult.Status);
        Assert.NotNull(consultResult.Value);

        // 4. Respond Consultation
        var respondResult = await service.RespondConsultationAsync(
            consultResult.Value.Id,
            "BT anjiyoda segmental PE saptandı, antikoagülan başlandı, servis yatışı önerilir.",
            doctorId);

        Assert.Equal(EmergencyOperationStatus.Success, respondResult.Status);
        Assert.Equal(EmergencyConsultationStatus.Completed, respondResult.Value!.Status);

        // 5. Record Disposition
        var dispResult = await service.RecordDispositionAsync(
            admission.Id,
            new RecordEmergencyDispositionDto(
                EmergencyDispositionType.AdmitToWard,
                Guid.NewGuid(),
                "Göğüs Hastalıkları Servisi",
                "Pulmoner emboli tanısı ile yatırıldı.",
                "DMAH tedavisine devam"),
            doctorId);

        Assert.Equal(EmergencyOperationStatus.Success, dispResult.Status);
        Assert.Equal(EmergencyAdmissionStatus.AdmittedToInpatient, dispResult.Value!.Status);

        Assert.True(_notifier.DashboardUpdateNotified);
    }

    private sealed class FakeAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> Events { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmergencyNotifier : IEmergencyRealtimeNotifier
    {
        public bool DashboardUpdateNotified
        {
            get; private set;
        }

        public Task NotifyAdmissionCreatedAsync(Guid admissionId, string protocolNumber, string arrivalType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyTriageRecordedAsync(Guid admissionId, string protocolNumber, string triageLevel, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyDoctorAssignedAsync(Guid admissionId, string protocolNumber, Guid doctorId, string? bedOrZone, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyStatusChangedAsync(Guid admissionId, string protocolNumber, string oldStatus, string newStatus, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyDashboardUpdatedAsync(CancellationToken cancellationToken = default)
        {
            DashboardUpdateNotified = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
