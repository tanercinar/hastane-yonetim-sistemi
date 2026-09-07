using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddInpatientDischarges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "discharges",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                DischargingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                DischargeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DischargeSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                FinalDiagnosisCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FinalDiagnosisDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DischargeRecommendations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                DischargePrescriptionSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                FollowUpAppointmentDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FollowUpDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                TransferFacilityName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                TransferReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                DischargedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_discharges", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_discharges_AdmissionId",
            schema: "inpatient",
            table: "discharges",
            column: "AdmissionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_discharges_DischargedAtUtc",
            schema: "inpatient",
            table: "discharges",
            column: "DischargedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_discharges_PatientId",
            schema: "inpatient",
            table: "discharges",
            column: "PatientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "discharges",
            schema: "inpatient");
    }
}
