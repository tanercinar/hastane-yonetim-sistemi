using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class OperatingRoomConfiguration : IEntityTypeConfiguration<OperatingRoom>
{
    public void Configure(EntityTypeBuilder<OperatingRoom> builder)
    {
        builder.ToTable("OperatingRooms", "surgery");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoomCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(r => r.RoomCode)
            .IsUnique();

        builder.Property(r => r.RoomName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.Version)
            .IsRowVersion();
    }
}
