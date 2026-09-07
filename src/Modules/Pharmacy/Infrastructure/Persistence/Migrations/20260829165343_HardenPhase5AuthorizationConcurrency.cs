using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenPhase5AuthorizationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_medication_stock_items_catalog_location_lot",
                schema: "pharmacy",
                table: "medication_stock_items");

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                schema: "pharmacy",
                table: "medication_stock_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE pharmacy.medication_stock_items " +
                "SET department_id = '30000000-0000-0000-0000-000000000007' " +
                "WHERE department_id = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateTable(
                name: "prescription_dispense_operations",
                schema: "pharmacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_dispense_operations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_medication_stock_items_department_id",
                schema: "pharmacy",
                table: "medication_stock_items",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ux_medication_stock_items_catalog_location_lot",
                schema: "pharmacy",
                table: "medication_stock_items",
                columns: new[] { "department_id", "medication_catalog_item_id", "location", "lot_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_medication_stock_items_reserved_not_above_on_hand",
                schema: "pharmacy",
                table: "medication_stock_items",
                sql: "quantity_reserved <= quantity_on_hand");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_dispense_operations_prescription_id",
                schema: "pharmacy",
                table: "prescription_dispense_operations",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ux_prescription_dispense_operations_idempotency_key",
                schema: "pharmacy",
                table: "prescription_dispense_operations",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prescription_dispense_operations",
                schema: "pharmacy");

            migrationBuilder.DropIndex(
                name: "ix_medication_stock_items_department_id",
                schema: "pharmacy",
                table: "medication_stock_items");

            migrationBuilder.DropIndex(
                name: "ux_medication_stock_items_catalog_location_lot",
                schema: "pharmacy",
                table: "medication_stock_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_medication_stock_items_reserved_not_above_on_hand",
                schema: "pharmacy",
                table: "medication_stock_items");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "pharmacy",
                table: "medication_stock_items");

            migrationBuilder.CreateIndex(
                name: "ux_medication_stock_items_catalog_location_lot",
                schema: "pharmacy",
                table: "medication_stock_items",
                columns: new[] { "medication_catalog_item_id", "location", "lot_number" },
                unique: true);

        }
    }
}
