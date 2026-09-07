[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$composeFile = Join-Path $repositoryRoot "compose.yaml"
$environmentFile = Join-Path $repositoryRoot ".env"
$exampleEnvironmentFile = Join-Path $repositoryRoot ".env.example"

function Get-EnvironmentFileValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $DefaultValue
    )

    $prefix = "$Name="
    $line = Get-Content -LiteralPath $environmentFile |
        Where-Object { $_.StartsWith($prefix, [System.StringComparison]::Ordinal) } |
        Select-Object -First 1

    if ($null -eq $line) {
        return $DefaultValue
    }

    $value = $line.Substring($prefix.Length)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $DefaultValue
    }

    return $value
}

function Invoke-DockerCaptured {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference

    try {
        # See the start script: Windows PowerShell 5.1 promotes native stderr
        # when ErrorActionPreference is Stop, so evaluate Docker's exit code.
        $ErrorActionPreference = "Continue"
        $output = & docker @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [PSCustomObject]@{
        ExitCode = $exitCode
        Output = $output
    }
}

function Assert-HttpEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $Uri
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Host "[OK] $Name health endpoint returned HTTP 200."
                return
            }
        }
        catch {
            $lastError = $_
        }

        Start-Sleep -Milliseconds 300
    }

    throw "$Name health endpoint failed: $lastError"
}

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Missing .env. Run tools/start-local-infrastructure.ps1 first."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker CLI was not found."
}

Push-Location $repositoryRoot

try {
    $secretVariableNames = @("HMS_POSTGRES_PASSWORD", "HMS_MINIO_ROOT_PASSWORD")
    $savedSecretVariables = @{}

    try {
        foreach ($variableName in $secretVariableNames) {
            $savedSecretVariables[$variableName] = [Environment]::GetEnvironmentVariable($variableName, "Process")
            [Environment]::SetEnvironmentVariable($variableName, $null, "Process")
        }

        $negativeResult = Invoke-DockerCaptured -Arguments @(
            "compose", "--env-file", $exampleEnvironmentFile, "--file", $composeFile, "config", "--quiet"
        )
    }
    finally {
        foreach ($variableName in $secretVariableNames) {
            [Environment]::SetEnvironmentVariable($variableName, $savedSecretVariables[$variableName], "Process")
        }
    }

    if ($negativeResult.ExitCode -eq 0) {
        throw "Negative configuration test failed: empty example secrets were accepted."
    }
    Write-Host "[OK] Empty example secrets are rejected by Compose validation."

    $composeArguments = @("compose", "--env-file", $environmentFile, "--file", $composeFile)
    $configResult = Invoke-DockerCaptured -Arguments ($composeArguments + @("config", "--quiet"))
    if ($configResult.ExitCode -ne 0) {
        throw "Docker Compose configuration validation failed."
    }
    Write-Host "[OK] Local Docker Compose configuration is valid."

    $postgresUser = Get-EnvironmentFileValue -Name "HMS_POSTGRES_USER" -DefaultValue "hospital_app"
    $postgresDatabase = Get-EnvironmentFileValue -Name "HMS_POSTGRES_DB" -DefaultValue "hospital_management"
    $postgresResult = Invoke-DockerCaptured -Arguments ($composeArguments + @(
        "exec", "--no-TTY", "postgres", "psql", "--username", $postgresUser, "--dbname", $postgresDatabase,
        "--no-align", "--tuples-only", "--command", "SELECT 1;"
    ))
    if (($postgresResult.ExitCode -ne 0) -or (($postgresResult.Output | Out-String).Trim() -ne "1")) {
        throw "PostgreSQL smoke query failed."
    }
    Write-Host "[OK] PostgreSQL accepted a smoke query."

    $minioApiPort = Get-EnvironmentFileValue -Name "HMS_MINIO_API_PORT" -DefaultValue "9000"
    $minioConsolePort = Get-EnvironmentFileValue -Name "HMS_MINIO_CONSOLE_PORT" -DefaultValue "9001"
    $mailpitSmtpPort = Get-EnvironmentFileValue -Name "HMS_MAILPIT_SMTP_PORT" -DefaultValue "1025"
    $mailpitWebPort = Get-EnvironmentFileValue -Name "HMS_MAILPIT_WEB_PORT" -DefaultValue "8025"

    Assert-HttpEndpoint -Name "MinIO" -Uri "http://127.0.0.1:$minioApiPort/minio/health/live"
    Assert-HttpEndpoint -Name "Mailpit" -Uri "http://127.0.0.1:$mailpitWebPort/readyz"

    $testId = [Guid]::NewGuid().ToString("N")
    $subject = "DEMO local infrastructure smoke $testId"
    $message = New-Object System.Net.Mail.MailMessage
    $smtpClient = New-Object System.Net.Mail.SmtpClient("127.0.0.1", [int] $mailpitSmtpPort)

    try {
        $message.From = "DEMO-sender@hospital.invalid"
        $message.To.Add("DEMO-recipient@hospital.invalid")
        $message.Subject = $subject
        $message.Body = "DEMO synthetic smoke message $testId"
        $smtpClient.EnableSsl = $false
        $smtpClient.Send($message)
    }
    finally {
        $message.Dispose()
        $smtpClient.Dispose()
    }

    $searchQuery = [Uri]::EscapeDataString(('subject:"{0}"' -f $subject))
    $messageCaptured = $false

    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            $capturedMessage = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$mailpitWebPort/view/latest.txt?query=$searchQuery" -TimeoutSec 5
            if (($capturedMessage.StatusCode -eq 200) -and $capturedMessage.Content.Contains($testId)) {
                $messageCaptured = $true
                break
            }
        }
        catch {
            # Mailpit may need a fraction of a second to index the SMTP message.
        }

        Start-Sleep -Milliseconds 300
    }

    if (-not $messageCaptured) {
        throw "Mailpit did not capture the DEMO SMTP smoke message."
    }
    Write-Host "[OK] Mailpit captured a synthetic DEMO SMTP message."

    Write-Host ""
    Write-Host "Local infrastructure is healthy:"
    Write-Host "  PostgreSQL : 127.0.0.1:$(Get-EnvironmentFileValue -Name 'HMS_POSTGRES_PORT' -DefaultValue '5432')"
    Write-Host "  MinIO API  : http://127.0.0.1:$minioApiPort"
    Write-Host "  MinIO UI   : http://127.0.0.1:$minioConsolePort"
    Write-Host "  Mailpit UI : http://127.0.0.1:$mailpitWebPort (MOCK)"
}
finally {
    Pop-Location
}
