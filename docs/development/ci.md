# CI kalite hattı

`.github/workflows/ci.yml`, `main` push'larında, pull request'lerde ve elle tetiklemede tek bir `CI / quality` durumu üretir. Bu iş kilitli restore, format, Release build, NuGet güvenlik taraması, tüm test katmanları, Playwright Chromium ve coverage raporunu aynı başarısızlık kapısında çalıştırır.

## Fork güvenliği

- Workflow yalnız `pull_request` kullanır; ayrıcalıklı `pull_request_target` kullanılmaz.
- `GITHUB_TOKEN` izni yalnız `contents: read` olarak tanımlıdır.
- Checkout credential'ı çalışma dizininde tutulmaz.
- Repository veya environment secret'ı okunmaz. Test yapılandırmaları kaynak kod içindeki sentetik `DEMO` değerleridir.
- Üçüncü taraf action'lar yayımlanmış sürümlerinin değişmez 40 karakterlik commit SHA'sına sabitlenmiştir.
- Workflow, pull request başlığı/branch adı gibi saldırgan kontrollü metni shell komutuna yerleştirmez.

## Coverage ve güvenlik çıktıları

`XPlat Code Coverage` ile üretilen Cobertura dosyaları ve test sonuçları `test-and-coverage-reports` artifact'ı olarak 14 gün saklanır. `tools/test-dependency-vulnerabilities.ps1`, bilinen zafiyeti bulunan doğrudan veya transitif herhangi bir NuGet paketi raporlanırsa işi başarısız yapar. Restore aşamasındaki NuGet audit de `moderate` ve üzeri bulguları warning-as-error ilkesiyle durdurur.

## Birleşme koruması

GitHub repository oluşturulduktan sonra `Settings > Rules > Rulesets` altında `main` için bir branch ruleset ekle:

1. `Require a pull request before merging` kuralını aç.
2. `Require status checks to pass` kuralını aç.
3. Zorunlu kontrol olarak `CI / quality` seç.
4. `Require branches to be up to date before merging` seçeneğini aç.
5. Bypass listesini boş bırak veya yalnız repository sahibine acil durum yetkisi ver.

Bu repository ayarı Git içindeki bir dosyayla zorlanamaz. Ruleset etkin olduğunda test, format, build, güvenlik veya Playwright adımlarından herhangi birinin başarısızlığı pull request birleşmesini engeller.
