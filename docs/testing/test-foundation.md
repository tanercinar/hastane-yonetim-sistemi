# Test omurgası

Bu belge Faz 1'de çalışan test katmanlarının amacını ve ortak kurulumlarını tanımlar. Testler yalnızca sentetik `DEMO` veri kullanır; gerçek hasta verisi, kurum bilgisi veya gerçek secret kullanılmaz.

## Katmanlar

| Katman | Proje | Sistem sınırı | Yerel bağımlılık |
|---|---|---|---|
| Unit | `HospitalManagement.UnitTests` | Domain/building-block davranışı | Yok; süreç içi sağlayıcı kullanılabilir |
| Architecture | `HospitalManagement.ArchitectureTests` | Modül ve katman bağımlılıkları | Yok |
| Component | `HospitalManagement.ComponentTests` | Razor bileşen çıktısı ve etkileşimi | bUnit, gerçek tarayıcı yok |
| Integration | `HospitalManagement.IntegrationTests` | ASP.NET Core sözleşmeleri ve PostgreSQL | Docker; PostgreSQL Testcontainers |
| End-to-end | `HospitalManagement.EndToEndTests` | Gerçek Kestrel + Chromium kullanıcı akışı | Playwright Chromium |

## Ortak fixture ilkeleri

- PostgreSQL integration testleri `PostgreSqlTestDatabase` ile her koşuda benzersiz `hms_it_*` veritabanı oluşturur ve container'ı test sonunda kapatır.
- API contract testleri `ApiWebApplicationFactory` ile in-memory `DEMO` yapılandırma kullanır. Gerçek servis endpoint'i veya repository secret'ı gerekmez.
- E2E testi `BrowserWebApplicationFactory` ile Kestrel'i rastgele loopback portunda başlatır. Veritabanı bağlantı dizesi yalnız startup sözleşmesini karşılayan sentetik bir değerdir; smoke akışı veritabanına bağlanmaz.
- Component testleri bUnit üzerinden erişilebilir rol, metin ve kullanıcı etkileşimini doğrular. HTML metin eşleştirmesi yerine mümkün olduğunda rol/etiket kullanılır.
- Testler birbirinin verisine veya çalışma sırasına güvenmez.

## Komutlar

```powershell
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj -c Release
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj -c Release
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj -c Release
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj -c Release

dotnet build .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj -c Release
pwsh .\tests\HospitalManagement.EndToEndTests\bin\Release\net10.0\playwright.ps1 install chromium
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj -c Release --no-build
```

İlk dört komut için Playwright kurulumu gerekmez. Integration testi Docker kullanır. Ayrıntılı insan doğrulaması kökteki `F1_Test.md` belgesindedir.
