# Bağımlılık ve Tedarik Zinciri Güvenliği Sertleştirmesi (F13-G06)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin NuGet paket bağımlılıklarını, Docker container imajlarını, üçüncü taraf yazılım lisanslarını ve CI/CD tedarik zinciri (supply chain) sertleştirme mekanizmalarını belgeler.

## 2. Tedarik Zinciri Güvenlik Standartları

### 2.1. Merkezi Paket Yönetimi (Central Package Management - CPM)
Tüm çözümde paket sürümleri `Directory.Packages.props` dosyası üzerinden tek bir merkezden yönetilir:
- `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
- `<CentralPackageVersionOverrideEnabled>false</CentralPackageVersionOverrideEnabled>`

Bu sayede projelerin kendi `.csproj` dosyaları içerisinde bağımsız, sabitlenmemiş veya uyumsuz paket sürümü tanımlaması kesin olarak engellenir.

### 2.2. Kilitli Restore ve packages.lock.json
Tüm 26 proje için `packages.lock.json` dosyaları taahhüt edilmiştir. CI boru hattında ve yerel derlemelerde `dotnet restore --locked-mode` kullanılarak bağımlılık grafiğinin yetkisiz veya sessizce değişmesi engellenir.

### 2.3. Zafiyet Taraması ve CVE Taraması
Otomatik güvenlik kapısı betiği `tools/test-dependency-vulnerabilities.ps1` düzenli olarak çalıştırılır:
- Hem doğrudan (direct) hem de dolaylı (transitive) paketler taranır (`dotnet package list --vulnerable --include-transitive --format json`).
- **Sonuç**: 0 bilinen doğrudan veya dolaylı zafiyet (`Dependency vulnerability gate PASS: no known vulnerable direct or transitive NuGet package was reported`).

### 2.4. Container İmajı Sabitlemesi (compose.yaml)
Yerel altyapıda ve üretim şablonlarında `:latest` etiketi kullanımı yasaklanmıştır:
- `postgres:18.6`
- `axllent/mailpit:v1.31.0`
- `minio: RELEASE.2025-10-15T17-29-55Z`
- Tüm portlar yalnızca loopback arayüzüne (`127.0.0.1`) bağlanır; dış ağlara port açılmaz.

### 2.5. Yazılım Lisans Denetimi (Software Bill of Materials / SBOM)
Kullanılan tüm üçüncü taraf kütüphaneler incelenmiş olup ticari kullanıma ve açık kaynak dağıtımına uygun izin verici (permissive) lisanslara sahiptir:
- **MIT Lisansı**: `bunit`, `coverlet.collector`, `Testcontainers.PostgreSql`.
- **Apache-2.0 Lisansı**: `xunit`, `OpenTelemetry.*`, `TngTech.ArchUnitNET.xUnit`.
- **MIT / .NET Foundation**: `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.Extensions.*`.
- **PostgreSQL Lisansı**: `Npgsql`, `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Projede viral/copyleft (GPL, AGPL) lisanslı hiçbir paket bulunmamaktadır.

### 2.6. CI/CD İş Akışı Güvenliği (.github/workflows/ci.yml)
- Tüm GitHub Action eylemleri değişken etiketler (`@v4`, `@v3`) yerine 40 karakterlik değişmez Git commit SHA'larına sabitlenmiştir.
- Minimum izin ilkesi: `permissions: contents: read`.
- Gizli anahtar sızıntısı engeli: `persist-credentials: false`, `pull_request` tetikleyicisi (güvenli olmayan `pull_request_target` yasak).

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.ArchitectureTests/CiWorkflowSecurityTests.cs`:
  - `PullRequestWorkflowIsSecretFreeReadOnlyAndUsesPinnedActions`
  - `QualityJobContainsEveryRequiredBlockingGate`
  - `CentralPackageManagementEnforcesPinnedVersionsAndDisablesOverrides`
  - `ComposeServicesUsePinnedImmutableImageVersions`
- `tools/test-dependency-vulnerabilities.ps1` başarıyla geçmiştir.
