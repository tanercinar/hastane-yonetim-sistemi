using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPharmacyAndMedicationCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "pharmacy");

        migrationBuilder.CreateTable(
            name: "medication_catalog_items",
            schema: "pharmacy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                brand_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                generic_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                form = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                strength_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                strength_unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                route = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                atc_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                catalog_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_medication_catalog_items", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_medication_catalog_items_atc_code",
            schema: "pharmacy",
            table: "medication_catalog_items",
            column: "atc_code");

        migrationBuilder.CreateIndex(
            name: "ix_medication_catalog_items_brand_name",
            schema: "pharmacy",
            table: "medication_catalog_items",
            column: "brand_name");

        migrationBuilder.CreateIndex(
            name: "ix_medication_catalog_items_catalog_version",
            schema: "pharmacy",
            table: "medication_catalog_items",
            column: "catalog_version");

        migrationBuilder.CreateIndex(
            name: "ix_medication_catalog_items_generic_name",
            schema: "pharmacy",
            table: "medication_catalog_items",
            column: "generic_name");

        migrationBuilder.CreateIndex(
            name: "ux_medication_catalog_items_code_version",
            schema: "pharmacy",
            table: "medication_catalog_items",
            columns: new[] { "code", "catalog_version" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "medication_catalog_items",
            schema: "pharmacy");
    }
}
