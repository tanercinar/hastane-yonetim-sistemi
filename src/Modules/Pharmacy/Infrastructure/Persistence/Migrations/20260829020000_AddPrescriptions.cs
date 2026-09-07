using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPrescriptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "prescriptions",
            schema: "pharmacy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                prescription_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                prescribing_doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                signed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                signed_by_doctor_id = table.Column<Guid>(type: "uuid", nullable: true),
                cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                cancelled_by_doctor_id = table.Column<Guid>(type: "uuid", nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                entered_in_error_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                entered_in_error_by_doctor_id = table.Column<Guid>(type: "uuid", nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                diagnosis_summary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                general_instructions = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_prescriptions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "prescription_items",
            schema: "pharmacy",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                medication_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                medication_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                brand_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                generic_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                form = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                route = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                dose = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                dose_unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                frequency = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                duration_days = table.Column<int>(type: "integer", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                quantity_unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                dispensed_quantity = table.Column<int>(type: "integer", nullable: false),
                instructions = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_prescription_items", x => x.id);
                table.ForeignKey(
                    name: "FK_prescription_items_prescriptions_prescription_id",
                    column: x => x.prescription_id,
                    principalSchema: "pharmacy",
                    principalTable: "prescriptions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_prescription_items_medication_catalog_item_id",
            schema: "pharmacy",
            table: "prescription_items",
            column: "medication_catalog_item_id");

        migrationBuilder.CreateIndex(
            name: "ix_prescription_items_prescription_id",
            schema: "pharmacy",
            table: "prescription_items",
            column: "prescription_id");

        migrationBuilder.CreateIndex(
            name: "ix_prescriptions_encounter_id",
            schema: "pharmacy",
            table: "prescriptions",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_prescriptions_patient_id",
            schema: "pharmacy",
            table: "prescriptions",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_prescriptions_prescribing_doctor_id",
            schema: "pharmacy",
            table: "prescriptions",
            column: "prescribing_doctor_id");

        migrationBuilder.CreateIndex(
            name: "ix_prescriptions_status",
            schema: "pharmacy",
            table: "prescriptions",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ux_prescriptions_prescription_number",
            schema: "pharmacy",
            table: "prescriptions",
            column: "prescription_number",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "prescription_items",
            schema: "pharmacy");

        migrationBuilder.DropTable(
            name: "prescriptions",
            schema: "pharmacy");
    }
}
