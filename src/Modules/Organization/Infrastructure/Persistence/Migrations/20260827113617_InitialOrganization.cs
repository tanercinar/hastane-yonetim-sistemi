using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "hospitals",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hospitals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "facilities",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facilities", x => x.id);
                    table.UniqueConstraint("ak_facilities_hospital_id_id", x => new { x.hospital_id, x.id });
                    table.ForeignKey(
                        name: "fk_facilities_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalSchema: "organization",
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_specialties", x => x.id);
                    table.UniqueConstraint("ak_specialties_hospital_id_id", x => new { x.hospital_id, x.id });
                    table.ForeignKey(
                        name: "fk_specialties_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalSchema: "organization",
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.UniqueConstraint("ak_departments_hospital_id_facility_id_id", x => new { x.hospital_id, x.facility_id, x.id });
                    table.UniqueConstraint("ak_departments_hospital_id_id", x => new { x.hospital_id, x.id });
                    table.ForeignKey(
                        name: "fk_departments_facilities_hospital_id_facility_id",
                        columns: x => new { x.hospital_id, x.facility_id },
                        principalSchema: "organization",
                        principalTable: "facilities",
                        principalColumns: new[] { "hospital_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_departments_parent_department",
                        columns: x => new { x.hospital_id, x.facility_id, x.parent_department_id },
                        principalSchema: "organization",
                        principalTable: "departments",
                        principalColumns: new[] { "hospital_id", "facility_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_profiles",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    profession = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    primary_specialty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_profiles", x => x.id);
                    table.UniqueConstraint("ak_staff_profiles_hospital_id_id", x => new { x.hospital_id, x.id });
                    table.ForeignKey(
                        name: "fk_staff_profiles_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalSchema: "organization",
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_profiles_specialties_primary_specialty",
                        columns: x => new { x.hospital_id, x.primary_specialty_id },
                        principalSchema: "organization",
                        principalTable: "specialties",
                        principalColumns: new[] { "hospital_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_department_assignments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_department_assignments", x => x.id);
                    table.CheckConstraint("ck_staff_department_assignments_valid_interval", "ends_at_utc IS NULL OR ends_at_utc > starts_at_utc");
                    table.ForeignKey(
                        name: "fk_staff_assignments_departments_hospital_scope",
                        columns: x => new { x.hospital_id, x.department_id },
                        principalSchema: "organization",
                        principalTable: "departments",
                        principalColumns: new[] { "hospital_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_assignments_staff_profiles_hospital_scope",
                        columns: x => new { x.hospital_id, x.staff_profile_id },
                        principalSchema: "organization",
                        principalTable: "staff_profiles",
                        principalColumns: new[] { "hospital_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "hospitals",
                columns: new[] { "id", "code", "created_at_utc", "display_name", "is_active", "version" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), "DEMO-HOSPITAL", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Eğitim Hastanesi", true, 1L });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "facilities",
                columns: new[] { "id", "code", "created_at_utc", "display_name", "hospital_id", "is_active", "version" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), "DEMO-CENTRAL", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Merkez Şube", new Guid("10000000-0000-0000-0000-000000000001"), true, 1L });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "specialties",
                columns: new[] { "id", "code", "created_at_utc", "display_name", "hospital_id", "is_active", "version" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "DEMO-INTERNAL-MEDICINE", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO İç Hastalıkları", new Guid("10000000-0000-0000-0000-000000000001"), true, 1L },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "DEMO-CARDIOLOGY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Kardiyoloji", new Guid("10000000-0000-0000-0000-000000000001"), true, 1L },
                    { new Guid("40000000-0000-0000-0000-000000000003"), "DEMO-MEDICAL-BIOCHEMISTRY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Tıbbi Biyokimya", new Guid("10000000-0000-0000-0000-000000000001"), true, 1L },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "DEMO-RADIOLOGY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Radyoloji", new Guid("10000000-0000-0000-0000-000000000001"), true, 1L }
                });

            migrationBuilder.InsertData(
                schema: "organization",
                table: "departments",
                columns: new[] { "id", "category", "code", "created_at_utc", "display_name", "facility_id", "hospital_id", "is_active", "parent_department_id", "version" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), "Clinical", "DEMO-CLINICAL-SERVICES", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Klinik Hizmetler", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, null, 1L },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "Diagnostic", "DEMO-DIAGNOSTIC-SERVICES", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Tanı Hizmetleri", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, null, 1L },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "Clinical", "DEMO-INTERNAL-MEDICINE", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO İç Hastalıkları Bölümü", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, new Guid("30000000-0000-0000-0000-000000000001"), 1L },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "Clinical", "DEMO-CARDIOLOGY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Kardiyoloji Bölümü", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, new Guid("30000000-0000-0000-0000-000000000001"), 1L },
                    { new Guid("30000000-0000-0000-0000-000000000005"), "Diagnostic", "DEMO-LABORATORY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Laboratuvar Bölümü", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, new Guid("30000000-0000-0000-0000-000000000004"), 1L },
                    { new Guid("30000000-0000-0000-0000-000000000006"), "Diagnostic", "DEMO-RADIOLOGY", new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Utc), "DEMO Radyoloji Bölümü", new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), true, new Guid("30000000-0000-0000-0000-000000000004"), 1L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_hospital_id_facility_id_parent_department_id",
                schema: "organization",
                table: "departments",
                columns: new[] { "hospital_id", "facility_id", "parent_department_id" });

            migrationBuilder.CreateIndex(
                name: "ux_departments_hospital_facility_code",
                schema: "organization",
                table: "departments",
                columns: new[] { "hospital_id", "facility_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_facilities_hospital_id_code",
                schema: "organization",
                table: "facilities",
                columns: new[] { "hospital_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_hospitals_code",
                schema: "organization",
                table: "hospitals",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_specialties_hospital_id_code",
                schema: "organization",
                table: "specialties",
                columns: new[] { "hospital_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_department_assignments_hospital_id_department_id",
                schema: "organization",
                table: "staff_department_assignments",
                columns: new[] { "hospital_id", "department_id" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_department_assignments_hospital_id_staff_profile_id",
                schema: "organization",
                table: "staff_department_assignments",
                columns: new[] { "hospital_id", "staff_profile_id" });

            migrationBuilder.CreateIndex(
                name: "ux_staff_assignments_active_primary",
                schema: "organization",
                table: "staff_department_assignments",
                column: "staff_profile_id",
                unique: true,
                filter: "is_primary AND ends_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_staff_assignments_active_staff_department",
                schema: "organization",
                table: "staff_department_assignments",
                columns: new[] { "staff_profile_id", "department_id" },
                unique: true,
                filter: "ends_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_staff_profiles_hospital_id_primary_specialty_id",
                schema: "organization",
                table: "staff_profiles",
                columns: new[] { "hospital_id", "primary_specialty_id" });

            migrationBuilder.CreateIndex(
                name: "ux_staff_profiles_hospital_id_person_id",
                schema: "organization",
                table: "staff_profiles",
                columns: new[] { "hospital_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_staff_profiles_hospital_id_staff_number",
                schema: "organization",
                table: "staff_profiles",
                columns: new[] { "hospital_id", "staff_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_department_assignments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "staff_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "facilities",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "specialties",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "hospitals",
                schema: "organization");
        }
    }
}
