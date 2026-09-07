using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations;

public partial class AddHomeHealthVisits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HomeHealthVisits",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                ProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ServiceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ScheduledDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VisitStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VisitCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                City = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                District = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AddressDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ContactPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                VitalsSummaryNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HomeHealthVisits", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HomeHealthVisits_AssignedStaffId",
            schema: "specialty",
            table: "HomeHealthVisits",
            column: "AssignedStaffId");

        migrationBuilder.CreateIndex(
            name: "IX_HomeHealthVisits_PatientId",
            schema: "specialty",
            table: "HomeHealthVisits",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_HomeHealthVisits_ProtocolNumber",
            schema: "specialty",
            table: "HomeHealthVisits",
            column: "ProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HomeHealthVisits_ScheduledDateUtc",
            schema: "specialty",
            table: "HomeHealthVisits",
            column: "ScheduledDateUtc");

        migrationBuilder.CreateIndex(
            name: "IX_HomeHealthVisits_Status",
            schema: "specialty",
            table: "HomeHealthVisits",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HomeHealthVisits",
            schema: "specialty");
    }
}
