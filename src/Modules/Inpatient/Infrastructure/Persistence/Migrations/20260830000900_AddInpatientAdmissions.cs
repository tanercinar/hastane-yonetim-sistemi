using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddInpatientAdmissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "admissions",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                OrderingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                AttendingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                AdmittingWardId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedBedId = table.Column<Guid>(type: "uuid", nullable: true),
                AdmissionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DiagnosisCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                DiagnosisDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                DietType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                FallRiskScore = table.Column<int>(type: "integer", nullable: false),
                IsolationRequired = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EstimatedStayDays = table.Column<int>(type: "integer", nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                AdmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DischargedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DischargeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_admissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_admissions_AdmissionNumber",
            schema: "inpatient",
            table: "admissions",
            column: "AdmissionNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_admissions_PatientId",
            schema: "inpatient",
            table: "admissions",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_Status",
            schema: "inpatient",
            table: "admissions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_AdmittingWardId",
            schema: "inpatient",
            table: "admissions",
            column: "AdmittingWardId");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_DepartmentId",
            schema: "inpatient",
            table: "admissions",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_AssignedBedId",
            schema: "inpatient",
            table: "admissions",
            column: "AssignedBedId");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_RequestedAtUtc",
            schema: "inpatient",
            table: "admissions",
            column: "RequestedAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "admissions",
            schema: "inpatient");
    }
}
