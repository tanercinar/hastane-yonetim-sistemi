using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationProviderCaptureMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_key",
                schema: "notifications",
                table: "notification_outbox_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Notification.Generic");

            migrationBuilder.AddColumn<string>(
                name: "template_tokens_json",
                schema: "notifications",
                table: "notification_outbox_events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                schema: "notifications",
                table: "mock_deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                schema: "notifications",
                table: "mock_deliveries",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "tr-TR");

            migrationBuilder.AddColumn<string>(
                name: "provider",
                schema: "notifications",
                table: "mock_deliveries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "MOCK-LOCAL-CAPTURE");

            migrationBuilder.AddColumn<string>(
                name: "template_key",
                schema: "notifications",
                table: "mock_deliveries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Notification.Generic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "template_key",
                schema: "notifications",
                table: "notification_outbox_events");

            migrationBuilder.DropColumn(
                name: "template_tokens_json",
                schema: "notifications",
                table: "notification_outbox_events");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                schema: "notifications",
                table: "mock_deliveries");

            migrationBuilder.DropColumn(
                name: "locale",
                schema: "notifications",
                table: "mock_deliveries");

            migrationBuilder.DropColumn(
                name: "provider",
                schema: "notifications",
                table: "mock_deliveries");

            migrationBuilder.DropColumn(
                name: "template_key",
                schema: "notifications",
                table: "mock_deliveries");
        }
    }
}
