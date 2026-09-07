using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMedicationAdministrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "medication_administrations",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                MedicationName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Dose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Route = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ScheduledTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AdministeredByNurseId = table.Column<Guid>(type: "uuid", nullable: true),
                AdministeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Verified5Rights = table.Column<bool>(type: "boolean", nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_medication_administrations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_medication_administrations_AdmissionId",
            schema: "inpatient",
            table: "medication_administrations",
            column: "AdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_medication_administrations_PatientId",
            schema: "inpatient",
            table: "medication_administrations",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_medication_administrations_ScheduledTimeUtc",
            schema: "inpatient",
            table: "medication_administrations",
            column: "ScheduledTimeUtc");

        migrationBuilder.CreateIndex(
            name: "IX_medication_administrations_Status",
            schema: "inpatient",
            table: "medication_administrations",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "medication_administrations",
            schema: "inpatient");
    }
}
