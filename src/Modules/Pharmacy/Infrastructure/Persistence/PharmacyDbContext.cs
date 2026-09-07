using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

public class PharmacyDbContext : DbContext
{
    public const string SchemaName = "pharmacy";

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options)
        : base(options)
    {
    }

    public DbSet<MedicationCatalogItem> MedicationCatalogItems => Set<MedicationCatalogItem>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<MedicationStockItem> MedicationStockItems => Set<MedicationStockItem>();
    public DbSet<MedicationStockTransaction> MedicationStockTransactions => Set<MedicationStockTransaction>();
    public DbSet<PrescriptionDispenseOperation> PrescriptionDispenseOperations => Set<PrescriptionDispenseOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PharmacyDbContext).Assembly);
    }
}
