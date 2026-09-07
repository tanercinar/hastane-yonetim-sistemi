using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations;

public partial class AddDeliveryAndNewbornRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeliveryRecords",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PregnancyEpisodeId = table.Column<Guid>(type: "uuid", nullable: true),
                MotherPatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                DeliveryProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DeliveryMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DeliveryTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                GestationalAgeWeeks = table.Column<int>(type: "integer", nullable: false),
                GestationalAgeDays = table.Column<int>(type: "integer", nullable: false),
                PerinealTear = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EstimatedBloodLossMl = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                AttendingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                AssistingMidwifeId = table.Column<Guid>(type: "uuid", nullable: true),
                PediatricianDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                MaternalComplicationsNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                DeliverySummaryNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeliveryRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NewbornRecords",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DeliveryRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                NewbornPatientId = table.Column<Guid>(type: "uuid", nullable: true),
                BirthOrder = table.Column<int>(type: "integer", nullable: false),
                BirthTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                BirthWeightGrams = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                BirthLengthCm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                HeadCircumferenceCm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                ApgarScore1Min = table.Column<int>(type: "integer", nullable: false),
                ApgarScore5Min = table.Column<int>(type: "integer", nullable: false),
                ApgarScore10Min = table.Column<int>(type: "integer", nullable: true),
                ResuscitationGiven = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CordBloodPh = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                ComplicationsNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NewbornRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_NewbornRecords_DeliveryRecords_DeliveryRecordId",
                    column: x => x.DeliveryRecordId,
                    principalSchema: "specialty",
                    principalTable: "DeliveryRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryRecords_DeliveryProtocolNumber",
            schema: "specialty",
            table: "DeliveryRecords",
            column: "DeliveryProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryRecords_MotherPatientId",
            schema: "specialty",
            table: "DeliveryRecords",
            column: "MotherPatientId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryRecords_PregnancyEpisodeId",
            schema: "specialty",
            table: "DeliveryRecords",
            column: "PregnancyEpisodeId");

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryRecords_DeliveryTimeUtc",
            schema: "specialty",
            table: "DeliveryRecords",
            column: "DeliveryTimeUtc");

        migrationBuilder.CreateIndex(
            name: "IX_NewbornRecords_DeliveryRecordId",
            schema: "specialty",
            table: "NewbornRecords",
            column: "DeliveryRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_NewbornRecords_NewbornPatientId",
            schema: "specialty",
            table: "NewbornRecords",
            column: "NewbornPatientId");

        migrationBuilder.CreateIndex(
            name: "IX_NewbornRecords_BirthTimeUtc",
            schema: "specialty",
            table: "NewbornRecords",
            column: "BirthTimeUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NewbornRecords",
            schema: "specialty");

        migrationBuilder.DropTable(
            name: "DeliveryRecords",
            schema: "specialty");
    }
}
