using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkHomeHealthVisitsToEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_HomeHealthVisits_EncounterId",
                schema: "specialty",
                table: "HomeHealthVisits",
                column: "EncounterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_HomeHealthVisits_EncounterId",
                schema: "specialty",
                table: "HomeHealthVisits");
        }
    }
}
