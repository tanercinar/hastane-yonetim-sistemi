using System.Xml.Linq;

namespace HospitalManagement.ArchitectureTests;

public sealed class ProjectReferenceArchitectureTests
{
    [Fact]
    [Trait("Category", "Architecture")]
    public void ProductionProjectsAreAllClassifiedByThePolicy()
    {
        var actualProjects = RepositoryProjects
            .LoadProductionProjects()
            .Select(project => project.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingFromPolicy = actualProjects
            .Except(ProjectReferencePolicy.KnownProductionProjects, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var missingFromSolution = ProjectReferencePolicy.KnownProductionProjects
            .Except(actualProjects, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingFromPolicy.Length == 0 && missingFromSolution.Length == 0,
            $"Unclassified projects: {string.Join(", ", missingFromPolicy)}; " +
            $"missing projects: {string.Join(", ", missingFromSolution)}.");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void ProductionProjectReferencesMustFollowAllowedDirection()
    {
        var projects = RepositoryProjects.LoadProductionProjects();
        var projectPaths = projects.ToDictionary(
            project => project.Name,
            project => project.Path,
            StringComparer.Ordinal);

        var violations = projects
            .SelectMany(project => project.References.Select(reference => (project, reference)))
            .Where(edge =>
                !projectPaths.TryGetValue(edge.reference.Name, out var expectedPath) ||
                !string.Equals(expectedPath, edge.reference.Path, StringComparison.OrdinalIgnoreCase) ||
                !ProjectReferencePolicy.IsAllowed(edge.project.Name, edge.reference.Name))
            .Select(edge =>
                $"{edge.project.Name} -> {edge.reference.Name} ({edge.reference.Path})")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Forbidden project references:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Theory]
    [Trait("Category", "Architecture")]
    [InlineData("HospitalManagement.Modules.Patients", "HospitalManagement.Modules.Scheduling")]
    [InlineData("HospitalManagement.BuildingBlocks", "HospitalManagement.Modules.Patients")]
    [InlineData("HospitalManagement.Contracts", "HospitalManagement.Host")]
    [InlineData("HospitalManagement.Web.Client", "HospitalManagement.Modules.Patients")]
    public void ForbiddenProjectReferenceIsRejected(string source, string target)
    {
        Assert.False(ProjectReferencePolicy.IsAllowed(source, target));
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Roadmap", "F01-KAPI")]
    public void WebClientRestoreGraphIsIndependentFromBuildConfiguration()
    {
        var repositoryRoot = RepositoryProjects.FindRepositoryRoot();
        var webClientDirectory = Path.Combine(
            repositoryRoot,
            "src",
            "HospitalManagement.Web.Client");
        var project = XDocument.Load(
            Path.Combine(webClientDirectory, "HospitalManagement.Web.Client.csproj"));
        var hotReloadSetting = project
            .Descendants()
            .Single(element => element.Name.LocalName == "WasmEnableHotReload")
            .Value;
        var lockFile = File.ReadAllText(Path.Combine(webClientDirectory, "packages.lock.json"));

        Assert.Equal("false", hotReloadSetting);
        Assert.DoesNotContain(
            "Microsoft.DotNet.HotReload.WebAssembly.Browser",
            lockFile,
            StringComparison.Ordinal);
    }
}
