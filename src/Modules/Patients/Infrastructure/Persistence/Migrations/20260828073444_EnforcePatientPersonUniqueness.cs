using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Patients.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePatientPersonUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patient_records_person_id",
                schema: "patients",
                table: "patient_records");

            migrationBuilder.CreateIndex(
                name: "ux_patient_records_person_id",
                schema: "patients",
                table: "patient_records",
                column: "person_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_patient_records_person_id",
                schema: "patients",
                table: "patient_records");

            migrationBuilder.CreateIndex(
                name: "ix_patient_records_person_id",
                schema: "patients",
                table: "patient_records",
                column: "person_id");
        }
    }
}
