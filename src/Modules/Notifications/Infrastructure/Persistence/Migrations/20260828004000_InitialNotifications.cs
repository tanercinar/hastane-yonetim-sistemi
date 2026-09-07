using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Notifications.Infrastructure.Persistence.Migrations;

public partial class InitialNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "notifications");

        migrationBuilder.CreateTable(
            name: "in_app_notifications",
            schema: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                recipient_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                action_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                is_read = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_in_app_notifications", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "mock_deliveries",
            schema: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                body = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mock_deliveries", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "notification_outbox_events",
            schema: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                recipient_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipient_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                recipient_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_outbox_events", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_in_app_notifications_recipient_created",
            schema: "notifications",
            table: "in_app_notifications",
            columns: new[] { "recipient_person_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_in_app_notifications_idempotency_key",
            schema: "notifications",
            table: "in_app_notifications",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_mock_deliveries_recipient_sent",
            schema: "notifications",
            table: "mock_deliveries",
            columns: new[] { "recipient", "sent_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_mock_deliveries_idempotency_channel",
            schema: "notifications",
            table: "mock_deliveries",
            columns: new[] { "idempotency_key", "channel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_notification_outbox_status_created",
            schema: "notifications",
            table: "notification_outbox_events",
            columns: new[] { "status", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ux_notification_outbox_idempotency_key",
            schema: "notifications",
            table: "notification_outbox_events",
            column: "idempotency_key",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "in_app_notifications",
            schema: "notifications");

        migrationBuilder.DropTable(
            name: "mock_deliveries",
            schema: "notifications");

        migrationBuilder.DropTable(
            name: "notification_outbox_events",
            schema: "notifications");
    }
}
