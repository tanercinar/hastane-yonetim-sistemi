# Hastane Yönetim Sistemi

Eğitim, staj ve portföy amacıyla geliştirilen kapsamlı bir hastane yönetim sistemi temelidir. Web istemcisi önce gelir; aynı API ve sözleşmeler daha sonraki fazlarda Windows masaüstü ve Android istemcileri tarafından da kullanılacaktır.

> Bu proje yalnız sentetik `DEMO` veri kullanır. Sertifikalı bir HBYS, klinik karar destek ürünü veya gerçek sağlık hizmeti sistemi değildir. Uygulamaya gömülü yapay zekâ yoktur; Antigravity, Codex, Claude ve Cursor yalnız geliştirme araçlarıdır. Gerçek hasta verisi, kurum kimliği veya gerçek entegrasyon credential'ı kullanmayın.

## Mevcut durum

Faz 0–3 tamamlanmıştır. Repository şu anda şunları sağlar:

- .NET 10 tabanlı modüler monolit çözüm iskeleti;
- Blazor WebAssembly istemcili responsive web kabuğu ve Türkçe UI temeli;
- PostgreSQL 18, MinIO ve `MOCK` Mailpit yerel altyapısı;
- güvenli yapılandırma/user-secrets akışı ve EF Core migration temeli;
- hastane, şube, hiyerarşik bölüm, uzmanlık ve klinik personel ataması için Organization domain modeli;
- ASP.NET Core Identity ile hasta self-registration, personel daveti, doğrulama, güvenli cookie oturumu, kilitleme ve parola sıfırlama akışları;
- izin, kaynak kapsamı ve bakım ilişkisi tabanlı API yetkilendirmesi ile salt-eklenir denetim izi;
- hasta ana kaydı, güvenli hasta arama/kayıt ekranı ve maskelenmiş hassas alanlar;
- doktor çalışma takvimi, uygun slot üretimi, randevu alma/iptal, check-in, no-show ve sıralı kuyruk akışları;
- idempotent outbox-benzeri uygulama içi/`MOCK` e-posta bildirimleri ve yetkili SignalR güncellemeleri;
- sürümlü API, OpenAPI, Problem Details, correlation ID ve health endpoint'leri;
- yapılandırılmış log ve OpenTelemetry temeli;
- unit, architecture, component, integration ve Playwright E2E test omurgası;
- secretsız ve fork güvenli GitHub Actions kalite hattı.

Elektronik sağlık kaydı ve diğer klinik modüller Faz 4 ve sonrasında eklenecektir. Ayrıntılı sıra için [`ROADMAP.md`](ROADMAP.md), Faz 1'i elle kabul etmek için [`F1_Test.md`](F1_Test.md), Faz 3 akışını doğrulamak için [`docs/development/phase3-first-product-gate-validation.md`](docs/development/phase3-first-product-gate-validation.md) kullanılır.

## Ajan destekli geliştirme

Antigravity workspace kuralı `.agents/rules/project-context.md`, tek-roadmap-görevi workflow'u `.agents/workflows/roadmap-task.md` altındadır. İlk kurulum, kopyalanabilir ana prompt ve Antigravity sonrasında Codex'e verilecek bağımsız kontrol promptu için [`docs/agents/antigravity.md`](docs/agents/antigravity.md) belgesini izleyin. `ROADMAP.md` tek kalıcı görev/status kaynağıdır; araçların geçici plan ve walkthrough Artifact'ları repoda ikinci bir görev listesine dönüştürülmez.

## Ön koşullar

- Windows 10/11;
- Git;
- `global.json` ile sabitlenen .NET SDK `10.0.201`;
- çalışır Docker Desktop ve Docker Compose v2;
- Windows PowerShell 5.1 veya PowerShell 7+;
- ilk restore, container image ve Playwright Chromium kurulumu için internet.

Kurulumları doğrula:

```powershell
dotnet --version
docker version
docker compose version
```

`dotnet --version` çıktısı `10.0.201` olmalıdır. Docker komutları daemon erişim hatası verirse Docker Desktop'ı başlatın.

## İlk kurulum

Repository henüz bilgisayarınızda değilse:

```powershell
git clone <repository-url>
cd Staj
```

Repository kökünde aşağıdaki sırayı izleyin.

### 1. Kilitli paketleri yükle ve derle

```powershell
dotnet restore .\HospitalManagement.slnx --locked-mode --configfile .\NuGet.Config
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
```

Build sonucu `0 Warning(s)` ve `0 Error(s)` olmalıdır. Bir Host süreci dosyaları kilitliyorsa o terminalde `Ctrl+C` ile uygulamayı kapatıp tekrar deneyin.

WebAssembly Hot Reload, Debug/Release arasında farklı örtük paket grafiği üreterek lock dosyası determinismini bozduğu için kapalıdır. UI değişikliğinden sonra normal tarayıcı yenilemesi kullanın.

