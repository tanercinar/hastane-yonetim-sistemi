using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddInpatientTransfers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "transfers",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceWardId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceBedId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetWardId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetBedId = table.Column<Guid>(type: "uuid", nullable: true),
                TransferReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ClinicalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_transfers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_transfers_AdmissionId",
            schema: "inpatient",
            table: "transfers",
            column: "AdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_transfers_PatientId",
            schema: "inpatient",
            table: "transfers",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_transfers_RequestedAtUtc",
            schema: "inpatient",
            table: "transfers",
            column: "RequestedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_transfers_Status",
            schema: "inpatient",
            table: "transfers",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "transfers",
            schema: "inpatient");
    }
}
