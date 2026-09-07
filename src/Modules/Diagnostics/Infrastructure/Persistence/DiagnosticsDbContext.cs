using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

public sealed class DiagnosticsDbContext(DbContextOptions<DiagnosticsDbContext> options) : DbContext(options)
{
    public DbSet<DiagnosticOrder> DiagnosticOrders => Set<DiagnosticOrder>();
    public DbSet<DiagnosticOrderItem> DiagnosticOrderItems => Set<DiagnosticOrderItem>();
    public DbSet<LabCatalogItem> LabCatalogItems => Set<LabCatalogItem>();
    public DbSet<LabCatalogParameter> LabCatalogParameters => Set<LabCatalogParameter>();
    public DbSet<Specimen> Specimens => Set<Specimen>();
    public DbSet<SpecimenTransitionEvent> SpecimenTransitionEvents => Set<SpecimenTransitionEvent>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<LabResultItem> LabResultItems => Set<LabResultItem>();
    public DbSet<CriticalResultNotification> CriticalResultNotifications => Set<CriticalResultNotification>();
    public DbSet<RadiologyCatalogItem> RadiologyCatalogItems => Set<RadiologyCatalogItem>();
    public DbSet<RadiologyStudy> RadiologyStudies => Set<RadiologyStudy>();
    public DbSet<PathologyCase> PathologyCases => Set<PathologyCase>();
    public DbSet<BloodUnit> BloodUnits => Set<BloodUnit>();
    public DbSet<CrossmatchRequest> CrossmatchRequests => Set<CrossmatchRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("diagnostics");

        modelBuilder.ApplyConfiguration(new DiagnosticOrderConfiguration());
        modelBuilder.ApplyConfiguration(new DiagnosticOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new LabCatalogItemConfiguration());
        modelBuilder.ApplyConfiguration(new LabCatalogParameterConfiguration());
        modelBuilder.ApplyConfiguration(new SpecimenConfiguration());
        modelBuilder.ApplyConfiguration(new SpecimenTransitionEventConfiguration());
        modelBuilder.ApplyConfiguration(new LabResultConfiguration());
        modelBuilder.ApplyConfiguration(new LabResultItemConfiguration());
        modelBuilder.ApplyConfiguration(new CriticalResultNotificationConfiguration());
        modelBuilder.ApplyConfiguration(new RadiologyCatalogItemConfiguration());
        modelBuilder.ApplyConfiguration(new RadiologyStudyConfiguration());
        modelBuilder.ApplyConfiguration(new PathologyCaseConfiguration());
        modelBuilder.ApplyConfiguration(new BloodUnitConfiguration());
        modelBuilder.ApplyConfiguration(new CrossmatchRequestConfiguration());
    }
}
