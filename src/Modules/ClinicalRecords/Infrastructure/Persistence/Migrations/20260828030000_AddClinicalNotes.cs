using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddClinicalNotes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "clinical_notes",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                author_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                note_type = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                chief_complaint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                history_of_present_illness = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                physical_examination = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                assessment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                plan = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                signed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                signed_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: true),
                parent_note_id = table.Column<Guid>(type: "uuid", nullable: true),
                correction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clinical_notes", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_clinical_notes_encounter_id",
            schema: "clinical_records",
            table: "clinical_notes",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_notes_parent_note_id",
            schema: "clinical_records",
            table: "clinical_notes",
            column: "parent_note_id");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_notes_patient_id",
            schema: "clinical_records",
            table: "clinical_notes",
            column: "patient_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "clinical_notes",
            schema: "clinical_records");
    }
}
