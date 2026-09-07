using System.Collections.ObjectModel;

namespace HospitalManagement.Host.Database;

public static class DatabaseSchemas
{
    public const string Platform = "platform";

    private static readonly ReadOnlyCollection<ModuleSchema> ModuleSchemaValues = Array.AsReadOnly<ModuleSchema>(
    [
        new("AuditPrivacy", "audit_privacy"),
        new("ClinicalRecords", "clinical_records"),
        new("Diagnostics", "diagnostics"),
        new("Emergency", "emergency"),
        new("IdentityAccess", "identity_access"),
        new("Inpatient", "inpatient"),
        new("Interoperability", "interoperability"),
        new("Inventory", "inventory"),
        new("Notifications", "notifications"),
        new("Organization", "organization"),
        new("Patients", "patients"),
        new("Pharmacy", "pharmacy"),
        new("Reporting", "reporting"),
        new("Scheduling", "scheduling"),
        new("SpecialtyCare", "specialty_care"),
        new("SurgeryCriticalCare", "surgery_critical_care"),
    ]);

    private static readonly ReadOnlyCollection<string> AllSchemaValues = Array.AsReadOnly(
        [Platform, .. ModuleSchemaValues.Select(module => module.SchemaName)]);

    public static IReadOnlyList<ModuleSchema> Modules => ModuleSchemaValues;

    public static IReadOnlyList<string> All => AllSchemaValues;
}

public sealed record ModuleSchema(string ModuleName, string SchemaName);
