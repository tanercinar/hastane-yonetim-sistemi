using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddLabCatalogAndParameters : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "lab_catalog_items",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                specimen_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                container_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_panel = table.Column<bool>(type: "boolean", nullable: false),
                turnaround_minutes = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                catalog_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lab_catalog_items", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "lab_catalog_parameters",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lab_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_range_low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                reference_range_high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                critical_low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                critical_high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                value_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lab_catalog_parameters", x => x.id);
                table.ForeignKey(
                    name: "FK_lab_catalog_parameters_lab_catalog_items_lab_catalog_item_id",
                    column: x => x.lab_catalog_item_id,
                    principalSchema: "diagnostics",
                    principalTable: "lab_catalog_items",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_lab_catalog_items_category",
            schema: "diagnostics",
            table: "lab_catalog_items",
            column: "category");

        migrationBuilder.CreateIndex(
            name: "IX_lab_catalog_items_code",
            schema: "diagnostics",
            table: "lab_catalog_items",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_lab_catalog_items_is_active",
            schema: "diagnostics",
            table: "lab_catalog_items",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_lab_catalog_parameters_code",
            schema: "diagnostics",
            table: "lab_catalog_parameters",
            column: "code");

        migrationBuilder.CreateIndex(
            name: "IX_lab_catalog_parameters_lab_catalog_item_id",
            schema: "diagnostics",
            table: "lab_catalog_parameters",
            column: "lab_catalog_item_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "lab_catalog_parameters",
            schema: "diagnostics");

        migrationBuilder.DropTable(
            name: "lab_catalog_items",
            schema: "diagnostics");
    }
}
