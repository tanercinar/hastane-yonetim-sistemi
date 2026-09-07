using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRadiologyStudyOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_radiology_studies_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "radiology_studies");

            migrationBuilder.CreateIndex(
                name: "IX_radiology_studies_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "radiology_studies",
                column: "diagnostic_order_item_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_radiology_studies_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "radiology_studies");

            migrationBuilder.CreateIndex(
                name: "IX_radiology_studies_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "radiology_studies",
                column: "diagnostic_order_item_id");
        }
    }
}
