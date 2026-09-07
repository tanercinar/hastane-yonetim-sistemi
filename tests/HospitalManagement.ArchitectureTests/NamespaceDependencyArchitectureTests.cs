using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace HospitalManagement.ArchitectureTests;

public sealed class NamespaceDependencyArchitectureTests
{
    private static readonly IObjectProvider<IType> DomainTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^HospitalManagement\.Modules\.[^.]+\.Domain(?:\.|$)")
        .As("module Domain types");

    private static readonly IObjectProvider<IType> ApplicationTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^HospitalManagement\.Modules\.[^.]+\.Application(?:\.|$)")
        .As("module Application types");

    private static readonly IObjectProvider<IType> ModuleTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^HospitalManagement\.Modules\.[^.]+(?:\.|$)")
        .As("module types");

    private static readonly IObjectProvider<IType> SharedTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^HospitalManagement\.(?:BuildingBlocks|Contracts)(?:\.|$)")
        .As("shared BuildingBlocks and Contracts types");

    private static readonly IObjectProvider<IType> DomainForbiddenTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^(?:HospitalManagement\.(?:Host|UI|Web\.Client)(?:\.|$)|" +
            @"HospitalManagement\.Modules\.[^.]+\.(?:Application|Infrastructure|Endpoints)(?:\.|$)|" +
            @"Microsoft\.(?:AspNetCore|EntityFrameworkCore)(?:\.|$))")
        .As("UI, ASP.NET Core, EF Core, Application, Infrastructure, or Endpoints types");

    private static readonly IObjectProvider<IType> ApplicationForbiddenTypes = Types()
        .That()
        .HaveFullNameMatching(
            @"^(?:HospitalManagement\.(?:Host|UI|Web\.Client)(?:\.|$)|" +
            @"HospitalManagement\.Modules\.[^.]+\.(?:Infrastructure|Endpoints)(?:\.|$)|" +
            @"Microsoft\.(?:AspNetCore|EntityFrameworkCore)(?:\.|$))")
        .As("UI, ASP.NET Core, EF Core, Infrastructure, or Endpoints types");

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainTypesMustNotDependOnTechnicalOrOuterLayers()
    {
        Types()
            .That()
            .Are(DomainTypes)
            .Should()
            .NotDependOnAny(DomainForbiddenTypes)
            .Because("Domain must remain independent from UI, EF Core, ASP.NET Core, and outer layers")
            .WithoutRequiringPositiveResults()
            .Check(ArchitectureModel.Value);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void ApplicationTypesMustNotDependOnPresentationOrInfrastructure()
    {
        Types()
            .That()
            .Are(ApplicationTypes)
            .Should()
            .NotDependOnAny(ApplicationForbiddenTypes)
            .Because("Application rules must remain host-agnostic and persistence-agnostic")
            .WithoutRequiringPositiveResults()
            .Check(ArchitectureModel.Value);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void SharedTypesMustNotDependOnModules()
    {
        Types()
            .That()
            .Are(SharedTypes)
            .Should()
            .NotDependOnAny(ModuleTypes)
            .Because("shared projects cannot point inward to a business module")
            .Check(ArchitectureModel.Value);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void ModuleTypesMustNotDependOnForeignInfrastructure()
    {
        foreach (var moduleName in ArchitectureModel.ModuleNames)
        {
            var sourceModuleTypes = Types()
                .That()
                .HaveFullNameMatching(
                    $@"^HospitalManagement\.Modules\.{moduleName}(?:\.|$)")
                .As($"{moduleName} module types");
            var foreignInfrastructureTypes = Types()
                .That()
                .HaveFullNameMatching(
                    $@"^HospitalManagement\.Modules\.(?!{moduleName}(?:\.|$))[^.]+\.Infrastructure(?:\.|$)")
                .As($"infrastructure owned by modules other than {moduleName}");

            Types()
                .That()
                .Are(sourceModuleTypes)
                .Should()
                .NotDependOnAny(foreignInfrastructureTypes)
                .Because("a module cannot access another module's Infrastructure area")
                .WithoutRequiringPositiveResults()
                .Check(ArchitectureModel.Value);
        }
    }
}
