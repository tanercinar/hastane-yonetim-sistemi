using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Migrations;

public partial class AddENabizTransmissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "enabiz_transmissions",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SysTakipNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PackageType = table.Column<int>(type: "integer", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientNationalId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                HasPatientConsent = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                PayloadSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ResponseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_enabiz_transmissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_enabiz_transmissions_PatientId",
            schema: "interoperability",
            table: "enabiz_transmissions",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_enabiz_transmissions_Status",
            schema: "interoperability",
            table: "enabiz_transmissions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_enabiz_transmissions_SysTakipNo",
            schema: "interoperability",
            table: "enabiz_transmissions",
            column: "SysTakipNo",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "enabiz_transmissions",
            schema: "interoperability");
    }
}
