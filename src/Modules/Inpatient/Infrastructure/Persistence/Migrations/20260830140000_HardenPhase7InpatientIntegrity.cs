using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InpatientDbContext))]
[Migration("20260830140000_HardenPhase7InpatientIntegrity")]
public partial class HardenPhase7InpatientIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_admissions_PatientId",
            schema: "inpatient",
            table: "admissions");

        migrationBuilder.DropIndex(
            name: "IX_admissions_AssignedBedId",
            schema: "inpatient",
            table: "admissions");

        migrationBuilder.DropIndex(
            name: "IX_transfers_AdmissionId",
            schema: "inpatient",
            table: "transfers");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_patient_id_active",
            schema: "inpatient",
            table: "admissions",
            column: "PatientId",
            unique: true,
            filter: "\"Status\" IN ('Requested', 'Accepted', 'Admitted', 'Transferring')");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_assigned_bed_id_active",
            schema: "inpatient",
            table: "admissions",
            column: "AssignedBedId",
            unique: true,
            filter: "\"AssignedBedId\" IS NOT NULL AND \"Status\" IN ('Accepted', 'Admitted', 'Transferring')");

        migrationBuilder.CreateIndex(
            name: "IX_transfers_admission_id_active",
            schema: "inpatient",
            table: "transfers",
            column: "AdmissionId",
            unique: true,
            filter: "\"Status\" IN ('Requested', 'Accepted')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_admissions_patient_id_active",
            schema: "inpatient",
            table: "admissions");

        migrationBuilder.DropIndex(
            name: "IX_admissions_assigned_bed_id_active",
            schema: "inpatient",
            table: "admissions");

        migrationBuilder.DropIndex(
            name: "IX_transfers_admission_id_active",
            schema: "inpatient",
            table: "transfers");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_patient_id",
            schema: "inpatient",
            table: "admissions",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_admissions_assigned_bed_id",
            schema: "inpatient",
            table: "admissions",
            column: "AssignedBedId");

        migrationBuilder.CreateIndex(
            name: "IX_transfers_admission_id",
            schema: "inpatient",
            table: "transfers",
            column: "AdmissionId");
    }
}
