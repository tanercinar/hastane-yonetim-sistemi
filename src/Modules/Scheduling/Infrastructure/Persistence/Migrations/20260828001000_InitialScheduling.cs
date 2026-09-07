using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Migrations;

public partial class InitialScheduling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "scheduling");

        migrationBuilder.CreateTable(
            name: "doctor_leave_blocks",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_doctor_leave_blocks", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "doctor_schedules",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                day_of_week = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                slot_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_doctor_schedules", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "appointment_slots",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                hold_expiration_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                held_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_appointment_slots", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "schedule_breaks",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_schedule_breaks", x => x.id);
                table.ForeignKey(
                    name: "FK_schedule_breaks_doctor_schedules_schedule_id",
                    column: x => x.schedule_id,
                    principalSchema: "scheduling",
                    principalTable: "doctor_schedules",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_appointment_slots_dept_start_status",
            schema: "scheduling",
            table: "appointment_slots",
            columns: new[] { "department_id", "start_utc", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_appointment_slots_doctor_start_status",
            schema: "scheduling",
            table: "appointment_slots",
            columns: new[] { "doctor_id", "start_utc", "status" });

        migrationBuilder.CreateIndex(
            name: "ux_appointment_slots_doctor_start",
            schema: "scheduling",
            table: "appointment_slots",
            columns: new[] { "doctor_id", "start_utc" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_doctor_leave_blocks_doctor_dates",
            schema: "scheduling",
            table: "doctor_leave_blocks",
            columns: new[] { "doctor_id", "start_utc", "end_utc", "is_active" });

        migrationBuilder.CreateIndex(
            name: "ix_doctor_schedules_doctor_day",
            schema: "scheduling",
            table: "doctor_schedules",
            columns: new[] { "doctor_id", "day_of_week", "is_active" });

        migrationBuilder.CreateIndex(
            name: "ix_schedule_breaks_schedule_id",
            schema: "scheduling",
            table: "schedule_breaks",
            column: "schedule_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "appointment_slots",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "doctor_leave_blocks",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "schedule_breaks",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "doctor_schedules",
            schema: "scheduling");
    }
}
