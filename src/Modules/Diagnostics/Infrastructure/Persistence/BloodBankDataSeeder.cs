using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

public sealed class BloodBankDataSeeder : IBloodBankDataSeeder
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public BloodBankDataSeeder(DiagnosticsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.BloodUnits.AnyAsync(cancellationToken))
        {
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var demoUnits = new List<BloodUnit>
        {
            // O Negative (Universal RBC)
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000001"),
                "DEMO-BLD-2026-ONEG-01",
                BloodProductType.RedBloodCells,
                BloodGroup.ONegative,
                450,
                nowUtc.AddDays(-5),
                nowUtc.AddDays(37),
                "Dolap-A / Raf-1 (2-6°C)",
                nowUtc),
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000002"),
                "DEMO-BLD-2026-ONEG-02",
                BloodProductType.RedBloodCells,
                BloodGroup.ONegative,
                450,
                nowUtc.AddDays(-2),
                nowUtc.AddDays(40),
                "Dolap-A / Raf-1 (2-6°C)",
                nowUtc),

            // O Positive
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                "DEMO-BLD-2026-OPOS-01",
                BloodProductType.RedBloodCells,
                BloodGroup.OPositive,
                450,
                nowUtc.AddDays(-10),
                nowUtc.AddDays(32),
                "Dolap-A / Raf-2 (2-6°C)",
                nowUtc),

            // A Positive
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                "DEMO-BLD-2026-APOS-01",
                BloodProductType.RedBloodCells,
                BloodGroup.APositive,
                450,
                nowUtc.AddDays(-4),
                nowUtc.AddDays(38),
                "Dolap-B / Raf-1 (2-6°C)",
                nowUtc),

            // A Negative
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000005"),
                "DEMO-BLD-2026-ANEG-01",
                BloodProductType.RedBloodCells,
                BloodGroup.ANegative,
                450,
                nowUtc.AddDays(-3),
                nowUtc.AddDays(39),
                "Dolap-B / Raf-2 (2-6°C)",
                nowUtc),

            // B Positive
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000006"),
                "DEMO-BLD-2026-BPOS-01",
                BloodProductType.RedBloodCells,
                BloodGroup.BPositive,
                450,
                nowUtc.AddDays(-6),
                nowUtc.AddDays(36),
                "Dolap-C / Raf-1 (2-6°C)",
                nowUtc),

            // AB Positive (Universal Recipient RBC / Universal Donor FFP)
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000007"),
                "DEMO-BLD-2026-ABPOS-01",
                BloodProductType.RedBloodCells,
                BloodGroup.ABPositive,
                450,
                nowUtc.AddDays(-1),
                nowUtc.AddDays(41),
                "Dolap-C / Raf-2 (2-6°C)",
                nowUtc),

            // Fresh Frozen Plasma (Taze Donmuş Plazma)
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000008"),
                "DEMO-BLD-2026-FFP-AB-01",
                BloodProductType.FreshFrozenPlasma,
                BloodGroup.ABPositive,
                250,
                nowUtc.AddDays(-20),
                nowUtc.AddDays(345),
                "Derin Dondurucu-1 (-25°C)",
                nowUtc),

            // Platelets (Trombosit Süspansiyonu)
            BloodUnit.Create(
                Guid.Parse("a1000000-0000-0000-0000-000000000009"),
                "DEMO-BLD-2026-PLT-OPOS-01",
                BloodProductType.Platelets,
                BloodGroup.OPositive,
                200,
                nowUtc.AddDays(-1),
                nowUtc.AddDays(4),
                "Trombosit Çalkalayıcı (20-24°C)",
                nowUtc),
        };

        _dbContext.BloodUnits.AddRange(demoUnits);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
