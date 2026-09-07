using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations;

public partial class AddOdontologyAndDentalRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DentalExaminations",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                ExaminationProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DentistId = table.Column<Guid>(type: "uuid", nullable: false),
                ExaminationDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ChiefComplaint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                DiagnosisNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                TreatmentPlanSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DentalExaminations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DentalProcedures",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                ProcedureProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ToothNumber = table.Column<int>(type: "integer", nullable: true),
                Surfaces = table.Column<int>(type: "integer", nullable: false),
                ProcedureCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ProcedureName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EstimatedCost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                PerformedByDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                ScheduledDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DentalProcedures", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DentalToothConditions",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                ToothNumber = table.Column<int>(type: "integer", nullable: false),
                Condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AffectedSurfaces = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RecordedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DentalToothConditions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DentalExaminations_ExaminationProtocolNumber",
            schema: "specialty",
            table: "DentalExaminations",
            column: "ExaminationProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DentalExaminations_PatientId",
            schema: "specialty",
            table: "DentalExaminations",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_DentalExaminations_ExaminationDateUtc",
            schema: "specialty",
            table: "DentalExaminations",
            column: "ExaminationDateUtc");

        migrationBuilder.CreateIndex(
            name: "IX_DentalProcedures_ProcedureProtocolNumber",
            schema: "specialty",
            table: "DentalProcedures",
            column: "ProcedureProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DentalProcedures_PatientId",
            schema: "specialty",
            table: "DentalProcedures",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_DentalProcedures_ToothNumber",
            schema: "specialty",
            table: "DentalProcedures",
            column: "ToothNumber");

        migrationBuilder.CreateIndex(
            name: "IX_DentalProcedures_Status",
            schema: "specialty",
            table: "DentalProcedures",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_DentalToothConditions_PatientId_ToothNumber",
            schema: "specialty",
            table: "DentalToothConditions",
            columns: new[] { "PatientId", "ToothNumber" });

        migrationBuilder.CreateIndex(
            name: "IX_DentalToothConditions_RecordedAtUtc",
            schema: "specialty",
            table: "DentalToothConditions",
            column: "RecordedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DentalExaminations",
            schema: "specialty");

        migrationBuilder.DropTable(
            name: "DentalProcedures",
            schema: "specialty");

        migrationBuilder.DropTable(
            name: "DentalToothConditions",
            schema: "specialty");
    }
}
