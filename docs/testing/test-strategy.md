# Test Stratejisi

## Amaç ve kalite riski

Testlerin amacı yalnız kod kapsamı değil; klinik kayıt bütünlüğü, rol/kaynak yetkisi, kritik yarış koşulları, veri minimizasyonu ve çoklu istemci sözleşmesini kanıtlamaktır. Test piramidi kullanılır; az sayıdaki geniş E2E senaryosu, daha çok integration/component ve çok sayıda hızlı domain unit testiyle desteklenir.

## Test seviyeleri

| Seviye | Araç/yaklaşım | Kanıtladığı | Kanıtlamadığı |
|---|---|---|---|
| Domain unit | xUnit | Durum geçişi, invariant, hesap, permission karar fonksiyonu | EF mapping, gerçek HTTP/DB |
| Application unit | xUnit + açık fake | Handler/service orkestrasyonu ve hata yolları | Gerçek provider davranışı |
| Architecture | NetArchTest/ArchUnitNET benzeri | Modül, katman ve UI bağımlılık kuralları | Runtime davranışı |
| Component | bUnit | Razor render, validation, loading/empty/error/forbidden, erişilebilir markup | Browser/JS/CSS bütünü |
| API integration | `WebApplicationFactory` + PostgreSQL Testcontainers | Routing, auth, validation, EF mapping, transaction, Problem Details | Gerçek browser |
| Contract | OpenAPI snapshot/semantic diff + mock contract | DTO/status/error ve dış adapter sözleşmesi | Görsel davranış |
| E2E | Playwright | Gerçek browser ve rol geçişli ana kullanıcı yolculuğu | Her kombinasyon/edge case |
| Performans/concurrency | k6/NBomber veya seçilen araç + integration harness | Latency bütçesi, yarış ve veri bütünlüğü | Üretim kapasite garantisi |
| Güvenlik | Otomatik negatif test, SAST/DAST/dependency/secret scan | Bilinen saldırı sınıfları ve regresyon | Sertifikasyon veya sıfır risk |
| Manuel keşif | Rol, erişilebilirlik ve kötüye kullanım charter'ı | Kullanılabilirlik ve beklenmeyen kombinasyon | Tekrarlanabilir otomatik regresyon |

EF Core `InMemory` provider kritik integration kanıtı olarak kullanılmaz; PostgreSQL semantiği Testcontainers ile doğrulanır. Mock yalnız kontrol ettiğimiz sınırın dış tarafında kullanılır; test edilen domain davranışını mock'layarak test geçirme yapılmaz.

## Test proje düzeni

```text
tests/
├─ UnitTests/          # Modül klasörleri, domain/application
├─ IntegrationTests/   # API + PostgreSQL + infrastructure
├─ ArchitectureTests/  # Bağımlılık ve naming kuralları
├─ ComponentTests/     # bUnit ve erişilebilir markup
├─ ContractTests/      # OpenAPI ve mock adapter sözleşmeleri
├─ EndToEndTests/      # Playwright rol yolculukları
├─ PerformanceTests/   # Tekrarlanabilir yük/concurrency senaryoları
└─ SecurityTests/      # Yetki matrisi, sızıntı canary ve abuse cases
```

Faz 1 gerçek solution oluştururken isimler küçük ölçüde değişebilir; test sorumlulukları birleştirilmez.

## İsimlendirme ve izlenebilirlik

Test adları davranışı ve koşulu açıklar:

```csharp
BookAppointment_WhenSlotWasTakenConcurrently_ReturnsConflict()
ViewPrescription_WhenPatientIsNotOwner_ReturnsNotFoundOrForbidden()
SignClinicalNote_WhenAlreadySigned_RequiresCorrection()
```

- `Given_When_Then` veya `Method_When_Expected` tutarlı seçilir.
- Kritik test comment/trait ile `US-*`, `AC-*`, `TM-*` veya roadmap görev kimliğine bağlanır.
- Bug fix önce başarısız regresyon testi üretir; sonra düzeltme yapılır.
- Test yalnız implementation ayrıntısını değil gözlemlenebilir davranışı doğrular.

## Fixture ve sentetik veri

