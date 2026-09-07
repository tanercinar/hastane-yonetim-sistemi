using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SpecialtyCareDbContext))]
[Migration("20260901000100_AddSpecialtyConcurrencyConstraints")]
public sealed class AddSpecialtyConcurrencyConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PregnancyEpisodes_PatientId",
            schema: "specialty",
            table: "PregnancyEpisodes");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_PatientId",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "PatientId",
            unique: true,
            filter: "\"Status\" = 'Active'");

        migrationBuilder.CreateIndex(
            name: "IX_DentalToothConditions_PatientId_ToothNumber_Version",
            schema: "specialty",
            table: "DentalToothConditions",
            columns: new[] { "PatientId", "ToothNumber", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DentalToothConditions_PatientId_ToothNumber_Version",
            schema: "specialty",
            table: "DentalToothConditions");

        migrationBuilder.DropIndex(
            name: "IX_PregnancyEpisodes_PatientId",
            schema: "specialty",
            table: "PregnancyEpisodes");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_PatientId",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "PatientId");
    }
}
