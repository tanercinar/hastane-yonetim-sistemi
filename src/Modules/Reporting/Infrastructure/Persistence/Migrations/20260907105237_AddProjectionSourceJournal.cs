using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalManagement.Modules.Reporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectionSourceJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projection_source_events",
                schema: "reporting",
                columns: table => new
                {
                    Position = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projection_source_events", x => x.Position);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projection_source_events_EventId",
                schema: "reporting",
                table: "projection_source_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projection_source_events_ProjectionName_Position",
                schema: "reporting",
                table: "projection_source_events",
                columns: new[] { "ProjectionName", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projection_source_events",
                schema: "reporting");
        }
    }
}
