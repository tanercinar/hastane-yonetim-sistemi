using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Migrations;

public partial class AddMhrsAppointments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "mhrs_appointments",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MhrsAppointmentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SlotId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PatientNationalId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                PatientFullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                DoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                ClinicName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                AppointmentDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mhrs_appointments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_mhrs_appointments_IdempotencyKey",
            schema: "interoperability",
            table: "mhrs_appointments",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_mhrs_appointments_MhrsAppointmentId",
            schema: "interoperability",
            table: "mhrs_appointments",
            column: "MhrsAppointmentId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_mhrs_appointments_PatientNationalId",
            schema: "interoperability",
            table: "mhrs_appointments",
            column: "PatientNationalId");

        migrationBuilder.CreateIndex(
            name: "IX_mhrs_appointments_SlotId",
            schema: "interoperability",
            table: "mhrs_appointments",
            column: "SlotId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "mhrs_appointments",
            schema: "interoperability");
    }
}
