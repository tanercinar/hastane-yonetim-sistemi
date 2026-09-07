using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyDepartmentScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "organization",
                table: "departments",
                columns: new[] { "id", "category", "code", "created_at_utc", "display_name", "facility_id", "hospital_id", "is_active", "parent_department_id", "version" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000007"), "Operational", "DEMO-PHARMACY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Eczane Bölümü", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, null, 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "organization",
                table: "departments",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000007"));
        }
    }
}
