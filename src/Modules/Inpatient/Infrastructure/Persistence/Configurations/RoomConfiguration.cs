using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms", "inpatient");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoomNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.GenderConstraint)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.IsolationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(r => new { r.WardId, r.RoomNumber })
            .IsUnique();

        builder.HasMany(r => r.Beds)
            .WithOne()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
