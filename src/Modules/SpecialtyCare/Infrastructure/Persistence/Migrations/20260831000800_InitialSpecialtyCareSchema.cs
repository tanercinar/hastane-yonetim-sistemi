using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations;

public partial class InitialSpecialtyCareSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "specialty");

        migrationBuilder.CreateTable(
            name: "PregnancyEpisodes",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                EpisodeProtocolNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Gravida = table.Column<int>(type: "integer", nullable: false),
                Para = table.Column<int>(type: "integer", nullable: false),
                Abortus = table.Column<int>(type: "integer", nullable: false),
                LivingChildren = table.Column<int>(type: "integer", nullable: false),
                LastMenstrualPeriodUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EstimatedDeliveryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BloodGroupAndRh = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                RiskCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RiskFactorsNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AssignedDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                AssignedMidwifeId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PregnancyEpisodes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AntenatalVisits",
            schema: "specialty",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PregnancyEpisodeId = table.Column<Guid>(type: "uuid", nullable: false),
                VisitDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                GestationalAgeWeeks = table.Column<int>(type: "integer", nullable: false),
                GestationalAgeDays = table.Column<int>(type: "integer", nullable: false),
                MaternalWeightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                SystolicBpMmHg = table.Column<int>(type: "integer", nullable: true),
                DiastolicBpMmHg = table.Column<int>(type: "integer", nullable: true),
                FundalHeightCm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                FetalHeartRateBpm = table.Column<int>(type: "integer", nullable: true),
                FetalPresentation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EdemaLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                UrineProteinPresent = table.Column<bool>(type: "boolean", nullable: false),
                UrineGlucosePresent = table.Column<bool>(type: "boolean", nullable: false),
                StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                NextVisitRecommendedDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AntenatalVisits", x => x.Id);
                table.ForeignKey(
                    name: "FK_AntenatalVisits_PregnancyEpisodes_PregnancyEpisodeId",
                    column: x => x.PregnancyEpisodeId,
                    principalSchema: "specialty",
                    principalTable: "PregnancyEpisodes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AntenatalVisits_PregnancyEpisodeId",
            schema: "specialty",
            table: "AntenatalVisits",
            column: "PregnancyEpisodeId");

        migrationBuilder.CreateIndex(
            name: "IX_AntenatalVisits_VisitDateUtc",
            schema: "specialty",
            table: "AntenatalVisits",
            column: "VisitDateUtc");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_EpisodeProtocolNumber",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "EpisodeProtocolNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_EstimatedDeliveryDateUtc",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "EstimatedDeliveryDateUtc");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_PatientId",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_RiskCategory",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "RiskCategory");

        migrationBuilder.CreateIndex(
            name: "IX_PregnancyEpisodes_Status",
            schema: "specialty",
            table: "PregnancyEpisodes",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AntenatalVisits",
            schema: "specialty");

        migrationBuilder.DropTable(
            name: "PregnancyEpisodes",
            schema: "specialty");
    }
}
