using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSpecimensAndCustodyChain : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "specimens",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                specimen_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                container_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                collection_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                collected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                collected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                received_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_specimens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "specimen_transition_events",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                specimen_id = table.Column<Guid>(type: "uuid", nullable: false),
                from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                transitioned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_specimen_transition_events", x => x.id);
                table.ForeignKey(
                    name: "FK_specimen_transition_events_specimens_specimen_id",
                    column: x => x.specimen_id,
                    principalSchema: "diagnostics",
                    principalTable: "specimens",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_specimen_transition_events_specimen_id",
            schema: "diagnostics",
            table: "specimen_transition_events",
            column: "specimen_id");

        migrationBuilder.CreateIndex(
            name: "IX_specimen_transition_events_transitioned_at_utc",
            schema: "diagnostics",
            table: "specimen_transition_events",
            column: "transitioned_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_specimens_barcode",
            schema: "diagnostics",
            table: "specimens",
            column: "barcode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_specimens_created_at_utc",
            schema: "diagnostics",
            table: "specimens",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_specimens_diagnostic_order_id",
            schema: "diagnostics",
            table: "specimens",
            column: "diagnostic_order_id");

        migrationBuilder.CreateIndex(
            name: "IX_specimens_patient_id",
            schema: "diagnostics",
            table: "specimens",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "IX_specimens_status",
            schema: "diagnostics",
            table: "specimens",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "specimen_transition_events",
            schema: "diagnostics");

        migrationBuilder.DropTable(
            name: "specimens",
            schema: "diagnostics");
    }
}
