using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

public partial class AddClinicalHandoffs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ClinicalHandoffs",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HandoffProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                InpatientStayId = table.Column<Guid>(type: "uuid", nullable: true),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceArea = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SourceLocationDetails = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DestinationArea = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DestinationLocationDetails = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                HandingOverStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                ReceivingStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                Situation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Background = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Assessment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CriticalAlerts = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                StatusReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                HandedOverAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClinicalHandoffs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ClinicalHandoffs_DestinationArea",
            schema: "surgery",
            table: "ClinicalHandoffs",
            column: "DestinationArea");

        migrationBuilder.CreateIndex(
            name: "IX_ClinicalHandoffs_HandedOverAtUtc",
            schema: "surgery",
            table: "ClinicalHandoffs",
            column: "HandedOverAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_ClinicalHandoffs_HandoffProtocolNumber",
            schema: "surgery",
            table: "ClinicalHandoffs",
            column: "HandoffProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClinicalHandoffs_PatientId",
            schema: "surgery",
            table: "ClinicalHandoffs",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_ClinicalHandoffs_Status",
            schema: "surgery",
            table: "ClinicalHandoffs",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ClinicalHandoffs",
            schema: "surgery");
    }
}
