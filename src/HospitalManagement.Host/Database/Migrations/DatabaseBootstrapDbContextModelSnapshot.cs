using HospitalManagement.Host.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HospitalManagement.Host.Database.Migrations;

[DbContext(typeof(DatabaseBootstrapDbContext))]
internal sealed partial class DatabaseBootstrapDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("platform")
            .HasAnnotation("ProductVersion", "10.0.11");
#pragma warning restore 612, 618
    }
}
