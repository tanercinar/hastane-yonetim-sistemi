# Faz 1 Manuel Test Rehberi

Bu belge Faz 1 çözüm iskeletini yeni bir geliştirici gözüyle elle kabul etmek için hazırlanmıştır. Adımları sırayla ve repository kökünde çalıştırın. Bir zorunlu adım başarısız olursa sonraki adıma geçmeden sonucu ve terminal çıktısını kaydedin.

Faz 1 testi klinik özellikleri değil; deterministik kurulum, mimari sınırlar, yerel altyapı, yapılandırma güvenliği, migration, API/web kabuğu, gözlemlenebilirlik ve test/CI omurgasını kabul eder.

## 1. Güvenlik kuralları

- Yalnız sentetik `DEMO` veri kullanın.
- `.env`, user-secrets, token veya credential içeriğini ekran görüntüsüne, issue'ya ya da test raporuna koymayın.
- `dotnet user-secrets list` çalıştırmayın; değerleri düz metin gösterir.
- Mailpit yalnız `.invalid` alıcılı sentetik mesajlar içermelidir.
- MinIO ve Mailpit arayüzlerini proxy ile yerel ağ/internete açmayın.
- Bu rehberdeki normal kapatma komutu volume silmez. `down --volumes` kullanmayın.
- Host çalışırken build almayın; Release DLL kilidi oluşursa Host terminalinde `Ctrl+C` kullanın.

## 2. Sonuç kayıt şablonu

Test başlamadan aşağıdaki bölümü bir not dosyasına kopyalayın:

```text
Test eden:
İşletim sistemi:
.NET SDK:
Docker Desktop / Engine:
Tarayıcı ve sürüm:
Başlangıç zamanı:

F1-T01 Temiz restore/build       : PASS / FAIL
F1-T02 Statik kalite kapıları    : PASS / FAIL
F1-T03 Yerel altyapı             : PASS / FAIL
F1-T04 Migration                 : PASS / FAIL
F1-T05 Otomatik test katmanları  : PASS / FAIL
F1-T06 Web/API smoke             : PASS / FAIL
F1-T07 Responsive/a11y           : PASS / FAIL
F1-T08 Güvenli hata/log          : PASS / FAIL
F1-T09 CI sözleşmesi             : PASS / FAIL

Kritik bulgu sayısı:
Majör bulgu sayısı:
Minör notlar:
Bitiş zamanı:
Genel karar: PASS / FAIL
```

`PASS` için kritik ve majör bulgu sayısı sıfır olmalıdır. Komutun exit code'u sıfır değilse veya beklenen çıktı oluşmadıysa ilgili adımı `FAIL` yazın.

## 3. Ön koşul kontrolü

PowerShell'i repository kökünde açın:

```powershell
Get-Location
git status --short
dotnet --version
docker version
docker compose version
```

Beklenenler:

- çalışma dizini bu repository'nin köküdür;
- `dotnet --version` tam olarak `10.0.201` döndürür;
- Docker istemcisi daemon'a erişebilir;
- Compose v2 kullanılabilir;
- `git status` çıktısında size ait değişiklikler varsa not edilir ve silinmez.

İnternet bağlantısı restore, container image ve ilk Playwright browser kurulumunda gereklidir.

## 4. F1-T01 — Temiz restore ve Release build

Önce çalışan Host terminallerini `Ctrl+C` ile kapatın. Aşağıdaki prova global NuGet paket önbelleğinden bağımsız, repository içindeki ignore edilen geçici bir klasöre kilitli restore yapar. Sonunda yalnız doğrulanmış bu geçici klasörü kaldırır ve normal restore durumunu geri kurar.

```powershell
$repositoryRoot = (Resolve-Path .).Path
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$phase1Packages = [System.IO.Path]::GetFullPath(
    (Join-Path $artifactsRoot ('f1-manual-nuget-' + [Guid]::NewGuid().ToString('N'))))
$requiredPrefix = $artifactsRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar

if (-not $phase1Packages.StartsWith(
        $requiredPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Geçici paket klasörü artifacts dışına çıktı.'
}

New-Item -ItemType Directory -Path $phase1Packages -Force | Out-Null

try {
    dotnet restore .\HospitalManagement.slnx `
        --locked-mode `
        --force `
        --no-cache `
        --packages $phase1Packages `
        --configfile .\NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'İzole kilitli restore başarısız.' }

    dotnet build .\HospitalManagement.slnx `
        --configuration Release `
        --no-restore `
        -p:RestorePackagesPath=$phase1Packages
    if ($LASTEXITCODE -ne 0) { throw 'İzole Release build başarısız.' }
}
finally {
    dotnet build-server shutdown

    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($phase1Packages)
    if (-not $resolvedTemporaryRoot.StartsWith(
            $requiredPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Doğrulanmamış geçici klasör silinmeyecek.'
    }

    if (Test-Path -LiteralPath $resolvedTemporaryRoot) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }

    dotnet restore .\HospitalManagement.slnx `
        --locked-mode `
        --configfile .\NuGet.Config
}
```

