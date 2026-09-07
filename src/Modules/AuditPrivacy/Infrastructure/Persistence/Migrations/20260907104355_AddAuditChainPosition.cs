using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditChainPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "chain_position",
                schema: "audit_privacy",
                table: "audit_logs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "hash_version",
                schema: "audit_privacy",
                table: "audit_logs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                WITH ordered AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY created_at_utc, id) AS position
                    FROM audit_privacy.audit_logs
                )
                UPDATE audit_privacy.audit_logs AS target
                SET chain_position = ordered.position
                FROM ordered
                WHERE target.id = ordered.id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_chain_position",
                schema: "audit_privacy",
                table: "audit_logs",
                column: "chain_position",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_chain_position",
                schema: "audit_privacy",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "chain_position",
                schema: "audit_privacy",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "hash_version",
                schema: "audit_privacy",
                table: "audit_logs");
        }
    }
}
