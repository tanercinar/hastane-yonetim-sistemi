using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddVitalSigns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "vital_sign_observations",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                measurement_type = table.Column<int>(type: "integer", nullable: false),
                value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                interpretation = table.Column<int>(type: "integer", nullable: false),
                measurement_method = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                measured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                recorded_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_entered_in_error = table.Column<bool>(type: "boolean", nullable: false),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_vital_sign_observations", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_vital_sign_observations_encounter_id",
            schema: "clinical_records",
            table: "vital_sign_observations",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_vital_sign_observations_measured_at_utc",
            schema: "clinical_records",
            table: "vital_sign_observations",
            column: "measured_at_utc");

        migrationBuilder.CreateIndex(
            name: "ix_vital_sign_observations_patient_id",
            schema: "clinical_records",
            table: "vital_sign_observations",
            column: "patient_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "vital_sign_observations",
            schema: "clinical_records");
    }
}
