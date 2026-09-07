using HospitalManagement.BuildingBlocks.Infrastructure.Persistence;
using HospitalManagement.BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests;

public sealed class ModuleDbContextTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F01-G10")]
    public void SaveChangesEnforcesUtcAndInitializesConcurrencyVersion()
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseInMemoryDatabase($"f01-g10-{Guid.NewGuid():N}")
            .Options;

        using var context = new ProbeDbContext(options);
        var validProbe = new Probe
        {
            Id = Guid.NewGuid(),
            RecordedAtUtc = new(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc),
        };

        context.Add(validProbe);
        Assert.Equal(1, context.SaveChanges());
        Assert.Equal(1, validProbe.Version);

        context.Add(new Probe
        {
            Id = Guid.NewGuid(),
            RecordedAtUtc = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Local),
        });

        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Contains("must contain a UTC timestamp", exception.Message, StringComparison.Ordinal);
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options)
        : ModuleDbContext(options)
    {
        protected override void ConfigureModuleModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Probe>().HasKey(probe => probe.Id);
        }
    }

    private sealed class Probe : IHasConcurrencyVersion
    {
        public Guid Id
        {
            get; set;
        }

        public DateTime RecordedAtUtc
        {
            get; set;
        }

        public long Version
        {
            get; set;
        }
    }
}
