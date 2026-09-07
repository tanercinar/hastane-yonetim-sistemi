[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$hostProject = Join-Path $repositoryRoot 'src/HospitalManagement.Host/HospitalManagement.Host.csproj'
$identityAccessProject = Join-Path $repositoryRoot 'src/Modules/IdentityAccess/HospitalManagement.Modules.IdentityAccess.csproj'
$organizationProject = Join-Path $repositoryRoot 'src/Modules/Organization/HospitalManagement.Modules.Organization.csproj'
$auditPrivacyProject = Join-Path $repositoryRoot 'src/Modules/AuditPrivacy/HospitalManagement.Modules.AuditPrivacy.csproj'
$patientsProject = Join-Path $repositoryRoot 'src/Modules/Patients/HospitalManagement.Modules.Patients.csproj'
$schedulingProject = Join-Path $repositoryRoot 'src/Modules/Scheduling/HospitalManagement.Modules.Scheduling.csproj'
$notificationsProject = Join-Path $repositoryRoot 'src/Modules/Notifications/HospitalManagement.Modules.Notifications.csproj'
$clinicalRecordsProject = Join-Path $repositoryRoot 'src/Modules/ClinicalRecords/HospitalManagement.Modules.ClinicalRecords.csproj'
$pharmacyProject = Join-Path $repositoryRoot 'src/Modules/Pharmacy/HospitalManagement.Modules.Pharmacy.csproj'
$diagnosticsProject = Join-Path $repositoryRoot 'src/Modules/Diagnostics/HospitalManagement.Modules.Diagnostics.csproj'
$inpatientProject = Join-Path $repositoryRoot 'src/Modules/Inpatient/HospitalManagement.Modules.Inpatient.csproj'
$emergencyProject = Join-Path $repositoryRoot 'src/Modules/Emergency/HospitalManagement.Modules.Emergency.csproj'
$surgeryProject = Join-Path $repositoryRoot 'src/Modules/SurgeryCriticalCare/HospitalManagement.Modules.SurgeryCriticalCare.csproj'
$specialtyCareProject = Join-Path $repositoryRoot 'src/Modules/SpecialtyCare/HospitalManagement.Modules.SpecialtyCare.csproj'
$interoperabilityProject = Join-Path $repositoryRoot 'src/Modules/Interoperability/HospitalManagement.Modules.Interoperability.csproj'
$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT

try {
    Push-Location $repositoryRoot
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $hostProject `
        --startup-project $hostProject `
        --context DatabaseBootstrapDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Database foundation migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $organizationProject `
        --startup-project $hostProject `
        --context OrganizationDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Organization migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $identityAccessProject `
        --startup-project $hostProject `
        --context IdentityAccessDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "IdentityAccess migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $auditPrivacyProject `
        --startup-project $hostProject `
        --context AuditPrivacyDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "AuditPrivacy migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $patientsProject `
        --startup-project $hostProject `
        --context PatientsDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Patients migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $schedulingProject `
        --startup-project $hostProject `
        --context SchedulingDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Scheduling migration failed with exit code $LASTEXITCODE."
    }

    & dotnet ef database update `
        --project $notificationsProject `
        --startup-project $hostProject `
        --context NotificationsDbContext `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Notifications migration failed with exit code $LASTEXITCODE."
    }

    $remainingModuleMigrations = @(
        @{ Name = 'ClinicalRecords'; Project = $clinicalRecordsProject; Context = 'ClinicalRecordsDbContext' }
        @{ Name = 'Pharmacy'; Project = $pharmacyProject; Context = 'PharmacyDbContext' }
        @{ Name = 'Diagnostics'; Project = $diagnosticsProject; Context = 'DiagnosticsDbContext' }
        @{ Name = 'Inpatient'; Project = $inpatientProject; Context = 'InpatientDbContext' }
        @{ Name = 'Emergency'; Project = $emergencyProject; Context = 'EmergencyDbContext' }
        @{ Name = 'SurgeryCriticalCare'; Project = $surgeryProject; Context = 'SurgeryDbContext' }
        @{ Name = 'SpecialtyCare'; Project = $specialtyCareProject; Context = 'SpecialtyCareDbContext' }
        @{ Name = 'Interoperability'; Project = $interoperabilityProject; Context = 'InteroperabilityDbContext' }
    )

    foreach ($migration in $remainingModuleMigrations) {
        & dotnet ef database update `
            --project $migration.Project `
            --startup-project $hostProject `
            --context $migration.Context `
            --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "$($migration.Name) migration failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host 'All platform and registered module migrations completed. No connection secrets were displayed.'
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
    Pop-Location
}
