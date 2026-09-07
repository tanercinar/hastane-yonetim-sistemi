using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Migrations;

public partial class AddAppointments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "appointments",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                department_id = table.Column<Guid>(type: "uuid", nullable: false),
                appointment_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                reason_for_visit = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                checked_in_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_appointments", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_dept_time_status",
            schema: "scheduling",
            table: "appointments",
            columns: new[] { "department_id", "appointment_time_utc", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_doctor_time_status",
            schema: "scheduling",
            table: "appointments",
            columns: new[] { "doctor_id", "appointment_time_utc", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_patient_time",
            schema: "scheduling",
            table: "appointments",
            columns: new[] { "patient_id", "appointment_time_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_appointments_slot_id",
            schema: "scheduling",
            table: "appointments",
            columns: new[] { "slot_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "appointments",
            schema: "scheduling");
    }
}
