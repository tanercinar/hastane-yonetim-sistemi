using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireNewbornPatientIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewbornRecords_NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_NewbornRecords_NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords",
                column: "NewbornPatientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_NewbornRecords_NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_NewbornRecords_NewbornPatientId",
                schema: "specialty",
                table: "NewbornRecords",
                column: "NewbornPatientId");
        }
    }
}
