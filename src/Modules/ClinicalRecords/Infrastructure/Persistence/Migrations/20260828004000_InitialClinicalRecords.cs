using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialClinicalRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "clinical_records");

        migrationBuilder.CreateTable(
            name: "encounters",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                primary_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_type = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                planned_start_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                actual_start_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                actual_end_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                chief_complaint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_encounters", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "encounter_participants",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<int>(type: "integer", nullable: false),
                joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                left_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_encounter_participants", x => x.id);
                table.ForeignKey(
                    name: "FK_encounter_participants_encounters_encounter_id",
                    column: x => x.encounter_id,
                    principalSchema: "clinical_records",
                    principalTable: "encounters",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_encounters_department_id",
            schema: "clinical_records",
            table: "encounters",
            column: "department_id");

        migrationBuilder.CreateIndex(
            name: "ix_encounters_patient_id",
            schema: "clinical_records",
            table: "encounters",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_encounters_primary_practitioner_id",
            schema: "clinical_records",
            table: "encounters",
            column: "primary_practitioner_id");

        migrationBuilder.CreateIndex(
            name: "ix_encounters_status",
            schema: "clinical_records",
            table: "encounters",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ux_encounters_appointment_active",
            schema: "clinical_records",
            table: "encounters",
            column: "appointment_id",
            unique: true,
            filter: "\"appointment_id\" IS NOT NULL AND \"status\" IN (1, 2, 3)");

        migrationBuilder.CreateIndex(
            name: "ix_encounter_participants_encounter_id",
            schema: "clinical_records",
            table: "encounter_participants",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_encounter_participants_practitioner_id",
            schema: "clinical_records",
            table: "encounter_participants",
            column: "practitioner_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "encounter_participants",
            schema: "clinical_records");

        migrationBuilder.DropTable(
            name: "encounters",
            schema: "clinical_records");
    }
}
