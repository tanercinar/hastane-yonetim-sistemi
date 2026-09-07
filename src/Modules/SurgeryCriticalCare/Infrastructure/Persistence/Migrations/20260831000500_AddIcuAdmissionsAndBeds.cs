using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

public partial class AddIcuAdmissionsAndBeds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IcuBeds",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                BedName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UnitName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IcuBeds", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "IcuAdmissions",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                InpatientStayId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                IcuBedId = table.Column<Guid>(type: "uuid", nullable: false),
                IcuBedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AttendingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryNurseId = table.Column<Guid>(type: "uuid", nullable: true),
                AdmissionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AcuityLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MonitoringFrequencyMinutes = table.Column<int>(type: "integer", nullable: false),
                VentilationMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CarePlanNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                AdmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DischargedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DischargeNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IcuAdmissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IcuAdmissions_AdmissionProtocolNumber",
            schema: "surgery",
            table: "IcuAdmissions",
            column: "AdmissionProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IcuAdmissions_IcuBedId",
            schema: "surgery",
            table: "IcuAdmissions",
            column: "IcuBedId");

        migrationBuilder.CreateIndex(
            name: "IX_IcuAdmissions_InpatientStayId",
            schema: "surgery",
            table: "IcuAdmissions",
            column: "InpatientStayId");

        migrationBuilder.CreateIndex(
            name: "IX_IcuAdmissions_PatientId",
            schema: "surgery",
            table: "IcuAdmissions",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_IcuAdmissions_Status",
            schema: "surgery",
            table: "IcuAdmissions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_IcuBeds_BedCode",
            schema: "surgery",
            table: "IcuBeds",
            column: "BedCode",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IcuAdmissions",
            schema: "surgery");

        migrationBuilder.DropTable(
            name: "IcuBeds",
            schema: "surgery");
    }
}
