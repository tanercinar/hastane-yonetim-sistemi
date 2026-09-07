using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public sealed class EmergencyDataSeeder : IEmergencyDataSeeder
{
    public static readonly Guid DemoPatient1Id = Guid.Parse("00000000-0000-0000-0000-000000000109");
    public static readonly Guid DemoPatient2Id = Guid.Parse("00000000-0000-0000-0000-000000000110");
    public static readonly Guid DemoDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid DemoNurseId = Guid.Parse("00000000-0000-0000-0000-000000000103");
    public static readonly Guid DemoStaffId = Guid.Parse("00000000-0000-0000-0000-000000000105");

    public static readonly Guid DemoAdmission1Id = Guid.Parse("60000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoAdmission2Id = Guid.Parse("60000000-0000-0000-0000-000000000002");

    private readonly EmergencyDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EmergencyDataSeeder(EmergencyDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (await _dbContext.Admissions.AnyAsync(cancellationToken))
        {
            return;
        }

        // 1. Admission with Triage (Yellow / Urgent - Göğüs Sıkışması)
        var adm1 = EmergencyAdmission.Create(
            DemoAdmission1Id,
            DemoPatient1Id,
            EmergencyArrivalType.Ambulance,
            "Göğüs ağrısı ve nefes darlığı şikayeti ile 112 tarafından getirildi.",
            "112 nakil formu mevcut. EKG çekildi.",
            DemoStaffId,
            nowUtc.AddMinutes(-45),
            "DEMO-EMG-20260831-100001");

        adm1.RecordTriage(
            TriageLevel.YellowUrgent,
            "Anjinal vasıfta göğüs ağrısı, taşikardi (HR 105), SpO2 %94. Acil hekim değerlendirmesi gerekir.",
            DemoNurseId,
            systolicBp: 145,
            diastolicBp: 90,
            heartRate: 105,
            bodyTemperatureCelsius: 36.8m,
            respiratoryRate: 20,
            oxygenSaturationPercent: 94,
            painScale: 6,
            consciousness: "Alert",
            clinicalNotes: "Monitörize edildi, oksijen 2L/dk başlandı.",
            nowUtc.AddMinutes(-40));

        adm1.AssignDoctor(DemoDoctorId, nowUtc.AddMinutes(-35));
        adm1.AssignBedOrZone("Sarı Alan - Yatak 2", nowUtc.AddMinutes(-35));

        // 2. Admission Waiting Triage (Green / Walk-in - Ayak Bileği Burkulması)
        var adm2 = EmergencyAdmission.Create(
            DemoAdmission2Id,
            DemoPatient2Id,
            EmergencyArrivalType.WalkIn,
            "Sağ ayak bileğinde burkulma, basamama ve şişlik.",
            "Kendi imkanlarıyla başvurdu.",
            DemoStaffId,
            nowUtc.AddMinutes(-15),
            "DEMO-EMG-20260831-100002");

        _dbContext.Admissions.AddRange(adm1, adm2);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
