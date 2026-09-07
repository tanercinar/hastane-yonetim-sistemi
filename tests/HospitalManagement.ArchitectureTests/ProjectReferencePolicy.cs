using System.Xml.Linq;

namespace HospitalManagement.ArchitectureTests;

internal static class ProjectReferencePolicy
{
    private const string BuildingBlocks = "HospitalManagement.BuildingBlocks";
    private const string Contracts = "HospitalManagement.Contracts";
    private const string Host = "HospitalManagement.Host";
    private const string Ui = "HospitalManagement.UI";
    private const string WebClient = "HospitalManagement.Web.Client";
    private const string Maui = "HospitalManagement.Maui";
    private const string ModulePrefix = "HospitalManagement.Modules.";

    private static readonly HashSet<string> HostTargets = new(
        ArchitectureModel.ProductionAssemblyNames.Where(name => name != Host),
        StringComparer.Ordinal);

    internal static readonly HashSet<string> KnownProductionProjects = new(
        ArchitectureModel.ProductionAssemblyNames.Append(Maui),
        StringComparer.Ordinal);

    internal static bool IsAllowed(string source, string target)
    {
        return source switch
        {
            Host => HostTargets.Contains(target),
            WebClient => target is Ui or Contracts,
            Maui => target is Ui or Contracts,
            Ui => target == Contracts,
            BuildingBlocks or Contracts => false,
            _ when source.StartsWith(ModulePrefix, StringComparison.Ordinal) =>
                target == BuildingBlocks,
            _ => false,
        };
    }
}

internal sealed record ProjectDefinition(
    string Name,
    string Path,
    IReadOnlyList<ProjectReferenceDefinition> References);

internal sealed record ProjectReferenceDefinition(string Name, string Path);

internal static class RepositoryProjects
{
    internal static IReadOnlyList<ProjectDefinition> LoadProductionProjects()
    {
        var sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");

        return Directory
            .EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(LoadProject)
            .ToArray();
    }

    private static ProjectDefinition LoadProject(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project directory not found: {projectPath}");

        var references = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include =>
            {
                var referencePath = Path.GetFullPath(Path.Combine(projectDirectory, include!));
                return new ProjectReferenceDefinition(
                    Path.GetFileNameWithoutExtension(referencePath),
                    referencePath);
            })
            .OrderBy(reference => reference.Name, StringComparer.Ordinal)
            .ToArray();

        return new ProjectDefinition(
            Path.GetFileNameWithoutExtension(projectPath),
            Path.GetFullPath(projectPath),
            references);
    }

    internal static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HospitalManagement.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Repository root was not found above {AppContext.BaseDirectory}.");
    }
}
