# Veritabanı temeli ve migration çalışma sözleşmesi

Bu belge F01-G06 ile kurulan PostgreSQL/EF Core temelini açıklar. Sistem yalnız sentetik `DEMO` veri içindir. Bağlantı bilgileri kaynak koda, migration'a veya komut satırı örneğine yazılmaz.

## Sabitlenen bileşenler

| Bileşen | Sürüm | Kullanım |
|---|---:|---|
| PostgreSQL | 18.6 | Yerel Compose ve integration Testcontainer |
| EF Core / dotnet-ef | 10.0.11 | Mapping, change tracking ve migration |
| Npgsql EF provider / Npgsql | 10.0.3 | PostgreSQL sağlayıcısı ve sürücüsü |
| Testcontainers.PostgreSql | 4.14.0 | Test başına geçici gerçek PostgreSQL |

Sürümler central package management ve yerel .NET tool manifest ile sabittir. Major yükseltme ayrı ADR/migration görevidir.

## Sahiplik modeli

`DatabaseBootstrapDbContext` bir uygulama/genel iş DbContext'i değildir. Yalnız platform migration geçmişine ve ilk şema kurulumuna sahiptir; hiçbir modül entity'si içermez. Uygulama başlangıcında migration çalıştırmaz.

Her iş modülü kendi `Infrastructure` alanında kendi DbContext, mapping, migration ve aşağıdaki şemasına sahip olacaktır. Bir modül başka şemadaki tabloya, DbContext'e veya entity'ye doğrudan erişemez.

`OrganizationDbContext` bu sözleşmenin ilk iş modülü uygulamasıdır. Hastane, şube, bölüm, uzmanlık, klinik personel profili ve bölüm atamalarına sahiptir; ayrıntılar [organizasyon modeli](organization-model.md) belgesindedir. `IdentityAccessDbContext`, ASP.NET Core Identity kalıtımı nedeniyle ortak `ModuleDbContext` tabanını kullanamaz; buna karşın aynı UTC, optimistic concurrency, şema sahipliği ve ayrı migration history kurallarını kendi sınırında uygular. Ayrıntılar [kimlik yaşam döngüsü](identity-lifecycle.md) belgesindedir.

| Modül | PostgreSQL şeması |
|---|---|
| Platform bootstrap | `platform` |
| AuditPrivacy | `audit_privacy` |
| ClinicalRecords | `clinical_records` |
| Diagnostics | `diagnostics` |
| Emergency | `emergency` |
| IdentityAccess | `identity_access` |
| Inpatient | `inpatient` |
| Interoperability | `interoperability` |
| Inventory | `inventory` |
| Notifications | `notifications` |
| Organization | `organization` |
| Patients | `patients` |
| Pharmacy | `pharmacy` |
| Reporting | `reporting` |
| Scheduling | `scheduling` |
| SpecialtyCare | `specialty_care` |
| SurgeryCriticalCare | `surgery_critical_care` |

Yeni modül DbContext'i `ModuleDbContext` tabanını kullanır, kendi `HasDefaultSchema` değerini verir ve kendine ait şemada `__ef_migrations_history` tablosu yapılandırır. Tarihsel migration dosyaları değişebilen şema kataloğunu çağırmaz; oluşturulduğu andaki literal şema adını taşır.

## Yerel migration

Önkoşullar:

1. Docker Desktop çalışıyor olmalı.
2. `tools/start-local-infrastructure.ps1` ile yerel PostgreSQL başlatılmış olmalı.
3. `tools/configure-local-user-secrets.ps1` ile Development user-secrets hazırlanmış olmalı.

Repo kökünde çalıştır:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
```

Script sırasıyla platform, Organization, IdentityAccess, AuditPrivacy, Patients, Scheduling ve Notifications migration'larını uygular. Aşağıdaki ilk üç komut kalıbı diğer modül DbContext'leri için de aynı şekilde kullanılır:

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update `
  --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context DatabaseBootstrapDbContext `
  --configuration Release

dotnet ef database update `
  --project .\src\Modules\Organization\HospitalManagement.Modules.Organization.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context OrganizationDbContext `
  --configuration Release

dotnet ef database update `
  --project .\src\Modules\IdentityAccess\HospitalManagement.Modules.IdentityAccess.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context IdentityAccessDbContext `
  --configuration Release
```

Yeni bootstrap migration yalnız platform temelinin gerçekten değiştiği görevde oluşturulur:

