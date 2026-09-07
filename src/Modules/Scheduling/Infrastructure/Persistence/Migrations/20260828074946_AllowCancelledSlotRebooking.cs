using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowCancelledSlotRebooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appointments_slot_id",
                schema: "scheduling",
                table: "appointments");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_slot_id",
                schema: "scheduling",
                table: "appointments",
                column: "slot_id",
                unique: true,
                filter: "\"status\" != 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appointments_slot_id",
                schema: "scheduling",
                table: "appointments");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_slot_id",
                schema: "scheduling",
                table: "appointments",
                column: "slot_id",
                unique: true);
        }
    }
}
