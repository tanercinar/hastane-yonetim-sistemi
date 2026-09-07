using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SurgeryDbContext))]
[Migration("20260831151000_HardenPhase8SurgeryIntegrity")]
public partial class HardenPhase8SurgeryIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "UX_IcuAdmissions_ActiveBed"
            ON surgery."IcuAdmissions" ("IcuBedId")
            WHERE "Status" = 'Active';

            CREATE UNIQUE INDEX "UX_IcuAdmissions_ActivePatient"
            ON surgery."IcuAdmissions" ("PatientId")
            WHERE "Status" = 'Active';

            CREATE UNIQUE INDEX "UX_ClinicalHandoffs_PendingPatient"
            ON surgery."ClinicalHandoffs" ("PatientId")
            WHERE "Status" = 'PendingAcceptance';
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE surgery."SurgeryBookings"
            ADD CONSTRAINT "EX_SurgeryBookings_ActiveRoomTime"
            EXCLUDE USING gist
            (
                "OperatingRoomId" WITH =,
                tstzrange("ScheduledStartTimeUtc", "ScheduledEndTimeUtc", '[)') WITH &&
            )
            WHERE ("Status" IN ('Scheduled', 'PreOpCleared', 'InProgress'));

            ALTER TABLE surgery."SurgeryBookings"
            ADD CONSTRAINT "EX_SurgeryBookings_ActiveLeadSurgeonTime"
            EXCLUDE USING gist
            (
                "LeadSurgeonDoctorId" WITH =,
                tstzrange("ScheduledStartTimeUtc", "ScheduledEndTimeUtc", '[)') WITH &&
            )
            WHERE ("Status" IN ('Scheduled', 'PreOpCleared', 'InProgress'));

            ALTER TABLE surgery."SurgeryBookings"
            ADD CONSTRAINT "EX_SurgeryBookings_ActiveAnesthesiologistTime"
            EXCLUDE USING gist
            (
                "AnesthesiologistDoctorId" WITH =,
                tstzrange("ScheduledStartTimeUtc", "ScheduledEndTimeUtc", '[)') WITH &&
            )
            WHERE ("Status" IN ('Scheduled', 'PreOpCleared', 'InProgress'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE surgery."SurgeryBookings"
                DROP CONSTRAINT IF EXISTS "EX_SurgeryBookings_ActiveAnesthesiologistTime";
            ALTER TABLE surgery."SurgeryBookings"
                DROP CONSTRAINT IF EXISTS "EX_SurgeryBookings_ActiveLeadSurgeonTime";
            ALTER TABLE surgery."SurgeryBookings"
                DROP CONSTRAINT IF EXISTS "EX_SurgeryBookings_ActiveRoomTime";

            DROP INDEX IF EXISTS surgery."UX_ClinicalHandoffs_PendingPatient";
            DROP INDEX IF EXISTS surgery."UX_IcuAdmissions_ActivePatient";
            DROP INDEX IF EXISTS surgery."UX_IcuAdmissions_ActiveBed";
            """);
    }
}