```powershell
dotnet ef migrations add MigrationAdi `
  --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context DatabaseBootstrapDbContext `
  --output-dir Database\Migrations
```

İş modülü migration'ı kendi proje ve DbContext'iyle üretilir. Organization örneği:

```powershell
dotnet ef migrations add MigrationAdi `
  --project .\src\Modules\Organization\HospitalManagement.Modules.Organization.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context OrganizationDbContext `
  --output-dir Infrastructure\Persistence\Migrations
```

IdentityAccess migration'ı aynı kalıpla kendi proje/context ve `Infrastructure\Persistence\Migrations` çıkışını kullanır. Kimlik migration'ı parola, ham işlem kodu, e-posta veya secret sabiti içeremez.

Üretilen migration ve SQL diff'i incelemeden görevi tamamlama. Uygulanmış migration silinmez veya yeniden yazılmaz.

## Production-benzeri uygulama ve geri alma

- Host açılışında `Database.Migrate`/`MigrateAsync` çağrılmaz.
- Dağıtım kimliği ile çalışma zamanı uygulama kimliği ayrılır.
- Production-benzeri ortam için idempotent SQL script veya migration bundle CI'da üretilir, gözden geçirilir ve ayrı dağıtım adımında uygulanır.
- Migration'lar ileri yönlüdür. Hata halinde veri kaybettiren `Down` veya şema düşürme yerine düzeltici migration hazırlanır.
- Her şema değişikliğinde veri kaybı etkisi, rollout ve geri dönüş/ileri düzeltme notu bulunur.

Örnek inceleme scripti:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
dotnet ef migrations script --idempotent `
  --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context DatabaseBootstrapDbContext `
  --output .\artifacts\database-foundation.sql
```

Deployment connection string'i script veya bundle içine gömülmez; dağıtım secret store'undan verilir.

## UTC ve optimistic concurrency kuralları

- Kalıcı zaman noktaları `DateTime` `Kind=Utc` veya `DateTimeOffset` `Offset=00:00` olmalıdır.
- Bu alanlar PostgreSQL `timestamp with time zone` (`timestamptz`) olarak eşlenir. `timestamptz` saat dilimi adını saklamaz; UTC zaman noktasını temsil eder.
- Local/Unspecified `DateTime` ve sıfır olmayan `DateTimeOffset`, SQL çalışmadan önce reddedilir. Kullanıcı gösterimi istemci sınırında `Europe/Istanbul` dönüşümüyle yapılır.
- Kritik aggregate `IHasConcurrencyVersion` uygular. Yeni kaydın sürümü `1`; başarılı her tracked update sürümü bir artırır. Eski sürümle update/delete `DbUpdateConcurrencyException` üretir.
- API daha sonraki görevlerde bu sürümü ETag/row-version sözleşmesine taşır; conflict sessizce retry/overwrite edilmez.
- `ExecuteUpdate`/`ExecuteDelete` change tracker ve ortak sürüm artırma davranışını atlar. Kritik aggregate üzerinde ancak sürüm koşulu + etkilenen satır sayısı açıkça denetlenirse kullanılabilir.
- Optimistic token tek başına randevu çakışması, negatif stok veya benzersizlik garantisi değildir; ilgili modül ayrıca PostgreSQL constraint/index ve concurrency testi ekler.

## Integration test izolasyonu

F01-G06 testleri EF InMemory veya ortak Compose veritabanını kullanmaz. Her test:

1. `postgres:18.6` imajından ayrı Testcontainer başlatır.
2. `hms_it_<GUID>` adlı benzersiz boş veritabanı oluşturur.
3. Hedef davranışı gerçek Npgsql/EF Core ile sınar.
4. Test bitiminde container ve veritabanını kaldırır; başarısız koşular için Ryuk temizliği devrededir.

Çalıştırma:

```powershell
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
  -c Release `
  --filter "Roadmap=F01-G06"
```

Docker erişilemiyorsa test atlanmaz; açıkça başarısız olur.

## Resmî teknik kaynaklar

- [EF Core migration uygulama stratejileri](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
- [EF Core optimistic concurrency](https://learn.microsoft.com/ef/core/saving/concurrency)
- [Npgsql timestamp/UTC davranışı](https://www.npgsql.org/efcore/release-notes/6.0.html#timestamp-rationalization-and-improvements)
- [Testcontainers PostgreSQL modülü](https://dotnet.testcontainers.org/modules/postgres/)
- [PostgreSQL 18.6 sürüm notları](https://www.postgresql.org/docs/release/18.6/)
