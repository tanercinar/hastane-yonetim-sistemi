using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;

public sealed class SpecialtyCareDbContext : DbContext
{
    public SpecialtyCareDbContext(DbContextOptions<SpecialtyCareDbContext> options)
        : base(options)
    {
    }

    public DbSet<PregnancyEpisode> PregnancyEpisodes => Set<PregnancyEpisode>();
    public DbSet<AntenatalVisit> AntenatalVisits => Set<AntenatalVisit>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();
    public DbSet<NewbornRecord> NewbornRecords => Set<NewbornRecord>();
    public DbSet<DentalToothCondition> DentalToothConditions => Set<DentalToothCondition>();
    public DbSet<DentalProcedure> DentalProcedures => Set<DentalProcedure>();
    public DbSet<DentalExaminationRecord> DentalExaminations => Set<DentalExaminationRecord>();
    public DbSet<HomeHealthVisit> HomeHealthVisits => Set<HomeHealthVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("specialty");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpecialtyCareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
