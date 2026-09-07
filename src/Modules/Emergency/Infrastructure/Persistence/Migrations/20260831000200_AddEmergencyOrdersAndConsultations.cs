using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Migrations;

public partial class AddEmergencyOrdersAndConsultations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DispositionType",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DispositionDecidedByDoctorId",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DispositionDecidedAtUtc",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DispositionTargetWardOrIcuId",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DispositionTargetDepartmentName",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DispositionSummaryNotes",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DispositionFollowUpInstructions",
            schema: "emergency",
            table: "EmergencyAdmissions",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "EmergencyCareOrders",
            schema: "emergency",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                OrderCatalogCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                OrderCatalogName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                OrderedByDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ClinicalInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ResultSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmergencyCareOrders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmergencyConsultations",
            schema: "emergency",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RequestedByDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Urgency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ClinicalReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ConsultantDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                ConsultationResponseNotes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmergencyConsultations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyCareOrders_AdmissionId_Status",
            schema: "emergency",
            table: "EmergencyCareOrders",
            columns: new[] { "AdmissionId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyCareOrders_OrderedAtUtc",
            schema: "emergency",
            table: "EmergencyCareOrders",
            column: "OrderedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyConsultations_AdmissionId_Status",
            schema: "emergency",
            table: "EmergencyConsultations",
            columns: new[] { "AdmissionId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyConsultations_DepartmentId_Status",
            schema: "emergency",
            table: "EmergencyConsultations",
            columns: new[] { "DepartmentId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_EmergencyConsultations_RequestedAtUtc",
            schema: "emergency",
            table: "EmergencyConsultations",
            column: "RequestedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmergencyCareOrders",
            schema: "emergency");

        migrationBuilder.DropTable(
            name: "EmergencyConsultations",
            schema: "emergency");

        migrationBuilder.DropColumn(
            name: "DispositionType",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionDecidedByDoctorId",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionDecidedAtUtc",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionTargetWardOrIcuId",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionTargetDepartmentName",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionSummaryNotes",
            schema: "emergency",
            table: "EmergencyAdmissions");

        migrationBuilder.DropColumn(
            name: "DispositionFollowUpInstructions",
            schema: "emergency",
            table: "EmergencyAdmissions");
    }
}
