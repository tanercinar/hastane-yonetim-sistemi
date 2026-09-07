using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Migrations;

public partial class InitialInteroperabilitySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "interoperability");

        migrationBuilder.CreateTable(
            name: "mock_server_configs",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                FaultMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                LatencyMilliseconds = table.Column<int>(type: "integer", nullable: false),
                FailureRatePercentage = table.Column<int>(type: "integer", nullable: false),
                MaxRetryAttempts = table.Column<int>(type: "integer", nullable: false),
                TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mock_server_configs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "integration_message_logs",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ActionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PayloadSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                DurationMs = table.Column<int>(type: "integer", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_integration_message_logs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "integration_circuit_states",
            schema: "interoperability",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                State = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                FailureThreshold = table.Column<int>(type: "integer", nullable: false),
                RecoveryTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                LastFailureTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NextAttemptAllowedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_integration_circuit_states", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_mock_server_configs_SystemType",
            schema: "interoperability",
            table: "mock_server_configs",
            column: "SystemType",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_integration_message_logs_CorrelationId",
            schema: "interoperability",
            table: "integration_message_logs",
            column: "CorrelationId");

        migrationBuilder.CreateIndex(
            name: "IX_integration_message_logs_TimestampUtc",
            schema: "interoperability",
            table: "integration_message_logs",
            column: "TimestampUtc");

        migrationBuilder.CreateIndex(
            name: "IX_integration_circuit_states_SystemType",
            schema: "interoperability",
            table: "integration_circuit_states",
            column: "SystemType",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "mock_server_configs",
            schema: "interoperability");

        migrationBuilder.DropTable(
            name: "integration_message_logs",
            schema: "interoperability");

        migrationBuilder.DropTable(
            name: "integration_circuit_states",
            schema: "interoperability");
    }
}