Beklenenler:

- izole restore lock dosyasını değiştirmeden tamamlanır;
- build `0 Warning(s)` ve `0 Error(s)` ile tamamlanır;
- `artifacts/f1-manual-nuget-*` klasörü kalmaz;
- son normal locked restore başarılıdır.

`NU1004` alınırsa lock dosyası proje/SDK bağımlılıklarıyla tutarsızdır ve test `FAIL` sayılır. `MSB3021/MSB3027` ile DLL kilidi görülürse açık Host'u kapatıp provayı baştan çalıştırın.

Web.Client projesinde WebAssembly Hot Reload bilinçli olarak kapalıdır; .NET 10 SDK'sının Debug ve Release için farklı örtük paket grafiği üretmesi lock determinismini bozmasın diye normal tarayıcı yenilemesi kullanılır.

## 5. F1-T02 — Statik kalite ve güvenlik kapıları

```powershell
dotnet format .\HospitalManagement.slnx `
    --no-restore `
    --verify-no-changes

dotnet build .\HospitalManagement.slnx `
    --configuration Release `
    --no-restore

powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tools\test-dependency-vulnerabilities.ps1

powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tools\validate-phase0.ps1
```

Her komut exit code `0` ile bitmelidir. Beklenen özet:

- format farkı yok;
- build 0 uyarı/0 hata;
- doğrudan veya transitif bilinen NuGet zafiyeti yok;
- gerekli dokümanlar ve yerel Markdown bağlantıları geçerli.

## 6. F1-T03 — Docker yerel altyapısı

Docker Desktop açıkken:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tools\start-local-infrastructure.ps1
```

İlk çalıştırma uzun sürebilir. Script tamamlandığında şu beş kontrol `[OK]` olmalıdır:

1. `.env.example` içindeki boş örnek secret'lar Compose tarafından reddedildi;
2. yerel Compose modeli geçerli;
3. PostgreSQL smoke sorgusunu kabul etti;
4. MinIO health endpoint'i HTTP 200 döndürdü;
5. Mailpit health endpoint'i HTTP 200 döndürdü ve sentetik `DEMO` e-postayı yakaladı.

Container durumunu ayrıca görün:

```powershell
docker compose --env-file .env --file compose.yaml ps
```

`postgres`, `minio` ve `mailpit` servisleri `healthy` olmalıdır. Yayınlanan portların `127.0.0.1` ile sınırlandığını doğrulayın; `0.0.0.0` kabul edilmez.

Arayüz smoke kontrolü:

- `http://127.0.0.1:9001` MinIO Console'a ulaşır;
- `http://127.0.0.1:8025` Mailpit'i açar ve yalnız sentetik `DEMO` test mesajını gösterir.

Secret değerlerini rapora kopyalamayın.

## 7. F1-T04 — User-secrets ve migration

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tools\configure-local-user-secrets.ps1

powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tools\apply-local-database-foundation.ps1
```

Beklenenler:

- ilk script üç Development secret'ının yapılandırıldığını bildirir fakat değer göstermez;
- migration scripti başarılıdır ve connection string göstermez;
- ilk çalıştırmada migration uygulanır, tekrarında `database is already up to date` benzeri sonuç alınır.

Model ile migration snapshot'ının eşleşmesini doğrulayın:

```powershell
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet ef migrations has-pending-model-changes `
        --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
        --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
        --context DatabaseBootstrapDbContext `
        --configuration Release `
        --no-build
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}
```

Beklenen çıktı: `No changes have been made to the model since the last migration.`

## 8. F1-T05 — Bütün otomatik test katmanları

Playwright Chromium'u ilk kez kuruyorsanız:

```powershell
dotnet build .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj `
    --configuration Release `
    --no-restore

powershell -NoProfile -ExecutionPolicy Bypass `
    -File .\tests\HospitalManagement.EndToEndTests\bin\Release\net10.0\playwright.ps1 `
    install chromium
```

