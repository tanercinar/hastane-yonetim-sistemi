using Microsoft.EntityFrameworkCore.Migrations;

namespace HospitalManagement.Host.Database.Migrations;

public partial class InitialDatabaseFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "platform");
        migrationBuilder.EnsureSchema(name: "audit_privacy");
        migrationBuilder.EnsureSchema(name: "clinical_records");
        migrationBuilder.EnsureSchema(name: "diagnostics");
        migrationBuilder.EnsureSchema(name: "emergency");
        migrationBuilder.EnsureSchema(name: "identity_access");
        migrationBuilder.EnsureSchema(name: "inpatient");
        migrationBuilder.EnsureSchema(name: "interoperability");
        migrationBuilder.EnsureSchema(name: "inventory");
        migrationBuilder.EnsureSchema(name: "notifications");
        migrationBuilder.EnsureSchema(name: "organization");
        migrationBuilder.EnsureSchema(name: "patients");
        migrationBuilder.EnsureSchema(name: "pharmacy");
        migrationBuilder.EnsureSchema(name: "reporting");
        migrationBuilder.EnsureSchema(name: "scheduling");
        migrationBuilder.EnsureSchema(name: "specialty_care");
        migrationBuilder.EnsureSchema(name: "surgery_critical_care");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Forward-only policy: schemas may later contain module-owned clinical records.
        // A corrective migration must be used instead of destructively dropping them.
    }
}
