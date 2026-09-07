using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Migrations;

public partial class InitialEmergencySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "emergency");

        migrationBuilder.CreateTable(
            name: "EmergencyAdmissions",
            schema: "emergency",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EmergencyProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                ArrivalType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ChiefComplaint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AdmissionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AdmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AdmittingStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                TriageLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                TriageCategoryReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                TriagedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TriageNurseId = table.Column<Guid>(type: "uuid", nullable: true),
                EducationalClassificationAssisted = table.Column<bool>(type: "boolean", nullable: true),
                SystolicBp = table.Column<int>(type: "integer", nullable: true),
                DiastolicBp = table.Column<int>(type: "integer", nullable: true),
                HeartRate = table.Column<int>(type: "integer", nullable: true),
                BodyTemperatureCelsius = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                RespiratoryRate = table.Column<int>(type: "integer", nullable: true),
                OxygenSaturationPercent = table.Column<int>(type: "integer", nullable: true),
                PainScale = table.Column<int>(type: "integer", nullable: true),
                Consciousness = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                TriageClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                AssignedDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                AssignedBedOrZone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DischargeOrDispositionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmergencyAdmissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyAdmissions_EmergencyProtocolNumber",
            schema: "emergency",
            table: "EmergencyAdmissions",
            column: "EmergencyProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyAdmissions_PatientId_Status",
            schema: "emergency",
            table: "EmergencyAdmissions",
            columns: new[] { "PatientId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyAdmissions_Status",
            schema: "emergency",
            table: "EmergencyAdmissions",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmergencyAdmissions",
            schema: "emergency");
    }
}