Docker açıkken kategorileri ayrı ayrı çalıştırın:

```powershell
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj `
    --configuration Release --no-build --no-restore

dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj `
    --configuration Release --no-build --no-restore

dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj `
    --configuration Release --no-build --no-restore

dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
    --configuration Release --no-build --no-restore

dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj `
    --configuration Release --no-build --no-restore
```

Faz 1 tabanındaki beklenen sonuçlar:

| Katman | Başarılı | Temel kanıt |
|---|---:|---|
| Unit | 1 | UTC ve concurrency ortak davranışı |
| Architecture | 13 | proje/modül sınırları, deterministik WebAssembly restore ve CI güvenlik sözleşmesi |
| Component | 5 | loading/empty/error/forbidden ve etkileşim |
| Integration | 17 | gerçek PostgreSQL, config, API ve observability |
| End-to-end | 1 | gerçek Kestrel + mobil Chromium yolculuğu |
| Toplam | 37 | başarısız veya atlanan test yok |

Tek koşuda coverage üretin:

```powershell
dotnet test .\HospitalManagement.slnx `
    --configuration Release `
    --no-build `
    --no-restore `
    --collect:'XPlat Code Coverage' `
    --results-directory .\artifacts\f1-manual-test-results

$coverageFiles = @(
    Get-ChildItem .\artifacts\f1-manual-test-results `
        -Recurse `
        -Filter coverage.cobertura.xml)

if ($coverageFiles.Count -ne 5) {
    throw "5 coverage raporu bekleniyordu; bulunan: $($coverageFiles.Count)"
}
```

Test sayısı yalnız bilinçli kod değişikliğiyle artabilir. Beklenenden az, atlanan veya başarısız test `FAIL` sayılır.

## 9. F1-T06 — Web ve API smoke testi

Birinci terminalde Host'u çalıştırın:

```powershell
dotnet run `
    --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
    --configuration Release `
    --no-build `
    --no-restore `
    --urls http://127.0.0.1:5111
```

Terminalde Development yapılandırmasının doğrulandığını ve uygulamanın `http://127.0.0.1:5111` üzerinde dinlediğini görmelisiniz. Secret veya bağlantı dizesi görünmemelidir.

İkinci terminalde:

```powershell
$applicationBaseUri = 'http://127.0.0.1:5111'

$rootPageResponse = Invoke-WebRequest -UseBasicParsing "$applicationBaseUri/"
if ($rootPageResponse.StatusCode -ne 200) { throw 'Ana sayfa HTTP 200 dönmedi.' }

$liveResponse = Invoke-RestMethod "$applicationBaseUri/health/live"
if ($liveResponse.status -ne 'Healthy') { throw 'Liveness Healthy değil.' }

$readyResponse = Invoke-RestMethod "$applicationBaseUri/health/ready"
if ($readyResponse.status -ne 'Healthy') { throw 'Readiness Healthy değil.' }

$manualCorrelationId = 'DEMO-F1-MANUAL-001'
$statusResponse = Invoke-RestMethod `
    "$applicationBaseUri/api/v1/platform/status" `
    -Headers @{ 'X-Correlation-ID' = $manualCorrelationId }

if ($statusResponse.service -ne 'HospitalManagement.Api') { throw 'API service adı hatalı.' }
if ($statusResponse.apiVersion -ne 'v1') { throw 'API sürümü hatalı.' }
if ($statusResponse.dataMode -ne 'DEMO') { throw 'API DEMO modunda değil.' }
if ($statusResponse.correlationId -ne $manualCorrelationId) { throw 'Correlation ID korunmadı.' }

$openApiResponse = Invoke-RestMethod "$applicationBaseUri/openapi/v1.json"
if ($openApiResponse.openapi -notlike '3.*') { throw 'OpenAPI 3 belgesi alınamadı.' }
if ($null -eq $openApiResponse.paths.'/api/v1/platform/status') {
    throw 'Sürümlü status yolu OpenAPI belgesinde yok.'
}

$unsafeCorrelationResponse = Invoke-RestMethod `
    "$applicationBaseUri/api/v1/platform/status" `
    -Headers @{ 'X-Correlation-ID' = 'unsafe correlation / path' }

if ($unsafeCorrelationResponse.correlationId -notmatch '^[a-f0-9]{32}$') {
    throw 'Güvensiz correlation ID sunucu tarafından değiştirilmedi.'
}

'[PASS] Ana sayfa, health, API status, OpenAPI ve correlation kontrolleri geçti.'
```

