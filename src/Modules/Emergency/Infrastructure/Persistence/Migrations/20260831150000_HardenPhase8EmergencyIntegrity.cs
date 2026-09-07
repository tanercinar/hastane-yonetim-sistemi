using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EmergencyDbContext))]
[Migration("20260831150000_HardenPhase8EmergencyIntegrity")]
public partial class HardenPhase8EmergencyIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "UX_EmergencyAdmissions_ActivePatient"
            ON emergency."EmergencyAdmissions" ("PatientId")
            WHERE "Status" IN ('WaitingTriage', 'TriagedWaitingDoctor', 'InEvaluation', 'InObservation');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS emergency."UX_EmergencyAdmissions_ActivePatient";
            """);
    }
}
