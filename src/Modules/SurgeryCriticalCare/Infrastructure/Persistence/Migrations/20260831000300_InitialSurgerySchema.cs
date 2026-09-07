using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Migrations;

public partial class InitialSurgerySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "surgery");

        migrationBuilder.CreateTable(
            name: "OperatingRooms",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RoomCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RoomName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Capacity = table.Column<int>(type: "integer", nullable: false),
                SpecialtyDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperatingRooms", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SurgeryBookings",
            schema: "surgery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ProcedureName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ProcedureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Urgency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                OperatingRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                LeadSurgeonDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                AnesthesiologistDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                OperatingNurseStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                ScheduledStartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ScheduledEndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ChecklistConsentSigned = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistAnesthesiaClearance = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistNpoConfirmed = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistBloodProductsReserved = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistSiteMarked = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistAllergyChecked = table.Column<bool>(type: "boolean", nullable: true),
                ChecklistCompletedByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                ChecklistCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ChecklistNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SurgeryBookings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OperatingRooms_RoomCode",
            schema: "surgery",
            table: "OperatingRooms",
            column: "RoomCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_AnesthesiologistDoctorId_ScheduledStartTime~",
            schema: "surgery",
            table: "SurgeryBookings",
            columns: new[] { "AnesthesiologistDoctorId", "ScheduledStartTimeUtc", "ScheduledEndTimeUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_BookingProtocolNumber",
            schema: "surgery",
            table: "SurgeryBookings",
            column: "BookingProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_LeadSurgeonDoctorId_ScheduledStartTimeUtc_S~",
            schema: "surgery",
            table: "SurgeryBookings",
            columns: new[] { "LeadSurgeonDoctorId", "ScheduledStartTimeUtc", "ScheduledEndTimeUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_OperatingRoomId_ScheduledStartTimeUtc_Sched~",
            schema: "surgery",
            table: "SurgeryBookings",
            columns: new[] { "OperatingRoomId", "ScheduledStartTimeUtc", "ScheduledEndTimeUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_PatientId",
            schema: "surgery",
            table: "SurgeryBookings",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_SurgeryBookings_Status",
            schema: "surgery",
            table: "SurgeryBookings",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OperatingRooms",
            schema: "surgery");

        migrationBuilder.DropTable(
            name: "SurgeryBookings",
            schema: "surgery");
    }
}
