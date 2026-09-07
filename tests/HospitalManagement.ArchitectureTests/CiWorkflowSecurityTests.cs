namespace HospitalManagement.ArchitectureTests;

public sealed class CiWorkflowSecurityTests
{
    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Roadmap", "F01-G11")]
    public void PullRequestWorkflowIsSecretFreeReadOnlyAndUsesPinnedActions()
    {
        var workflowPath = Path.Combine(
            RepositoryProjects.FindRepositoryRoot(),
            ".github",
            "workflows",
            "ci.yml");
        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("pull_request_target", workflow, StringComparison.Ordinal);
        Assert.Contains("permissions:\n  contents: read", NormalizeNewLines(workflow), StringComparison.Ordinal);
        Assert.Contains("persist-credentials: false", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("secrets.", workflow, StringComparison.OrdinalIgnoreCase);

        var externalActionLines = NormalizeNewLines(workflow)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("uses: ", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(externalActionLines);
        Assert.All(externalActionLines, line =>
        {
            var reference = line.Split('#', 2)[0].Trim().Split('@', 2)[1];
            Assert.Equal(40, reference.Length);
            Assert.All(reference, character => Assert.True(Uri.IsHexDigit(character)));
        });
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Roadmap", "F01-G11")]
    public void QualityJobContainsEveryRequiredBlockingGate()
    {
        var workflowPath = Path.Combine(
            RepositoryProjects.FindRepositoryRoot(),
            ".github",
            "workflows",
            "ci.yml");
        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("dotnet restore HospitalManagement.slnx --locked-mode", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet format HospitalManagement.slnx --no-restore --verify-no-changes", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet build HospitalManagement.slnx", workflow, StringComparison.Ordinal);
        Assert.Contains("test-dependency-vulnerabilities.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("playwright.ps1 install --with-deps chromium", workflow, StringComparison.Ordinal);
        Assert.Contains("HospitalManagement.IntegrationTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("HospitalManagement.EndToEndTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("Browser end-to-end tests", workflow, StringComparison.Ordinal);
        Assert.Contains("XPlat Code Coverage", workflow, StringComparison.Ordinal);
        Assert.Contains("Upload test and coverage reports", workflow, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Roadmap", "F13-G06")]
    public void CentralPackageManagementEnforcesPinnedVersionsAndDisablesOverrides()
    {
        var propsPath = Path.Combine(
            RepositoryProjects.FindRepositoryRoot(),
            "Directory.Packages.props");
        var doc = System.Xml.Linq.XDocument.Load(propsPath);

        var manageVersions = doc.Descendants("ManagePackageVersionsCentrally").FirstOrDefault()?.Value;
        var overrideEnabled = doc.Descendants("CentralPackageVersionOverrideEnabled").FirstOrDefault()?.Value;

        Assert.Equal("true", manageVersions);
        Assert.Equal("false", overrideEnabled);

        var packageVersions = doc.Descendants("PackageVersion").ToArray();
        Assert.NotEmpty(packageVersions);

        foreach (var pv in packageVersions)
        {
            var version = pv.Attribute("Version")?.Value;
            Assert.False(string.IsNullOrWhiteSpace(version), $"Package {pv.Attribute("Include")?.Value} must have a version.");
            Assert.DoesNotContain("*", version);
            Assert.DoesNotContain("latest", version, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Roadmap", "F13-G06")]
    public void ComposeServicesUsePinnedImmutableImageVersions()
    {
        var composePath = Path.Combine(
            RepositoryProjects.FindRepositoryRoot(),
            "compose.yaml");
        var composeText = File.ReadAllText(composePath);

        Assert.DoesNotContain(":latest", composeText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("postgres:18.6", composeText, StringComparison.Ordinal);
        Assert.Contains("axllent/mailpit:v1.31.0", composeText, StringComparison.Ordinal);
    }

    private static string NormalizeNewLines(string value) => value.ReplaceLineEndings("\n");
}
