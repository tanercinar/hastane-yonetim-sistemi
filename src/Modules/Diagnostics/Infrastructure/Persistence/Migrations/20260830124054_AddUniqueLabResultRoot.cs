using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueLabResultRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lab_results_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "lab_results");

            migrationBuilder.CreateIndex(
                name: "UX_lab_results_order_item_root",
                schema: "diagnostics",
                table: "lab_results",
                column: "diagnostic_order_item_id",
                unique: true,
                filter: "previous_result_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_lab_results_order_item_root",
                schema: "diagnostics",
                table: "lab_results");

            migrationBuilder.CreateIndex(
                name: "ix_lab_results_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "lab_results",
                column: "diagnostic_order_item_id");
        }
    }
}
