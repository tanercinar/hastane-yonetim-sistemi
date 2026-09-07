using HospitalManagement.Modules.Scheduling.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
{
    public const string Schema = "scheduling";
    public const string MigrationHistoryTable = "__EFMigrationsHistory_Scheduling";

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<ScheduleBreak> ScheduleBreaks => Set<ScheduleBreak>();

    public DbSet<DoctorLeaveBlock> DoctorLeaveBlocks => Set<DoctorLeaveBlock>();

    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
