using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAllergiesAndProblems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "allergy_intolerances",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                substance = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                category = table.Column<int>(type: "integer", nullable: false),
                criticality = table.Column<int>(type: "integer", nullable: false),
                clinical_status = table.Column<int>(type: "integer", nullable: false),
                verification_status = table.Column<int>(type: "integer", nullable: false),
                manifestation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                onset_date_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                recorded_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_allergy_intolerances", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "clinical_problems",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                problem_title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                category = table.Column<int>(type: "integer", nullable: false),
                clinical_status = table.Column<int>(type: "integer", nullable: false),
                verification_status = table.Column<int>(type: "integer", nullable: false),
                onset_date = table.Column<DateOnly>(type: "date", nullable: true),
                resolved_date = table.Column<DateOnly>(type: "date", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                recorded_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clinical_problems", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_allergy_intolerances_clinical_status",
            schema: "clinical_records",
            table: "allergy_intolerances",
            column: "clinical_status");

        migrationBuilder.CreateIndex(
            name: "ix_allergy_intolerances_patient_id",
            schema: "clinical_records",
            table: "allergy_intolerances",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_problems_category",
            schema: "clinical_records",
            table: "clinical_problems",
            column: "category");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_problems_clinical_status",
            schema: "clinical_records",
            table: "clinical_problems",
            column: "clinical_status");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_problems_patient_id",
            schema: "clinical_records",
            table: "clinical_problems",
            column: "patient_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "allergy_intolerances",
            schema: "clinical_records");

        migrationBuilder.DropTable(
            name: "clinical_problems",
            schema: "clinical_records");
    }
}
