using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialDiagnosticsAndOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "diagnostics");

        migrationBuilder.CreateTable(
            name: "diagnostic_orders",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                placing_doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                clinical_indication = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                order_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                placed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_diagnostic_orders", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "diagnostic_order_items",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                catalog_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                catalog_item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                special_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_diagnostic_order_items", x => x.id);
                table.ForeignKey(
                    name: "FK_diagnostic_order_items_diagnostic_orders_diagnostic_order_id",
                    column: x => x.diagnostic_order_id,
                    principalSchema: "diagnostics",
                    principalTable: "diagnostic_orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_order_items_catalog_code",
            schema: "diagnostics",
            table: "diagnostic_order_items",
            column: "catalog_code");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_order_items_diagnostic_order_id",
            schema: "diagnostics",
            table: "diagnostic_order_items",
            column: "diagnostic_order_id");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_created_at_utc",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_encounter_id",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_order_number",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "order_number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_order_type",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "order_type");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_patient_id",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_orders_status",
            schema: "diagnostics",
            table: "diagnostic_orders",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "diagnostic_order_items",
            schema: "diagnostics");

        migrationBuilder.DropTable(
            name: "diagnostic_orders",
            schema: "diagnostics");
    }
}
