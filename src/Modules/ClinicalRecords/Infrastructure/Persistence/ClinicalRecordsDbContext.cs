using HospitalManagement.Modules.ClinicalRecords.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

public sealed class ClinicalRecordsDbContext(DbContextOptions<ClinicalRecordsDbContext> options) : DbContext(options)
{
    public const string Schema = "clinical_records";
    public const string MigrationHistoryTable = "__EFMigrationsHistory_ClinicalRecords";

    public DbSet<Encounter> Encounters => Set<Encounter>();

    public DbSet<EncounterParticipant> EncounterParticipants => Set<EncounterParticipant>();

    public DbSet<AllergyIntolerance> AllergyIntolerances => Set<AllergyIntolerance>();

    public DbSet<ClinicalProblem> ClinicalProblems => Set<ClinicalProblem>();

    public DbSet<VitalSignObservation> VitalSignObservations => Set<VitalSignObservation>();

    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    public DbSet<DiagnosisCatalogItem> DiagnosisCatalogItems => Set<DiagnosisCatalogItem>();

    public DbSet<EncounterDiagnosis> EncounterDiagnoses => Set<EncounterDiagnosis>();

    public DbSet<ConsultationRequest> ConsultationRequests => Set<ConsultationRequest>();

    public DbSet<ClinicalAttachment> ClinicalAttachments => Set<ClinicalAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicalRecordsDbContext).Assembly);
    }
}
