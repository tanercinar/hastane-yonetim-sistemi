using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Reporting.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialReportingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "reporting");

        migrationBuilder.CreateTable(
            name: "bed_occupancy_metrics",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                WardType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TotalBeds = table.Column<int>(type: "integer", nullable: false),
                OccupiedBeds = table.Column<int>(type: "integer", nullable: false),
                AvailableBeds = table.Column<int>(type: "integer", nullable: false),
                PendingTransferCount = table.Column<int>(type: "integer", nullable: false),
                LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bed_occupancy_metrics", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "daily_outpatient_metrics",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                DoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                DoctorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                TotalAppointments = table.Column<int>(type: "integer", nullable: false),
                ScheduledCount = table.Column<int>(type: "integer", nullable: false),
                CheckedInCount = table.Column<int>(type: "integer", nullable: false),
                InProgressCount = table.Column<int>(type: "integer", nullable: false),
                CompletedCount = table.Column<int>(type: "integer", nullable: false),
                CancelledCount = table.Column<int>(type: "integer", nullable: false),
                NoShowCount = table.Column<int>(type: "integer", nullable: false),
                LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_daily_outpatient_metrics", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "diagnostic_workload_metrics",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                ModalityOrSection = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TotalOrders = table.Column<int>(type: "integer", nullable: false),
                PendingSpecimenCount = table.Column<int>(type: "integer", nullable: false),
                ProcessingCount = table.Column<int>(type: "integer", nullable: false),
                FinalizedCount = table.Column<int>(type: "integer", nullable: false),
                CriticalCount = table.Column<int>(type: "integer", nullable: false),
                AvgTurnaroundMinutes = table.Column<double>(type: "double precision", nullable: false),
                LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_diagnostic_workload_metrics", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "pharmacy_dispensing_metrics",
            schema: "reporting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                TotalPrescriptions = table.Column<int>(type: "integer", nullable: false),
                PendingDispenseCount = table.Column<int>(type: "integer", nullable: false),
                DispensedCount = table.Column<int>(type: "integer", nullable: false),
                LowStockItemCount = table.Column<int>(type: "integer", nullable: false),
                NearExpiryLotCount = table.Column<int>(type: "integer", nullable: false),
                LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_pharmacy_dispensing_metrics", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "projection_checkpoints",
            schema: "reporting",
            columns: table => new
            {
                ProjectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastProcessedPosition = table.Column<long>(type: "bigint", nullable: false),
                LastProcessedTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projection_checkpoints", x => x.ProjectionName);
            });

        migrationBuilder.CreateTable(
            name: "projection_processed_events",
            schema: "reporting",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projection_processed_events", x => x.EventId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bed_occupancy_metrics_Date",
            schema: "reporting",
            table: "bed_occupancy_metrics",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_bed_occupancy_metrics_Date_DepartmentId_WardType",
            schema: "reporting",
            table: "bed_occupancy_metrics",
            columns: new[] { "Date", "DepartmentId", "WardType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_bed_occupancy_metrics_DepartmentId",
            schema: "reporting",
            table: "bed_occupancy_metrics",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_daily_outpatient_metrics_Date",
            schema: "reporting",
            table: "daily_outpatient_metrics",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_daily_outpatient_metrics_Date_DepartmentId_DoctorId",
            schema: "reporting",
            table: "daily_outpatient_metrics",
            columns: new[] { "Date", "DepartmentId", "DoctorId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_daily_outpatient_metrics_DepartmentId",
            schema: "reporting",
            table: "daily_outpatient_metrics",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_workload_metrics_Date",
            schema: "reporting",
            table: "diagnostic_workload_metrics",
            column: "Date");

        migrationBuilder.CreateIndex(
            name: "IX_diagnostic_workload_metrics_Date_ModalityOrSection",
            schema: "reporting",
            table: "diagnostic_workload_metrics",
            columns: new[] { "Date", "ModalityOrSection" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_pharmacy_dispensing_metrics_Date",
            schema: "reporting",
            table: "pharmacy_dispensing_metrics",
            column: "Date",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_projection_processed_events_ProjectionName_ProcessedAtUtc",
            schema: "reporting",
            table: "projection_processed_events",
            columns: new[] { "ProjectionName", "ProcessedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bed_occupancy_metrics",
            schema: "reporting");

        migrationBuilder.DropTable(
            name: "daily_outpatient_metrics",
            schema: "reporting");

        migrationBuilder.DropTable(
            name: "diagnostic_workload_metrics",
            schema: "reporting");

        migrationBuilder.DropTable(
            name: "pharmacy_dispensing_metrics",
            schema: "reporting");

        migrationBuilder.DropTable(
            name: "projection_checkpoints",
            schema: "reporting");

        migrationBuilder.DropTable(
            name: "projection_processed_events",
            schema: "reporting");
    }
}
