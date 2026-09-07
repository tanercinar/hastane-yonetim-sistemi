using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Migrations;

public partial class AddHl7DeadLetterQueue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "hl7_dead_letter_entries",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageControlId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MessageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                PayloadSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_hl7_dead_letter_entries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_hl7_dead_letter_entries_MessageControlId",
            schema: "interoperability",
            table: "hl7_dead_letter_entries",
            column: "MessageControlId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "hl7_dead_letter_entries",
            schema: "interoperability");
    }
}
