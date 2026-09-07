using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCriticalResultNotificationIdempotency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(name: "clinical_notes", schema: "diagnostics", table: "lab_results", type: "character varying(2000)", maxLength: 2000, nullable: true, oldClrType: typeof(string), oldType: "character varying(1000)", oldMaxLength: 1000, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "string_value", schema: "diagnostics", table: "lab_result_items", type: "character varying(500)", maxLength: 500, nullable: true, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "reference_range_text", schema: "diagnostics", table: "lab_result_items", type: "character varying(200)", maxLength: 200, nullable: true, oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "parameter_name", schema: "diagnostics", table: "lab_result_items", type: "character varying(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "character varying(150)", oldMaxLength: 150);
        migrationBuilder.AlterColumn<string>(name: "value_type", schema: "diagnostics", table: "lab_catalog_parameters", type: "character varying(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30);
        migrationBuilder.AlterColumn<string>(name: "name", schema: "diagnostics", table: "lab_catalog_parameters", type: "character varying(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "character varying(150)", oldMaxLength: 150);
        migrationBuilder.AlterColumn<string>(name: "category", schema: "diagnostics", table: "lab_catalog_items", type: "character varying(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50);

        migrationBuilder.CreateIndex("IX_lab_results_created_at_utc", "lab_results", "created_at_utc", schema: "diagnostics");
        migrationBuilder.CreateIndex("IX_lab_result_items_parameter_code", "lab_result_items", "parameter_code", schema: "diagnostics");
        migrationBuilder.CreateIndex(
            "IX_critical_result_notifications_lab_result_id_parameter_code",
            "critical_result_notifications",
            new[] { "lab_result_id", "parameter_code" },
            schema: "diagnostics",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_lab_results_created_at_utc", "lab_results", schema: "diagnostics");
        migrationBuilder.DropIndex("IX_lab_result_items_parameter_code", "lab_result_items", schema: "diagnostics");
        migrationBuilder.DropIndex("IX_critical_result_notifications_lab_result_id_parameter_code", "critical_result_notifications", schema: "diagnostics");

        migrationBuilder.AlterColumn<string>(name: "clinical_notes", schema: "diagnostics", table: "lab_results", type: "character varying(1000)", maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "character varying(2000)", oldMaxLength: 2000, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "string_value", schema: "diagnostics", table: "lab_result_items", type: "character varying(200)", maxLength: 200, nullable: true, oldClrType: typeof(string), oldType: "character varying(500)", oldMaxLength: 500, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "reference_range_text", schema: "diagnostics", table: "lab_result_items", type: "character varying(100)", maxLength: 100, nullable: true, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "parameter_name", schema: "diagnostics", table: "lab_result_items", type: "character varying(150)", maxLength: 150, nullable: false, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200);
        migrationBuilder.AlterColumn<string>(name: "value_type", schema: "diagnostics", table: "lab_catalog_parameters", type: "character varying(30)", maxLength: 30, nullable: false, oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50);
        migrationBuilder.AlterColumn<string>(name: "name", schema: "diagnostics", table: "lab_catalog_parameters", type: "character varying(150)", maxLength: 150, nullable: false, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200);
        migrationBuilder.AlterColumn<string>(name: "category", schema: "diagnostics", table: "lab_catalog_items", type: "character varying(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100);
    }
}
