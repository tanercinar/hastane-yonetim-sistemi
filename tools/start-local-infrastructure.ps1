[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$composeFile = Join-Path $repositoryRoot "compose.yaml"
$environmentFile = Join-Path $repositoryRoot ".env"

function New-HexSecret {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }

    return (($bytes | ForEach-Object { $_.ToString("x2") }) -join "")
}

function Invoke-DockerCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [switch] $Quiet
    )

    $previousErrorActionPreference = $ErrorActionPreference

    try {
        # Windows PowerShell 5.1 turns native stderr into a terminating error when
        # ErrorActionPreference is Stop. Docker exit codes remain authoritative.
        $ErrorActionPreference = "Continue"

        if ($Quiet) {
            & docker @Arguments *> $null
        }
        else {
            & docker @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
        }

        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker CLI was not found. Install Docker Desktop and run this command again."
}

if (-not (Test-Path -LiteralPath $environmentFile)) {
    $postgresPassword = New-HexSecret
    $minioPassword = New-HexSecret
    $environmentLines = @(
        "# Generated for local development. Do not commit or share this file.",
        "HMS_POSTGRES_DB=hospital_management",
        "HMS_POSTGRES_USER=hospital_app",
        "HMS_POSTGRES_PASSWORD=$postgresPassword",
        "HMS_POSTGRES_PORT=5432",
        "",
        "HMS_MINIO_ROOT_USER=hospital_local_admin",
        "HMS_MINIO_ROOT_PASSWORD=$minioPassword",
        "HMS_MINIO_API_PORT=9000",
        "HMS_MINIO_CONSOLE_PORT=9001",
        "",
        "HMS_MAILPIT_SMTP_PORT=1025",
        "HMS_MAILPIT_WEB_PORT=8025"
    )
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($environmentFile, $environmentLines, $utf8WithoutBom)
    Write-Host "[OK] Generated ignored .env with random local credentials."
}
else {
    Write-Host "[OK] Reusing existing ignored .env; credentials were not changed."
}

Push-Location $repositoryRoot

try {
    $dockerInfoExitCode = Invoke-DockerCommand -Arguments @("info", "--format", "{{.ServerVersion}}") -Quiet
    if ($dockerInfoExitCode -ne 0) {
        throw "Docker daemon is unavailable. Start Docker Desktop and run this command again."
    }

    $composeArguments = @("compose", "--env-file", $environmentFile, "--file", $composeFile)
    $configExitCode = Invoke-DockerCommand -Arguments ($composeArguments + @("config", "--quiet"))
    if ($configExitCode -ne 0) {
        throw "Docker Compose configuration validation failed."
    }

    $startExitCode = Invoke-DockerCommand -Arguments ($composeArguments + @("up", "--detach", "--wait", "--wait-timeout", "300", "--build"))
    if ($startExitCode -ne 0) {
        throw "Local infrastructure did not become healthy. Inspect it with: docker compose --env-file .env logs"
    }

    & (Join-Path $PSScriptRoot "test-local-infrastructure.ps1")
}
finally {
    Pop-Location
}
