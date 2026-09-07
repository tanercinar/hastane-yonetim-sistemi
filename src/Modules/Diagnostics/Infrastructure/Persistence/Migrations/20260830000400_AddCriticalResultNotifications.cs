using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCriticalResultNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "critical_result_notifications",
            schema: "diagnostics",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lab_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnostic_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                parameter_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                parameter_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                numeric_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                string_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                flag = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                escalation_level = table.Column<int>(type: "integer", nullable: false),
                responsible_doctor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                acknowledged_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                acknowledgment_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                escalated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                escalation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_critical_result_notifications", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_critical_result_notifications_diagnostic_order_id",
            schema: "diagnostics",
            table: "critical_result_notifications",
            column: "diagnostic_order_id");

        migrationBuilder.CreateIndex(
            name: "IX_critical_result_notifications_lab_result_id",
            schema: "diagnostics",
            table: "critical_result_notifications",
            column: "lab_result_id");

        migrationBuilder.CreateIndex(
            name: "IX_critical_result_notifications_patient_id",
            schema: "diagnostics",
            table: "critical_result_notifications",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "IX_critical_result_notifications_status",
            schema: "diagnostics",
            table: "critical_result_notifications",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "critical_result_notifications",
            schema: "diagnostics");
    }
}
