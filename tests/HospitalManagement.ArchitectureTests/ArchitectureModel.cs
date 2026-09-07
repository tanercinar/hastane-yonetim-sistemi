using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

using ReflectionAssembly = System.Reflection.Assembly;

namespace HospitalManagement.ArchitectureTests;

internal static class ArchitectureModel
{
    internal static readonly IReadOnlyList<string> ModuleNames =
    [
        "AuditPrivacy",
        "ClinicalRecords",
        "Diagnostics",
        "Emergency",
        "IdentityAccess",
        "Inpatient",
        "Interoperability",
        "Inventory",
        "Notifications",
        "Organization",
        "Patients",
        "Pharmacy",
        "Reporting",
        "Scheduling",
        "SpecialtyCare",
        "SurgeryCriticalCare",
    ];

    internal static readonly IReadOnlyList<string> ProductionAssemblyNames =
    [
        "HospitalManagement.BuildingBlocks",
        "HospitalManagement.Contracts",
        "HospitalManagement.Host",
        "HospitalManagement.UI",
        "HospitalManagement.Web.Client",
        .. ModuleNames.Select(name => $"HospitalManagement.Modules.{name}"),
    ];

    internal static Architecture Value { get; } = Build();

    private static Architecture Build()
    {
        var assemblies = ProductionAssemblyNames
            .Select(ReflectionAssembly.Load)
            .ToList();

        AddWhenAvailable(assemblies, "Microsoft.AspNetCore.Components");
        AddWhenAvailable(assemblies, "Microsoft.AspNetCore.Http.Abstractions");
        AddWhenAvailable(assemblies, "Microsoft.AspNetCore.Mvc.Core");
        AddWhenAvailable(assemblies, "Microsoft.EntityFrameworkCore");

        return new ArchLoader()
            .LoadAssemblies([.. assemblies])
            .Build();
    }

    private static void AddWhenAvailable(
        List<ReflectionAssembly> assemblies,
        string assemblyName)
    {
        try
        {
            assemblies.Add(ReflectionAssembly.Load(assemblyName));
        }
        catch (FileNotFoundException)
        {
            // Optional technical assemblies are loaded once a production project references them.
        }
    }
}