Tarayıcıda şu sayfaları açın:

- `http://127.0.0.1:5111/`
- `http://127.0.0.1:5111/ui-states`
- `http://127.0.0.1:5111/openapi/v1.json`

Ana sayfada Türkçe dashboard, görünür `DEMO` işaretleri, internet gereksinimi, dört örnek metrik ve modül kartları bulunmalıdır. “Durum bileşenlerini incele” bağlantısı `/ui-states` sayfasına gitmelidir. Browser Console'da error olmamalı, Network panelinde yerel CSS/JS/font/Blazor asset'leri 404 dönmemelidir.

## 10. F1-T07 — Responsive ve erişilebilirlik keşfi

Tarayıcı geliştirici araçlarında önce geniş ekranı, sonra `390 × 844` mobil viewport'u kullanın.

Her iki görünümde kontrol edin:

- yatay kaydırma çubuğu oluşmuyor;
- başlık, navigasyon, `DEMO` etiketi ve ana eylem görünür;
- metinler üst üste binmiyor veya kesilmiyor;
- kartlar mobilde okunabilir tek kolon/uygun akışa dönüşüyor;
- `/ui-states` üzerindeki loading, empty, error ve forbidden içerikleri birbirinden ayırt ediliyor.

Klavye kontrolü:

1. Sayfayı yenileyin ve fare kullanmayın.
2. İlk `Tab` ile “Ana içeriğe geç” bağlantısının görünür olduğunu doğrulayın.
3. `Enter` ile ana içeriğe geçin.
4. Tüm bağlantılarda görünür focus işareti bulunduğunu doğrulayın.
5. “Durum bileşenlerini incele” bağlantısını klavyeyle açın.

Semantik kontrol:

- her sayfada anlaşılır tek ana `h1` vardır;
- navigasyon bağlantıları erişilebilir ad taşır;
- loading/empty/forbidden bileşenleri `role="status"`, error bileşeni `role="alert"` olarak görünür;
- yalnız renkle aktarılan kritik bir anlam yoktur;
- tarayıcı zoom'u `%200` olduğunda içerik kullanılabilir kalır.

## 11. F1-T08 — Güvenli hata ve gözlemlenebilirlik

### Eksik kritik ayar

Gerçek Development secret deposunu silmeden izole config testini çalıştırın:

```powershell
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter 'FullyQualifiedName~StartAsyncWithMissingDatabaseConnectionStopsWithoutExposingSecretValues'
```

Beklenen: tek test geçer. Test kasıtlı olarak `ConnectionStrings:HospitalDatabase` anahtarını kaldırır; başlangıcın bu güvenli anahtar adını içeren hata üretmesini ve diğer sentetik secret canary değerlerini dışarı vermemesini doğrular.

### Problem Details negatif yolu

Host çalışırken ikinci terminalde:

```powershell
$applicationBaseUri = 'http://127.0.0.1:5111'
$missingApiOutput = curl.exe `
    --silent `
    --include `
    "$applicationBaseUri/api/v1/missing"

$missingApiOutput
```

Beklenen:

- HTTP `404`;
- `Content-Type: application/problem+json`;
- gövdede `code: not_found`, güvenli Türkçe başlık ve correlation ID;
- stack trace, yerel dosya yolu, connection string veya secret yok.

### Log/trace kontrolü

Status isteğini bilinen correlation ID ile gönderin:

```powershell
Invoke-RestMethod `
    'http://127.0.0.1:5111/api/v1/platform/status' `
    -Headers @{ 'X-Correlation-ID' = 'DEMO-F1-TRACE-001' }
