using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddConsultationRequests : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "consultation_requests",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                requesting_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                target_department_id = table.Column<Guid>(type: "uuid", nullable: false),
                target_practitioner_id = table.Column<Guid>(type: "uuid", nullable: true),
                assigned_practitioner_id = table.Column<Guid>(type: "uuid", nullable: true),
                urgency = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                reason_for_consultation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                clinical_question = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                consultation_report = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                decline_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_consultation_requests", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_consultation_requests_assigned_practitioner_id",
            schema: "clinical_records",
            table: "consultation_requests",
            column: "assigned_practitioner_id");

        migrationBuilder.CreateIndex(
            name: "ix_consultation_requests_encounter_id",
            schema: "clinical_records",
            table: "consultation_requests",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_consultation_requests_patient_id",
            schema: "clinical_records",
            table: "consultation_requests",
            column: "patient_id");

        migrationBuilder.CreateIndex(
            name: "ix_consultation_requests_target_department_id",
            schema: "clinical_records",
            table: "consultation_requests",
            column: "target_department_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "consultation_requests",
            schema: "clinical_records");
    }
}
