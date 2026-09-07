using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Patients.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPatients : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "patients");

        migrationBuilder.CreateTable(
            name: "patient_records",
            schema: "patients",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                person_id = table.Column<Guid>(type: "uuid", nullable: false),
                medical_record_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                national_id_synthetic = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                address_city = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                address_district = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                address_line1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                address_postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                emergency_contact_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                emergency_contact_relationship = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                emergency_contact_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                pref_allow_sms = table.Column<bool>(type: "boolean", nullable: false),
                pref_allow_email = table.Column<bool>(type: "boolean", nullable: false),
                pref_language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_patient_records", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_patient_records_name_dob",
            schema: "patients",
            table: "patient_records",
            columns: new[] { "last_name", "first_name", "date_of_birth" });

        migrationBuilder.CreateIndex(
            name: "ix_patient_records_national_id",
            schema: "patients",
            table: "patient_records",
            column: "national_id_synthetic");

        migrationBuilder.CreateIndex(
            name: "ix_patient_records_person_id",
            schema: "patients",
            table: "patient_records",
            column: "person_id");

        migrationBuilder.CreateIndex(
            name: "ux_patient_records_mrn",
            schema: "patients",
            table: "patient_records",
            column: "medical_record_number",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "patient_records",
            schema: "patients");
    }
}
