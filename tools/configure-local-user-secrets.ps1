[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "Low")]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$environmentFile = Join-Path $repositoryRoot ".env"
$hostProject = Join-Path $repositoryRoot "src\HospitalManagement.Host\HospitalManagement.Host.csproj"

function Get-RequiredEnvironmentFileValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $prefix = "$Name="
    $line = Get-Content -LiteralPath $environmentFile |
        Where-Object { $_.StartsWith($prefix, [System.StringComparison]::Ordinal) } |
        Select-Object -First 1

    if ($null -eq $line) {
        throw "Missing required local setting '$Name' in .env."
    }

    $value = $line.Substring($prefix.Length)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Local setting '$Name' in .env cannot be empty."
    }

    return $value
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found."
}

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing ignored .env. Run tools/start-local-infrastructure.ps1 first."
}

$connectionStringBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
$connectionStringBuilder["Host"] = "127.0.0.1"
$connectionStringBuilder["Port"] = Get-RequiredEnvironmentFileValue -Name "HMS_POSTGRES_PORT"
$connectionStringBuilder["Database"] = Get-RequiredEnvironmentFileValue -Name "HMS_POSTGRES_DB"
$connectionStringBuilder["Username"] = Get-RequiredEnvironmentFileValue -Name "HMS_POSTGRES_USER"
$connectionStringBuilder["Password"] = Get-RequiredEnvironmentFileValue -Name "HMS_POSTGRES_PASSWORD"
$connectionStringBuilder["Include Error Detail"] = "false"

$secretPayload = [ordered]@{
    "ConnectionStrings:HospitalDatabase" = $connectionStringBuilder.ConnectionString
    "HospitalManagement:ObjectStorage:AccessKey" = Get-RequiredEnvironmentFileValue -Name "HMS_MINIO_ROOT_USER"
    "HospitalManagement:ObjectStorage:SecretKey" = Get-RequiredEnvironmentFileValue -Name "HMS_MINIO_ROOT_PASSWORD"
} | ConvertTo-Json

if ($PSCmdlet.ShouldProcess($hostProject, "Synchronize three local Development user-secret keys")) {
    [xml] $projectDocument = Get-Content -LiteralPath $hostProject -Raw
    $userSecretsIdNode = $projectDocument.SelectSingleNode("//*[local-name()='UserSecretsId']")
    if (($null -eq $userSecretsIdNode) -or [string]::IsNullOrWhiteSpace($userSecretsIdNode.InnerText)) {
        throw "Host project does not define UserSecretsId."
    }

    $userSecretsId = $userSecretsIdNode.InnerText.Trim()
    $isWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    if ($isWindows) {
        $userSecretsRoot = Join-Path `
            ([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)) `
            "Microsoft\UserSecrets"
    }
    else {
        $userSecretsRoot = Join-Path `
            ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) `
            ".microsoft/usersecrets"
    }

    $secretDirectory = Join-Path $userSecretsRoot $userSecretsId
    $secretFile = Join-Path $secretDirectory "secrets.json"
    [System.IO.Directory]::CreateDirectory($secretDirectory) | Out-Null

    $mergedSecrets = [ordered]@{}
    if (Test-Path -LiteralPath $secretFile -PathType Leaf) {
        $existingSecrets = Get-Content -LiteralPath $secretFile -Raw | ConvertFrom-Json
        foreach ($property in $existingSecrets.PSObject.Properties) {
            $mergedSecrets[$property.Name] = $property.Value
        }
    }

    $newSecrets = $secretPayload | ConvertFrom-Json
    foreach ($property in $newSecrets.PSObject.Properties) {
        $mergedSecrets[$property.Name] = $property.Value
    }

    $serializedSecrets = $mergedSecrets | ConvertTo-Json -Depth 20
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    $temporarySecretFile = Join-Path $secretDirectory "secrets.$([Guid]::NewGuid().ToString('N')).tmp"
    $backupSecretFile = Join-Path $secretDirectory "secrets.$([Guid]::NewGuid().ToString('N')).backup"

    try {
        [System.IO.File]::WriteAllText($temporarySecretFile, $serializedSecrets, $utf8WithoutBom)
        if (Test-Path -LiteralPath $secretFile -PathType Leaf) {
            [System.IO.File]::Replace($temporarySecretFile, $secretFile, $backupSecretFile)
        }
        else {
            [System.IO.File]::Move($temporarySecretFile, $secretFile)
        }
    }
    finally {
        if (Test-Path -LiteralPath $temporarySecretFile) {
            Remove-Item -LiteralPath $temporarySecretFile -Force
        }
        if (Test-Path -LiteralPath $backupSecretFile) {
            Remove-Item -LiteralPath $backupSecretFile -Force
        }
    }

    $serializedSecrets = $null
    $mergedSecrets = $null
    $newSecrets = $null
    Write-Host "[OK] Configured three local Development secrets; values were not displayed."
}

$secretPayload = $null
$connectionStringBuilder.Clear()
