using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRadiologyCatalogAndStudies : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "radiology_catalog_items",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                modality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                body_site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                preparation_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                contrast_required = table.Column<bool>(type: "boolean", nullable: false),
                estimated_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_radiology_catalog_items", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "radiology_studies",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                accession_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                modality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                procedure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                procedure_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                body_site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                scheduled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                technician_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                technician_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                radiologist_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                report_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                impression = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                report_drafted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                report_finalized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                addendum_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                addendum_added_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                addendum_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_radiology_studies", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_radiology_catalog_items_code",
            schema: "diagnostics",
            table: "radiology_catalog_items",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_radiology_catalog_items_is_active",
            schema: "diagnostics",
            table: "radiology_catalog_items",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_catalog_items_modality",
            schema: "diagnostics",
            table: "radiology_catalog_items",
            column: "modality");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_accession_number",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "accession_number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_diagnostic_order_id",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "diagnostic_order_id");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_diagnostic_order_item_id",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "diagnostic_order_item_id");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_modality",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "modality");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_patient_id",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "IX_radiology_studies_status",
            schema: "diagnostics",
            table: "radiology_studies",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "radiology_catalog_items",
            schema: "diagnostics");

        migrationBuilder.DropTable(
            name: "radiology_studies",
            schema: "diagnostics");
    }
}
