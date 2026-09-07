using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddClinicalAttachments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "clinical_attachments",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                uploaded_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                attachment_type = table.Column<int>(type: "integer", nullable: false),
                file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                byte_size = table.Column<long>(type: "bigint", nullable: false),
                sha256_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_entered_in_error = table.Column<bool>(type: "boolean", nullable: false),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clinical_attachments", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_clinical_attachments_encounter_id",
            schema: "clinical_records",
            table: "clinical_attachments",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_attachments_patient_id",
            schema: "clinical_records",
            table: "clinical_attachments",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_clinical_attachments_sha256_checksum",
            schema: "clinical_records",
            table: "clinical_attachments",
            column: "sha256_checksum");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "clinical_attachments",
            schema: "clinical_records");
    }
}
