using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class SurgeryDataSeeder : ISurgeryDataSeeder
{
    private static readonly (Guid Id, string Code, string Name, int Capacity)[] SeedRooms =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000401"), "DEMO-OR-01", "Ameliyathane Salon 1 - Genel Cerrahi & Laparoskopi", 1),
        (Guid.Parse("00000000-0000-0000-0000-000000000402"), "DEMO-OR-02", "Ameliyathane Salon 2 - Kalp Damar & Göğüs Cerrahisi", 1),
        (Guid.Parse("00000000-0000-0000-0000-000000000403"), "DEMO-OR-03", "Ameliyathane Salon 3 - Ortopedi & Acil Travma Salonu", 1),
    ];

    private static readonly (Guid Id, string Code, string Name)[] SeedIcuBeds =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000501"), "DEMO-ICU-01", "Yoğun Bakım Yatak 1 (İnvaziv Monitörizasyon & Ventilatör)"),
        (Guid.Parse("00000000-0000-0000-0000-000000000502"), "DEMO-ICU-02", "Yoğun Bakım Yatak 2 (İnvaziv Monitörizasyon & Ventilatör)"),
        (Guid.Parse("00000000-0000-0000-0000-000000000503"), "DEMO-ICU-03", "Yoğun Bakım Yatak 3 (İzolasyon & Negatif Basınç)"),
        (Guid.Parse("00000000-0000-0000-0000-000000000504"), "DEMO-ICU-04", "Yoğun Bakım Yatak 4 (Post-Op Koroner & Cerrahi)"),
        (Guid.Parse("00000000-0000-0000-0000-000000000505"), "DEMO-ICU-05", "Yoğun Bakım Yatak 5 (Basamak 2 Yoğun Bakım)"),
        (Guid.Parse("00000000-0000-0000-0000-000000000506"), "DEMO-ICU-06", "Yoğun Bakım Yatak 6 (Basamak 2 Yoğun Bakım)"),
    ];

    private readonly SurgeryDbContext _dbContext;

    public SurgeryDataSeeder(SurgeryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (id, code, name, capacity) in SeedRooms)
        {
            var exists = await _dbContext.OperatingRooms.AnyAsync(r => r.Id == id, cancellationToken);
            if (!exists)
            {
                var room = OperatingRoom.Create(id, code, name, capacity);
                _dbContext.OperatingRooms.Add(room);
            }
        }

        foreach (var (id, code, name) in SeedIcuBeds)
        {
            var exists = await _dbContext.IcuBeds.AnyAsync(b => b.Id == id, cancellationToken);
            if (!exists)
            {
                var bed = IcuBed.Create(id, code, name);
                _dbContext.IcuBeds.Add(bed);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
