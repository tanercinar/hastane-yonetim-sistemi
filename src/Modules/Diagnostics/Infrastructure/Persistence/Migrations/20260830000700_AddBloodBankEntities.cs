using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodBankEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_units",
                schema: "diagnostics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    blood_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    volume_ml = table.Column<int>(type: "integer", nullable: false),
                    donation_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    storage_location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reserved_for_patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reserved_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crossmatch_requests",
                schema: "diagnostics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_blood_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_product_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    units_requested = table.Column<int>(type: "integer", nullable: false),
                    required_by_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    compatibility_result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    technician_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allocated_blood_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crossmatch_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_blood_group",
                schema: "diagnostics",
                table: "blood_units",
                column: "blood_group");

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_product_type",
                schema: "diagnostics",
                table: "blood_units",
                column: "product_type");

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_reserved_for_patient_id",
                schema: "diagnostics",
                table: "blood_units",
                column: "reserved_for_patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_status",
                schema: "diagnostics",
                table: "blood_units",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_blood_units_unit_number",
                schema: "diagnostics",
                table: "blood_units",
                column: "unit_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crossmatch_requests_compatibility_result",
                schema: "diagnostics",
                table: "crossmatch_requests",
                column: "compatibility_result");

            migrationBuilder.CreateIndex(
                name: "IX_crossmatch_requests_diagnostic_order_id",
                schema: "diagnostics",
                table: "crossmatch_requests",
                column: "diagnostic_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_crossmatch_requests_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "crossmatch_requests",
                column: "diagnostic_order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_crossmatch_requests_patient_id",
                schema: "diagnostics",
                table: "crossmatch_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_crossmatch_requests_status",
                schema: "diagnostics",
                table: "crossmatch_requests",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_units",
                schema: "diagnostics");

            migrationBuilder.DropTable(
                name: "crossmatch_requests",
                schema: "diagnostics");
        }
    }
}
