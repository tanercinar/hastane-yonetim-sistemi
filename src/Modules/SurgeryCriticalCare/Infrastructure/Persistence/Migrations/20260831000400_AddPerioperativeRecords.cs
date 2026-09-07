using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

public partial class AddPerioperativeRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PerioperativeRecords",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SurgeryBookingId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                OperatingRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                RoomEntryTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AnesthesiaStartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IncisionTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClosureTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AnesthesiaEndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RoomExitTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AnesthesiaType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AnesthesiaNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IntraoperativeFindings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                IntraoperativeComplications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                EstimatedBloodLossMl = table.Column<int>(type: "integer", nullable: true),
                SpecimensCollected = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CountsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                PostOpDisposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                PostOpInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                SignedByDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                SignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PerioperativeRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PerioperativeCorrections",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PerioperativeRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                CorrectedByDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                CorrectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReasonForCorrection = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CorrectionNote = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PerioperativeCorrections", x => x.Id);
                table.ForeignKey(
                    name: "FK_PerioperativeCorrections_PerioperativeRecords_Perioperativ~",
                    column: x => x.PerioperativeRecordId,
                    principalSchema: "surgery",
                    principalTable: "PerioperativeRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PerioperativeCorrections_PerioperativeRecordId",
            schema: "surgery",
            table: "PerioperativeCorrections",
            column: "PerioperativeRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_PerioperativeRecords_SurgeryBookingId",
            schema: "surgery",
            table: "PerioperativeRecords",
            column: "SurgeryBookingId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PerioperativeCorrections",
            schema: "surgery");

        migrationBuilder.DropTable(
            name: "PerioperativeRecords",
            schema: "surgery");
    }
}
