using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPathologyCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pathology_cases",
                schema: "diagnostics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pathology_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    specimen_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    anatomic_site = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    clinical_history_and_diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fixative_used = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gross_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    gross_exam_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gross_exam_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    microscopic_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    microscopic_exam_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    microscopic_exam_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pathological_diagnosis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    report_drafted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report_finalized_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pathologist_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    previous_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pathology_cases", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pathology_cases_diagnostic_order_id",
                schema: "diagnostics",
                table: "pathology_cases",
                column: "diagnostic_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pathology_cases_diagnostic_order_item_id",
                schema: "diagnostics",
                table: "pathology_cases",
                column: "diagnostic_order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_pathology_cases_pathology_number",
                schema: "diagnostics",
                table: "pathology_cases",
                column: "pathology_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pathology_cases_patient_id",
                schema: "diagnostics",
                table: "pathology_cases",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_pathology_cases_status",
                schema: "diagnostics",
                table: "pathology_cases",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pathology_cases",
                schema: "diagnostics");
        }
    }
}