- Tüm fixture'lar ADR-0006 ve `data-classification.md` ile uyumlu `DEMO-*` veridir.
- Her test kendi verisini kurar; test sırası veya ortak mutable seed'e dayanmaz.
- Saat, UUID, bildirim ve dış servis davranışı interface/test double ile kontrol edilebilir; production kodunda global static saat kullanılmaz.
- Integration test başına izole schema/database veya transaction/reset stratejisi kullanılır; paralellik kararı ölçülür.
- E2E için rol başına storage state üretilebilir; test birbirinin state'ini değiştirmez.
- Dosya fixture'ları küçük, lisanslı/projede üretilmiş, metadata'sız ve zararlı dosya testleri açıkça karantinadır.
- Rastgele/property test seed'i başarısızlık çıktısında yazılır ve tekrar üretilebilir.

## Zorunlu davranış sınıfları

Her işlev için uygun olanlar test edilir:

1. Mutlu yol
2. Alan doğrulama ve sınır değerleri
3. Authentication yok / hesap kilitli / oturum iptal
4. Permission var-yok ve yanlış resource scope
5. Başka hastanın/başka bölümün ID'si (IDOR)
6. Geçersiz durum geçişi
7. Eski concurrency token ve aynı kaynağa yarış
8. Aynı idempotency anahtarının tekrar gönderilmesi
9. Downstream timeout/hata/bozuk cevap
10. Hassas verinin response, log, audit ve telemetry'ye sızmaması
11. UI loading, empty, validation, error, forbidden, success
12. Klavye/focus/label ve responsive görünüm

## Yetkilendirme testi

`docs/security/authorization-matrix.md` makinece okunabilir theory/case verisine dönüştürülür. Her permission için:

- En az bir doğru rol + doğru kapsam allow testi
- Aynı rol + yanlış kapsam deny testi
- Yanlış rol + doğru kaynak deny testi
- İstemcinin `Role`, `PatientId`, `DepartmentId` veya owner alanı enjekte etme testi
- Gerekli ise MFA/step-up yok testi
- Deny halinde body'de hassas varlık bilgisi ve uygunsuz var/yok oracle'ı olmaması
- Audit davranışının doğrulanması

UI authorization testi API negatif testinin yerine geçmez.

## Klinik bütünlük ve concurrency

- İzin verilen/yasak durum geçişlerinin tamamı domain unit testine bağlanır.
- İmzalı/final kaydın silent update/hard delete denemesi reddedilir.
- Correction/addendum eski sürüm referansı, gerekçe, aktör ve UTC taşır.
- Randevu slotu, yatak zaman aralığı, stok düşme, barkod ve finalizasyon eşzamanlı integration testine sahiptir.
- Uygulama kontrolü ile DB constraint ayrı ayrı, mümkünse farklı testlerle kanıtlanır.
- Tekrarlanan mesaj/HTTP isteği çift bildirim, çift teslim veya çift stok hareketi üretmez.

## Contract ve entegrasyon testleri

- OpenAPI breaking change semantic diff ile incelenir; snapshot körlemesine güncellenmez.
- Problem Details tür/kod/status/alan sözleşmesi testlidir; stack trace veya klinik içerik dönmez.
- Mock dış sistem contract'ı başarı, timeout, gecikme, duplicate, bozuk payload ve 4xx/5xx durumlarını kapsar.
- Retry yalnız idempotent/güvenli işlemde uygulanır ve tekrar yan etki üretmez.
- Mock base URL'nin izinli reserved host dışına çıkması startup/config testinde reddedilir.

## UI, erişilebilirlik ve görsel doğrulama

- bUnit bileşen testleri semantic role/label, validation summary ve permission durumunu doğrular.
- Playwright ana akışları Chromium'da başlar; yayın öncesi en az Chromium + Firefox/WebKit uygunluğu değerlendirilir.
- Viewport seti en az mobil, tablet ve geniş masaüstünü kapsar.
- Kritik akış yalnız klavyeyle tamamlanır; focus görünür ve hata özetine taşınır.
- Otomatik axe benzeri tarama manuel klavye/screen-reader incelemesinin yerine geçmez.
- Faz kapısında yüksek değerli sayfalar için ekran görüntüsü karşılaştırması eklenebilir; dinamik klinik veri maskelenir/sabitlenir.

## Performans bütçeleri

Faz 1'de ölçüm ortamı, Faz 13'te kesin demo hedefleri kaydedilir. Başlangıç hedefleri taahhüt değil alarm bütçesidir:

- Basit API okuma/yazma p95: yerel kontrollü demo yükünde 500 ms altında
- Hasta arama p95: sayfalı ve indeksli sorguda 1 sn altında
- Dashboard güncelleme görünürlüğü: normal demo yükünde birkaç saniye içinde
- Kritik yazmada veri bütünlüğü: yükten bağımsız sıfır çift slot/yatak ve sıfır negatif stok

