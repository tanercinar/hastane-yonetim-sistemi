using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkPregnancyToEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OpeningEncounterId",
                schema: "specialty",
                table: "PregnancyEpisodes",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EncounterId",
                schema: "specialty",
                table: "AntenatalVisits",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_PregnancyEpisodes_OpeningEncounterId",
                schema: "specialty",
                table: "PregnancyEpisodes",
                column: "OpeningEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AntenatalVisits_EncounterId",
                schema: "specialty",
                table: "AntenatalVisits",
                column: "EncounterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PregnancyEpisodes_OpeningEncounterId",
                schema: "specialty",
                table: "PregnancyEpisodes");

            migrationBuilder.DropIndex(
                name: "IX_AntenatalVisits_EncounterId",
                schema: "specialty",
                table: "AntenatalVisits");

            migrationBuilder.DropColumn(
                name: "OpeningEncounterId",
                schema: "specialty",
                table: "PregnancyEpisodes");

            migrationBuilder.DropColumn(
                name: "EncounterId",
                schema: "specialty",
                table: "AntenatalVisits");
        }
    }
}
