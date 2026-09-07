using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNursingObservationsAndCarePlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "nursing_care_plans",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByNurseId = table.Column<Guid>(type: "uuid", nullable: false),
                NursingDiagnosis = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Goal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nursing_care_plans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "nursing_observations",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                RecordedByNurseId = table.Column<Guid>(type: "uuid", nullable: false),
                ObservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SystolicBp = table.Column<int>(type: "integer", nullable: true),
                DiastolicBp = table.Column<int>(type: "integer", nullable: true),
                HeartRate = table.Column<int>(type: "integer", nullable: true),
                RespiratoryRate = table.Column<int>(type: "integer", nullable: true),
                BodyTemperatureCelsius = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                OxygenSaturationPercent = table.Column<int>(type: "integer", nullable: true),
                PainScale = table.Column<int>(type: "integer", nullable: true),
                OralIntakeMl = table.Column<int>(type: "integer", nullable: true),
                IvIntakeMl = table.Column<int>(type: "integer", nullable: true),
                UrineOutputMl = table.Column<int>(type: "integer", nullable: true),
                DrainOutputMl = table.Column<int>(type: "integer", nullable: true),
                OtherOutputMl = table.Column<int>(type: "integer", nullable: true),
                Consciousness = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ClinicalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                IsCorrection = table.Column<bool>(type: "boolean", nullable: false),
                CorrectedObservationId = table.Column<Guid>(type: "uuid", nullable: true),
                CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nursing_observations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "nursing_care_tasks",
            schema: "inpatient",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CarePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Frequency = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DueTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CompletedByNurseId = table.Column<Guid>(type: "uuid", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CancelledByNurseId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nursing_care_tasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_nursing_care_tasks_nursing_care_plans_CarePlanId",
                    column: x => x.CarePlanId,
                    principalSchema: "inpatient",
                    principalTable: "nursing_care_plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_plans_AdmissionId",
            schema: "inpatient",
            table: "nursing_care_plans",
            column: "AdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_plans_PatientId",
            schema: "inpatient",
            table: "nursing_care_plans",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_plans_Status",
            schema: "inpatient",
            table: "nursing_care_plans",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_tasks_CarePlanId",
            schema: "inpatient",
            table: "nursing_care_tasks",
            column: "CarePlanId");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_tasks_DueTimeUtc",
            schema: "inpatient",
            table: "nursing_care_tasks",
            column: "DueTimeUtc");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_care_tasks_Status",
            schema: "inpatient",
            table: "nursing_care_tasks",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_observations_AdmissionId",
            schema: "inpatient",
            table: "nursing_observations",
            column: "AdmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_observations_ObservedAtUtc",
            schema: "inpatient",
            table: "nursing_observations",
            column: "ObservedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_nursing_observations_PatientId",
            schema: "inpatient",
            table: "nursing_observations",
            column: "PatientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "nursing_care_tasks",
            schema: "inpatient");

        migrationBuilder.DropTable(
            name: "nursing_observations",
            schema: "inpatient");

        migrationBuilder.DropTable(
            name: "nursing_care_plans",
            schema: "inpatient");
    }
}
