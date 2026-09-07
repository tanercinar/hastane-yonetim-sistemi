using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.ENabiz;
using HospitalManagement.Modules.Interoperability.Domain.Hl7;
using HospitalManagement.Modules.Interoperability.Domain.Mhrs;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;

public sealed class InteroperabilityDbContext : DbContext
{
    public InteroperabilityDbContext(DbContextOptions<InteroperabilityDbContext> options)
        : base(options)
    {
    }

    public DbSet<MockServerConfiguration> MockServerConfigurations => Set<MockServerConfiguration>();
    public DbSet<IntegrationMessageLog> IntegrationMessageLogs => Set<IntegrationMessageLog>();
    public DbSet<IntegrationCircuitState> IntegrationCircuitStates => Set<IntegrationCircuitState>();
    public DbSet<Hl7DeadLetterEntry> Hl7DeadLetterEntries => Set<Hl7DeadLetterEntry>();
    public DbSet<MhrsAppointmentRecord> MhrsAppointments => Set<MhrsAppointmentRecord>();
    public DbSet<ENabizTransmissionRecord> ENabizTransmissions => Set<ENabizTransmissionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("interoperability");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InteroperabilityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
