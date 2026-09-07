using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMedicationStockAndTransactions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "medication_stock_items",
            schema: "pharmacy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                location = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                medication_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                expiration_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                quantity_reserved = table.Column<int>(type: "integer", nullable: false),
                reorder_level = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_medication_stock_items", x => x.id);
                table.CheckConstraint("ck_medication_stock_items_quantity_on_hand", "quantity_on_hand >= 0");
                table.CheckConstraint("ck_medication_stock_items_quantity_reserved", "quantity_reserved >= 0");
                table.CheckConstraint("ck_medication_stock_items_reorder_level", "reorder_level >= 0");
            });

        migrationBuilder.CreateTable(
            name: "medication_stock_transactions",
            schema: "pharmacy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                transaction_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                previous_quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                new_quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                reference_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                performed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_medication_stock_transactions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_medication_stock_items_catalog_location_lot",
            schema: "pharmacy",
            table: "medication_stock_items",
            columns: new[] { "medication_catalog_item_id", "location", "lot_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_medication_stock_items_fefo",
            schema: "pharmacy",
            table: "medication_stock_items",
            columns: new[] { "medication_catalog_item_id", "expiration_date_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_medication_stock_transactions_stock_item_id",
            schema: "pharmacy",
            table: "medication_stock_transactions",
            column: "stock_item_id");

        migrationBuilder.CreateIndex(
            name: "ix_medication_stock_transactions_performed_at",
            schema: "pharmacy",
            table: "medication_stock_transactions",
            column: "performed_at_utc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "medication_stock_transactions",
            schema: "pharmacy");

        migrationBuilder.DropTable(
            name: "medication_stock_items",
            schema: "pharmacy");
    }
}
