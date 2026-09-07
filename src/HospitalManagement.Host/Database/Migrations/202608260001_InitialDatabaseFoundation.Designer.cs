using HospitalManagement.Host.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HospitalManagement.Host.Database.Migrations;

[DbContext(typeof(DatabaseBootstrapDbContext))]
[Migration("202608260001_InitialDatabaseFoundation")]
partial class InitialDatabaseFoundation
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("platform")
            .HasAnnotation("ProductVersion", "10.0.11");
#pragma warning restore 612, 618
    }
}
