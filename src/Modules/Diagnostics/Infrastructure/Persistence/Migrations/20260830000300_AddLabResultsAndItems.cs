using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddLabResultsAndItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "lab_results",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                specimen_id = table.Column<Guid>(type: "uuid", nullable: true),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                catalog_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                catalog_item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                technically_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                technically_approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                clinically_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                clinically_approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                previous_result_id = table.Column<Guid>(type: "uuid", nullable: true),
                correction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                clinical_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_lab_results", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "lab_result_items",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lab_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                parameter_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                parameter_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                numeric_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                string_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                reference_range_low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                reference_range_high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                reference_range_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                flag = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_lab_result_items", x => x.id);
                table.ForeignKey(
                    name: "fk_lab_result_items_lab_results_lab_result_id",
                    column: x => x.lab_result_id,
                    principalSchema: "diagnostics",
                    principalTable: "lab_results",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_lab_results_created_at_utc",
            schema: "diagnostics",
            table: "lab_results",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "ix_lab_results_diagnostic_order_id",
            schema: "diagnostics",
            table: "lab_results",
            column: "diagnostic_order_id");

        migrationBuilder.CreateIndex(
            name: "ix_lab_results_diagnostic_order_item_id",
            schema: "diagnostics",
            table: "lab_results",
            column: "diagnostic_order_item_id");

        migrationBuilder.CreateIndex(
            name: "ix_lab_results_patient_id",
            schema: "diagnostics",
            table: "lab_results",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_lab_results_status",
            schema: "diagnostics",
            table: "lab_results",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_lab_result_items_lab_result_id",
            schema: "diagnostics",
            table: "lab_result_items",
            column: "lab_result_id");

        migrationBuilder.CreateIndex(
            name: "ix_lab_result_items_parameter_code",
            schema: "diagnostics",
            table: "lab_result_items",
            column: "parameter_code");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "lab_result_items",
            schema: "diagnostics");

        migrationBuilder.DropTable(
            name: "lab_results",
            schema: "diagnostics");
    }
}
