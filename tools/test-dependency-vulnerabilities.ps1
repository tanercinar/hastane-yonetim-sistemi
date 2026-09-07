[CmdletBinding()]
param(
    [string]$Solution = "HospitalManagement.slnx"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot $Solution
$nugetConfigPath = Join-Path $repositoryRoot "NuGet.Config"

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "Solution file was not found: $solutionPath"
}

if (-not (Test-Path -LiteralPath $nugetConfigPath -PathType Leaf)) {
    throw "Repository NuGet configuration was not found: $nugetConfigPath"
}

Push-Location $repositoryRoot
try {
    $output = @(
        & dotnet package list `
            --project $solutionPath `
            --vulnerable `
            --include-transitive `
            --format json `
            --output-version 1 `
            --configfile $nugetConfigPath `
            --no-restore 2>&1
    )
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$jsonText = $output -join [Environment]::NewLine
if ($exitCode -ne 0) {
    throw "NuGet vulnerability query failed with exit code ${exitCode}:$([Environment]::NewLine)$jsonText"
}

try {
    $report = $jsonText | ConvertFrom-Json
}
catch {
    throw "NuGet vulnerability output was not valid JSON. $($_.Exception.Message)"
}

$findings = @(
    foreach ($project in @($report.projects)) {
        foreach ($framework in @($project.frameworks)) {
            $packageGroups = @(
                $framework.PSObject.Properties |
                    Where-Object Name -In @("topLevelPackages", "transitivePackages")
            )

            foreach ($group in $packageGroups) {
                foreach ($package in @($group.Value)) {
                    [PSCustomObject]@{
                        Project = $project.path
                        Framework = $framework.framework
                        Package = $package.id
                        Version = $package.resolvedVersion
                    }
                }
            }
        }
    }
)

if ($findings.Count -gt 0) {
    $details = $findings |
        Sort-Object Project, Package |
        Format-Table -AutoSize |
        Out-String
    throw "Known vulnerable NuGet packages were found:$([Environment]::NewLine)$details"
}

Write-Host "Dependency vulnerability gate PASS: no known vulnerable direct or transitive NuGet package was reported."