```

Host terminalinde tamamlanan istek kaydında method, route template, status code, süre, correlation ID ve 32 karakterlik trace ID bulunmalıdır. İstek/cevap gövdesi, klinik içerik, URL query değeri veya secret bulunmamalıdır. Development console exporter nedeniyle trace/metric çıktısı ayrıntılı olabilir; bu tek başına hata değildir.

Host'u `Ctrl+C` ile kapattığınızda hassas değer içermeyen normal kapanış beklenir.

## 12. F1-T09 — CI kalite sözleşmesi

`.github/workflows/ci.yml` dosyasını açın ve şunları kontrol edin:

- tetikleyici `pull_request`, `main` push ve `workflow_dispatch` içerir;
- `pull_request_target` yoktur;
- üst seviye izin yalnız `contents: read` değeridir;
- checkout için credential persistence kapalıdır;
- action sürümleri tam 40 karakterlik commit SHA'sına sabittir;
- locked restore, format, Release build, zafiyet taraması, Playwright Chromium, bütün testler ve coverage adımları vardır;
- workflow herhangi bir repository/environment secret'ı okumaz.

Otomatik sözleşme testini ayrıca çalıştırın:

```powershell
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter 'Roadmap=F01-G11'
```

Beklenen: 2/2 test geçer.

Repository GitHub'a gönderildiğinde `main` ruleset'inde `CI / quality` zorunlu status check yapılmalıdır. Repository ayarı Git içinden uygulanamadığı için yerel Faz 1 testinde yalnız [`docs/development/ci.md`](docs/development/ci.md) yönergesinin varlığı kabul edilir.

## 13. Kapatma ve temizlik

1. Host çalışıyorsa kendi terminalinde `Ctrl+C` kullanın.
2. Container'ları volume'ları koruyarak durdurun:

```powershell
docker compose --env-file .env --file compose.yaml down
```

3. İsterseniz yalnız üretilmiş test raporlarını daha sonra kaldırabilirsiniz; test kabulü için silmek zorunlu değildir.
4. `.env` ve user-secrets dosyalarını rapora eklemeyin veya commit etmeyin.

## 14. Arıza giderme

| Belirti | Olası neden | Yapılacak işlem |
|---|---|---|
| `NU1004` | Lock dosyası bağımlılık grafiğiyle tutarsız | Testi `FAIL` yazın; lock dosyasını sessizce atlamayın |
| `MSB3021` / `MSB3027` | Host Release DLL'lerini kilitliyor | Host terminalinde `Ctrl+C`, sonra `dotnet build-server shutdown` |
| Docker daemon unavailable | Docker Desktop kapalı | Docker Desktop'ı başlatıp `docker info` kontrol edin |
| Container unhealthy | Port çakışması veya image/build sorunu | `docker compose --env-file .env --file compose.yaml ps` ve `logs` inceleyin; secret'ı rapora kopyalamayın |
| Readiness `Unhealthy` | PostgreSQL kapalı veya migration/config sorunu | F1-T03 ve F1-T04'ü yeniden çalıştırın |
| `playwright.ps1` bulunamadı | E2E proje build edilmedi | E2E projesini Release build edin |
| Browser executable missing | Chromium kurulmadı | Playwright `install chromium` komutunu çalıştırın |
| Static asset warning/404 | Yanlış environment veya eski build | Host'u kapatın, locked restore + Release build alın, Development ile yeniden başlatın |
| HTTPS sertifika uyarısı | Launch profile HTTPS adresi kullanıldı | Rehberdeki açık `http://127.0.0.1:5111` URL'sini kullanın |
| PowerShell execution policy | Yerel script çalışması engellendi | Rehberdeki `-ExecutionPolicy Bypass -File` biçimini kullanın |

## 15. Faz 1 kabul kararı

Aşağıdakilerin tamamını işaretleyebiliyorsanız Faz 1 manuel testi `PASS` olur:

- [ ] İzole locked restore ve Release build 0 uyarı/0 hatayla geçti.
- [ ] Format, belge, mimari ve bağımlılık güvenlik kapıları geçti.
- [ ] PostgreSQL, MinIO ve Mailpit healthy; sentetik e-posta yakalandı.
- [ ] İlk migration uygulandı/veritabanı güncel; pending model change yok.
- [ ] Beş test katmanında toplam 37/37 test geçti ve 5 coverage raporu oluştu.
- [ ] Ana sayfa, health, API status ve OpenAPI HTTP düzeyinde doğrulandı.
- [ ] Masaüstü ve 390 × 844 mobil görünümde yatay taşma/console error/asset 404 yok.
- [ ] Klavye, skip link, focus ve status/alert semantiği doğrulandı.
- [ ] Eksik kritik ayar güvenli biçimde reddedildi; secret/stack trace sızıntısı yok.
- [ ] CI workflow fork güvenliği ve kalite adımları doğrulandı.
- [ ] Kritik bulgu yok.
- [ ] Majör bulgu yok.

Bir madde boşsa genel sonucu `FAIL` bırakın; sorunu, tekrar komutunu ve gözlenen çıktıyı kaydedin. Faz 2 özelliği ekleyerek Faz 1 arızasını dolanmayın.
