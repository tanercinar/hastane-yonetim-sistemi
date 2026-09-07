[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

Write-Host "==> Platform Paketleme ve Güvenlik Doğrulaması Başlatılıyor..." -ForegroundColor Cyan

# 1. Gizli Anahtar ve Keystore Dosya Taraması
Write-Host "1. Sertifika ve Keystore Dosya Taraması..." -ForegroundColor Yellow
$forbiddenExtensions = @("*.keystore", "*.jks", "*.pfx", "*.p12", "*.cer", "*.key")
$leakedFiles = @()

foreach ($ext in $forbiddenExtensions) {
    $found = Get-ChildItem -Path $repoRoot -Recurse -Include $ext -Exclude "bin", "obj", ".git" -File -ErrorAction SilentlyContinue
    if ($found) {
        $leakedFiles += $found
    }
}

if ($leakedFiles.Count -gt 0) {
    Write-Error "GÜVENLİK İHLALİ: Repoda yasaklı imzalama/sertifika dosyaları bulundu:`n$($leakedFiles | ForEach-Object { $_.FullName } | Out-String)"
    exit 1
}

Write-Host "   PASS: Repoda hiçbir özel anahtar veya keystore dosyası bulunamadı." -ForegroundColor Green

# 2. Proje Dosyalarında Sabit Parola/Secret Taraması
Write-Host "2. Proje Dosyalarında Açık Anahtar/Parola Taraması..." -ForegroundColor Yellow
$projectFiles = Get-ChildItem -Path (Join-Path $repoRoot "src") -Recurse -Filter "*.csproj" -File

foreach ($proj in $projectFiles) {
    $content = Get-Content $proj.FullName -Raw
    if ($content -match "PackageCertificatePassword" -or $content -match "AndroidSigningKeyPass" -or $content -match "AndroidSigningStorePass") {
        Write-Error "GÜVENLİK İHLALİ: $($proj.Name) içerisinde açık imzalama parolası tespit edildi!"
        exit 1
    }
}

Write-Host "   PASS: Proje dosyalarında açık imzalama kimlik bilgisi bulunmuyor." -ForegroundColor Green

# 3. Windows ve Android MAUI Derleme Kontrolü
if (-not $SkipBuild) {
    Write-Host "3. MAUI Windows Hedefi Derleme Testi..." -ForegroundColor Yellow
    $winBuild = dotnet build "$repoRoot/src/HospitalManagement.Maui/HospitalManagement.Maui.csproj" -f net10.0-windows10.0.19041.0 -c Debug --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Windows hedefi derlenemedi!"
        exit 1
    }
    Write-Host "   PASS: Windows hedefi başarıyla derlendi." -ForegroundColor Green

    Write-Host "4. MAUI Android Hedefi Derleme Testi..." -ForegroundColor Yellow
    $androidBuild = dotnet build "$repoRoot/src/HospitalManagement.Maui/HospitalManagement.Maui.csproj" -f net10.0-android -c Debug --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Android hedefi derlenemedi!"
        exit 1
    }
    Write-Host "   PASS: Android hedefi başarıyla derlendi." -ForegroundColor Green
}

[PSCustomObject]@{
    Status              = "PASS"
    WindowsTarget       = "net10.0-windows10.0.19041.0"
    AndroidTarget       = "net10.0-android"
    SecretLeaksDetected = 0
    KeystoresCommitted  = 0
} | Format-List
