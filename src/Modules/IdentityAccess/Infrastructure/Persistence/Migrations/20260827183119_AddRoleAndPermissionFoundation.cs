using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndPermissionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity_access",
                table: "roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "40000000-0000-0000-0000-000000000001", "PAT", "PAT" },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "40000000-0000-0000-0000-000000000002", "DOC", "DOC" },
                    { new Guid("40000000-0000-0000-0000-000000000003"), "40000000-0000-0000-0000-000000000003", "NUR", "NUR" },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "40000000-0000-0000-0000-000000000004", "CHM", "CHM" },
                    { new Guid("40000000-0000-0000-0000-000000000005"), "40000000-0000-0000-0000-000000000005", "REG", "REG" },
                    { new Guid("40000000-0000-0000-0000-000000000006"), "40000000-0000-0000-0000-000000000006", "LAB", "LAB" },
                    { new Guid("40000000-0000-0000-0000-000000000007"), "40000000-0000-0000-0000-000000000007", "RAD", "RAD" },
                    { new Guid("40000000-0000-0000-0000-000000000008"), "40000000-0000-0000-0000-000000000008", "PHA", "PHA" },
                    { new Guid("40000000-0000-0000-0000-000000000009"), "40000000-0000-0000-0000-000000000009", "ADM", "ADM" },
                    { new Guid("40000000-0000-0000-0000-00000000000a"), "40000000-0000-0000-0000-00000000000a", "MGR", "MGR" },
                    { new Guid("40000000-0000-0000-0000-00000000000b"), "40000000-0000-0000-0000-00000000000b", "FIN", "FIN" },
                    { new Guid("40000000-0000-0000-0000-00000000000c"), "40000000-0000-0000-0000-00000000000c", "HR", "HR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-00000000000b"));

            migrationBuilder.DeleteData(
                schema: "identity_access",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-00000000000c"));
        }
    }
}