### 2. Yerel altyapıyı başlat

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
```

İlk çalıştırma ignore edilen `.env` dosyasına rastgele yerel credential'lar üretir; bunları ekrana yazmaz. PostgreSQL, MinIO ve Mailpit sağlıklı olana kadar bekler ve smoke test yapar.

### 3. Development secret'larını yapılandır

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
```

Komut yalnız üç yerel anahtarı repo dışındaki Development user-secrets deposuna aktarır ve değerleri göstermez. Doğrulamak için `dotnet user-secrets list` kullanmayın; bu komut değerleri düz metin yazdırır.

### 4. Veritabanı temelini uygula

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
```

İlk koşuda platform, Organization, IdentityAccess, AuditPrivacy, Patients, Scheduling ve Notifications migration'ları uygulanır; sonraki koşularda veritabanının güncel olduğu bildirilir.

### 5. Uygulamayı çalıştır

Güvenli kimlik cookie/antiforgery akışları HTTPS ister. Geliştirme sertifikasını bir kez güvenilir yapıp HTTPS launch profile ile çalıştırın:

```powershell
dotnet dev-certs https --trust
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

Tarayıcıda kontrol edin:

| Adres | Beklenen sonuç |
|---|---|
| `https://localhost:7111/` | Türkçe responsive dashboard ve görünür `DEMO` etiketi |
| `https://localhost:7111/ui-states` | Loading, empty, error ve forbidden örnekleri |
| `https://localhost:7111/account/register` | DEMO hasta hesabı kayıt başlangıcı |
| `https://localhost:7111/health/live` | Minimal `Healthy` cevabı |
| `https://localhost:7111/health/ready` | PostgreSQL kontrolü dahil `Healthy` cevabı |
| `https://localhost:7111/api/v1/platform/status` | `v1` ve `DEMO` API durum sözleşmesi |
| `https://localhost:7111/openapi/v1.json` | OpenAPI 3 belgesi |

Uygulamayı kapatmak için Host terminalinde `Ctrl+C` kullanın.

## Testler

Önce Release build alın. Playwright Chromium'u ilk E2E koşusundan önce bir kez kurun:

```powershell
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\HospitalManagement.EndToEndTests\bin\Release\net10.0\playwright.ps1 install chromium
```

Docker Desktop açıkken tüm testleri çalıştırın:

```powershell
dotnet test .\HospitalManagement.slnx --configuration Release --no-build --no-restore
```

Faz 3 kapı tabanı `139` başarılı testtir: unit `47`, component `22`, architecture `13`, gerçek PostgreSQL integration `54` ve Playwright E2E `3`. Başarısız veya atlanan test kabul edilmez. Ayrıca kalite kapıları:

```powershell
dotnet format .\HospitalManagement.slnx --no-restore --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-dependency-vulnerabilities.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Faz 1 manuel kabulü için [`F1_Test.md`](F1_Test.md), kimlik akışlarının manuel/otomatik doğrulaması için [`docs/development/identity-lifecycle.md`](docs/development/identity-lifecycle.md) dosyasını izleyin. Test katmanlarının sorumlulukları [`docs/testing/test-foundation.md`](docs/testing/test-foundation.md) ve [`docs/testing/test-strategy.md`](docs/testing/test-strategy.md) içinde açıklanır.

## Mimari ve güvenlik sınırları

- Modüller başka modülün `Infrastructure`, `DbContext` veya tablolarına doğrudan erişmez.
- Yetki API sınırında `Permission + Resource Scope + Care Relationship` ile uygulanır; UI gizleme yetkilendirme değildir.
- Final/imzalı klinik kayıtlar sessizce güncellenmez veya silinmez.
- `MOCK` entegrasyonlar açıkça etiketlenir ve gerçek endpoint'e çağrı yapmaz.
- Klinik içerik, token ve secret loga, telemetry'ye, URL'ye veya exception mesajına yazılmaz.
- Finans, satın alma, bordro ve tam İK kapsam dışıdır.

Başlangıç kaynakları:

- [`docs/architecture/solution-structure.md`](docs/architecture/solution-structure.md)
- [`docs/adr/README.md`](docs/adr/README.md)
- [`docs/security/authorization-matrix.md`](docs/security/authorization-matrix.md)
- [`docs/privacy/data-classification.md`](docs/privacy/data-classification.md)
- [`docs/development/local-infrastructure.md`](docs/development/local-infrastructure.md)
- [`docs/development/ci.md`](docs/development/ci.md)

## Yerel altyapıyı durdurma

Verileri koruyarak container'ları durdurun:

```powershell
docker compose --env-file .env --file compose.yaml down
```

`down --volumes` yerel PostgreSQL/MinIO verisini kalıcı olarak siler; normal kapatmada kullanmayın. Bu projede her koşulda yalnız sentetik `DEMO` veri tutulmalıdır.
