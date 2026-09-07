using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialInpatientWardsRoomsBeds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "inpatient");

        migrationBuilder.CreateTable(
            name: "wards",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                Building = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Floor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                WardType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wards", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "rooms",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WardId = table.Column<Guid>(type: "uuid", nullable: false),
                RoomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                GenderConstraint = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsolationType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsNegativePressure = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rooms", x => x.Id);
                table.ForeignKey(
                    name: "FK_rooms_wards_WardId",
                    column: x => x.WardId,
                    principalSchema: "inpatient",
                    principalTable: "wards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "beds",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                WardId = table.Column<Guid>(type: "uuid", nullable: false),
                BedNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CurrentAdmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                CurrentPatientId = table.Column<Guid>(type: "uuid", nullable: true),
                GenderConstraint = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsolationType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                HasTelemetry = table.Column<bool>(type: "boolean", nullable: false),
                HasOxygen = table.Column<bool>(type: "boolean", nullable: false),
                HasVentilator = table.Column<bool>(type: "boolean", nullable: false),
                MaintenanceReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_beds", x => x.Id);
                table.ForeignKey(
                    name: "FK_beds_rooms_RoomId",
                    column: x => x.RoomId,
                    principalSchema: "inpatient",
                    principalTable: "rooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_beds_CurrentAdmissionId",
            schema: "inpatient",
            table: "beds",
            column: "CurrentAdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_beds_CurrentPatientId",
            schema: "inpatient",
            table: "beds",
            column: "CurrentPatientId");

        migrationBuilder.CreateIndex(
            name: "IX_beds_RoomId_BedNumber",
            schema: "inpatient",
            table: "beds",
            columns: new[] { "RoomId", "BedNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_beds_Status",
            schema: "inpatient",
            table: "beds",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_beds_WardId",
            schema: "inpatient",
            table: "beds",
            column: "WardId");

        migrationBuilder.CreateIndex(
            name: "IX_rooms_WardId_RoomNumber",
            schema: "inpatient",
            table: "rooms",
            columns: new[] { "WardId", "RoomNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_wards_Code",
            schema: "inpatient",
            table: "wards",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_wards_DepartmentId",
            schema: "inpatient",
            table: "wards",
            column: "DepartmentId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "beds",
            schema: "inpatient");

        migrationBuilder.DropTable(
            name: "rooms",
            schema: "inpatient");

        migrationBuilder.DropTable(
            name: "wards",
            schema: "inpatient");
    }
}
