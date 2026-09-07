using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceActiveScheduleUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_doctor_schedules_doctor_day",
                schema: "scheduling",
                table: "doctor_schedules");

            migrationBuilder.CreateIndex(
                name: "ux_doctor_schedules_active_doctor_day",
                schema: "scheduling",
                table: "doctor_schedules",
                columns: new[] { "doctor_id", "day_of_week", "is_active" },
                unique: true,
                filter: "\"is_active\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_doctor_schedules_active_doctor_day",
                schema: "scheduling",
                table: "doctor_schedules");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_doctor_day",
                schema: "scheduling",
                table: "doctor_schedules",
                columns: new[] { "doctor_id", "day_of_week", "is_active" });
        }
    }
}
