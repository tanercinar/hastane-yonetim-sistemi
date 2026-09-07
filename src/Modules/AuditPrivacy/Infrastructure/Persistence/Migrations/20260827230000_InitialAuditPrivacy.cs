using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit_privacy");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit_privacy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    actor_ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    actor_user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_resource_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_resource_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    details_json = table.Column<string>(type: "text", nullable: true),
                    record_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    previous_record_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_person_id",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "actor_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at_utc",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_target_resource_type_target_resource_id",
                schema: "audit_privacy",
                table: "audit_logs",
                columns: new[] { "target_resource_type", "target_resource_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit_privacy");
        }
    }
}
