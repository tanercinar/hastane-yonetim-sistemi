using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddEncounterReopenSupport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "reopen_reason",
            schema: "clinical_records",
            table: "encounters",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "reopened_at_utc",
            schema: "clinical_records",
            table: "encounters",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "reopened_by_practitioner_id",
            schema: "clinical_records",
            table: "encounters",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "reopen_reason",
            schema: "clinical_records",
            table: "encounters");

        migrationBuilder.DropColumn(
            name: "reopened_at_utc",
            schema: "clinical_records",
            table: "encounters");

        migrationBuilder.DropColumn(
            name: "reopened_by_practitioner_id",
            schema: "clinical_records",
            table: "encounters");
    }
}