Test sonucu donanım, veri hacmi, commit ve konfigürasyonla raporlanır; tek sayı üretim kapasite garantisi olarak sunulmaz.

## Güvenlik ve gizlilik testleri

- OWASP ASVS 5.0 L2 kontrol listesi Faz 13'te kanıt bağlantılarıyla değerlendirilir.
- Dependency/CVE, secret ve license scan CI'da çalışır.
- XSS, CSRF, SQL injection, mass assignment, SSRF, path traversal, file upload, open redirect, CSV injection ve SignalR group bypass otomatik/manuel test edilir.
- Log/trace/export canary değeri (`DEMO-SENSITIVE-CANARY-*`) işlemden sonra log/telemetry artefact'larında aranır; bulunması testi kırar.
- Test hiçbir zaman gerçek zararlı endpoint'e, dış kuruma veya gerçek kişiye mesaj göndermez.

## Test kategorileri ve çalıştırma sıklığı

| Kategori | Yerel görev | Pull request | Faz kapısı/gecelik |
|---|---:|---:|---:|
| Unit + architecture | Hedef + etkilenen | Tümü | Tümü |
| Component | Etkilenen | Tümü | Tümü |
| Integration | Etkilenen modül | Tümü | Tümü |
| Contract | Sözleşme değişiminde | Tümü | Tümü |
| E2E smoke | Ana akış etkilenirse | Smoke | Faz ana senaryoları |
| Security static/dependency | Paket/config etkilenirse | Tümü | Derin tarama |
| Performance/concurrency | Kritik yazma/sorgu | Hafif concurrency | Tam senaryo |
| Accessibility/manual | UI görevi | Otomatik | Manuel charter |

## Komut sözleşmesi

Faz 1'de gerçek solution kurulduktan sonra README ve CI bu kanonik komutları sağlamalıdır:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet test --filter "Category=Unit|Category=Architecture"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Component"
dotnet test --filter "Category=Security"
```

Playwright, contract ve performans için repo script/target'ı eklendiğinde burada kesin komut yazılır. Dokümante edilen komut CI'dakiyla aynı davranmalıdır.

SDK, merkezi MSBuild ve ignore politikasının ayrıntılı kontrolü için [`repository-policy-verification.md`](repository-policy-verification.md) kullanılır.

## Başarısız, flaky ve atlanan test politikası

- Başarısız test, ilgili görevi/faz kapısını engeller.
- Test beklenen davranış yanlış olmadığı sürece silinmez veya gevşetilmez; ürün kararı değiştiyse belge/acceptance önce güncellenir.
- Flaky test yeniden çalıştırmayla yeşile boyanmaz; karantinaya alınacaksa issue/roadmap notu, sahibi, son tarih ve görünür başarısızlık metriği gerekir.
- `Skip` yalnız dış platformun geçici olarak bulunmadığı açık durumda ve takip kaydıyla kullanılır; güvenlik/klinik bütünlük testleri atlanamaz.
- Test çalıştırılamadıysa `ROADMAP.md` kutusu `[ ]` kalır ve tekrar komutu/engel ilerleme günlüğüne yazılır.

## Faz kapısı raporu

Her faz kapısı şu özeti üretir:

```text
Faz/commit:
Ortam ve bağımlılıklar:
Çalıştırılan komutlar:
Geçen/başarısız/atlanan testler:
Ana E2E senaryoları:
Yetki allow/deny kanıtı:
Concurrency/veri bütünlüğü kanıtı:
Erişilebilirlik/manual charter:
Güvenlik/gizlilik bulguları:
Açık riskler ve karar:
```

Açık kritik/yüksek güvenlik bulgusu, temel başarı senaryosu hatası, veri bütünlüğü bozulması veya belgesiz skipped test faz kapısını engeller.

## Coverage yaklaşımı

Tek bir minimum yüzde kalite hedefi değildir. Coverage raporu değişimde düşen/dokunulmayan kritik yolları gösterir. Şunların davranış kapsamı zorunludur:

- Permission + resource authorization
- Klinik durum makineleri ve correction
- Randevu/yatak/stok concurrency
- Audit ve hassas veri redaction
- Final sonuç/reçete yayınlama
- Retention dry-run/imha kanıtı

Anlamsız getter/setter veya implementation satırlarını test ederek yüzde yükseltme yapılmaz.
