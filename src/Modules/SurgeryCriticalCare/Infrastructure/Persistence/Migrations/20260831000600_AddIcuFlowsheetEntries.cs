using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

public partial class AddIcuFlowsheetEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IcuFlowsheetEntries",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IcuAdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RecordedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                HeartRateBpm = table.Column<int>(type: "integer", nullable: true),
                SystolicBpMmHg = table.Column<int>(type: "integer", nullable: true),
                DiastolicBpMmHg = table.Column<int>(type: "integer", nullable: true),
                MeanArterialPressureMmHg = table.Column<int>(type: "integer", nullable: true),
                RespiratoryRateBpm = table.Column<int>(type: "integer", nullable: true),
                OxygenSaturationPct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                BodyTemperatureCelsius = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                GlasgowComaScale = table.Column<int>(type: "integer", nullable: true),
                RichmondAgitationSedationScale = table.Column<int>(type: "integer", nullable: true),
                VentilationMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FractionOfInspiredOxygenPct = table.Column<int>(type: "integer", nullable: true),
                PositiveEndExpiratoryPressure = table.Column<int>(type: "integer", nullable: true),
                TidalVolumeMl = table.Column<int>(type: "integer", nullable: true),
                PeakInspiratoryPressure = table.Column<int>(type: "integer", nullable: true),
                IvFluidIntakeMl = table.Column<int>(type: "integer", nullable: true),
                EnteralNutritionIntakeMl = table.Column<int>(type: "integer", nullable: true),
                UrineOutputMl = table.Column<int>(type: "integer", nullable: true),
                DrainOutputMl = table.Column<int>(type: "integer", nullable: true),
                ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IcuFlowsheetEntries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IcuFlowsheetEntries_IcuAdmissionId",
            schema: "surgery",
            table: "IcuFlowsheetEntries",
            column: "IcuAdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_IcuFlowsheetEntries_IcuAdmissionId_RecordedAtUtc",
            schema: "surgery",
            table: "IcuFlowsheetEntries",
            columns: new[] { "IcuAdmissionId", "RecordedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IcuFlowsheetEntries",
            schema: "surgery");
    }
}
