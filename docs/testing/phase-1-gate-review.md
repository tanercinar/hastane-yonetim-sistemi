# Faz 1 Kapı İncelemesi

- **Tarih:** 2026-08-27
- **Kapsam:** F01-G01–F01-G11 ve F01-KAPI
- **Karar:** Geçti
- **İnceleme türü:** İzole temiz restore/build, otomatik regresyon, gerçek container/migration, HTTP ve gerçek tarayıcı smoke testi

## Kapı ölçütleri

| Ölçüt | Kanıt | Sonuç |
|---|---|---|
| Sabit SDK ve deterministik bağımlılık grafiği | .NET SDK 10.0.201; tüm projelerde lock dosyası; boş geçici paket deposuna `--locked-mode --force --no-cache` restore | Geçti |
| Temiz Release build | İzole NuGet deposuyla 26 proje; 0 uyarı, 0 hata | Geçti |
| Mimari sınırlar | Architecture testleri 13/13; yasak proje/modül kenarları, deterministik WebAssembly restore ve CI sözleşmesi | Geçti |
| Yerel servisler | PostgreSQL sorgusu, MinIO HTTP 200, Mailpit HTTP 200 ve sentetik `DEMO` SMTP yakalama | Geçti |
| Secret güvenliği | Rastgele ignore edilen `.env`; BOM'suz atomik user-secrets senkronizasyonu; değerler terminale yazılmadı | Geçti |
| Veritabanı temeli | Migration gerçek yerel PostgreSQL'e uygulandı/veritabanı güncel; pending model change yok | Geçti |
| API temeli | Ana sayfa HTTP 200; live/ready `Healthy`; `/api/v1/platform/status` v1/DEMO; OpenAPI 3.1.1 | Geçti |
| Web kabuğu | Gerçek tarayıcıda dashboard → UI states yolculuğu; 390 × 844 görünümde yatay taşma ve console error yok | Geçti |
| Güvenli hata ve telemetry | Eksik kritik config güvenli anahtar adıyla reddediliyor; secret canary/stack trace sızıntısı yok; correlation/trace sözleşmesi testli | Geçti |
| Test omurgası | Unit 1/1, architecture 13/13, component 5/5, integration 17/17, E2E 1/1; toplam 37/37 | Geçti |
| Coverage/CI | Beş Cobertura çıktısı; locked restore, format, build, zafiyet taraması, Playwright ve tüm testleri içeren `CI / quality` | Geçti |
| Yeni geliştirici akışı | Kök `README.md` ilk kurulum/çalıştırma/test akışını; `F1_Test.md` ayrıntılı manuel kabulü tanımlar | Geçti |

## Temiz kurulum bulgusu ve düzeltmesi

İlk izole restore provası, WebAssembly SDK'sının otomatik `Microsoft.DotNet.HotReload.WebAssembly.Browser` bağımlılığının Web.Client lock dosyasında bulunmadığını `NU1004` ile ortaya çıkardı. Önceki üretilmiş `obj` durumu bunu görünür kılmıyordu. Üretilmiş dosyalar dışlanarak oluşturulan temiz checkout kopyası hatayı tekrar üretti.

.NET 10 WebAssembly SDK'sının bu paketi varsayılan olarak yalnız Debug restore'da eklediği, Release restore'da eklemediği SDK target'ından doğrulandı. Tek lock dosyasının yapılandırmaya göre değişmesini engellemek için Web.Client projesinde `WasmEnableHotReload=false` sabitlendi; geliştirmede otomatik Hot Reload yerine normal tarayıcı yenilemesi kullanılacaktır. Tüm lock dosyaları `--force-evaluate` ile bu yapılandırmadan bağımsız grafik üzerinden yeniden üretildi. Kalıcı architecture testi hem özelliği hem lock dosyasında koşullu paketin bulunmadığını denetler.

Son provada `.git`, `.env`, `bin`, `obj` ve `artifacts` içermeyen temiz repository kopyası oluşturuldu; tamamen boş ve kopyaya özel NuGet deposuna locked/no-cache restore yapıldı. Release build 0 uyarı/0 hatayla tamamlandı. Build server'lar düzenli kapatıldı ve yalnız doğrulanmış geçici kopya kaldırıldı. Ürün/derleme hatası kalmadı.

## Otomatik test kanıtı

| Proje | Test sayısı | Kapsam |
|---|---:|---|
| `HospitalManagement.UnitTests` | 1 | UTC/concurrency ortak persistence davranışı |
| `HospitalManagement.ArchitectureTests` | 13 | katman/modül bağımlılıkları, deterministik restore, repository ve CI politikası |
| `HospitalManagement.ComponentTests` | 5 | dört ortak UI durumu ve kullanıcı etkileşimi |
| `HospitalManagement.IntegrationTests` | 17 | Testcontainers PostgreSQL, migration, config, API ve observability |
| `HospitalManagement.EndToEndTests` | 1 | gerçek Kestrel, Chromium ve mobil viewport kullanıcı akışı |

CI eşdeğeri Release koşusunda 37 testin tamamı geçti. Her test projesi için bir adet olmak üzere beş Cobertura raporu üretildi. Bilinen doğrudan veya transitif NuGet zafiyeti raporlanmadı.

## Gerçek altyapı ve tarayıcı kanıtı

Yerel Compose modeli boş örnek secret'ları reddetti. PostgreSQL 18, kaynak koddan sabitlenmiş MinIO ve Mailpit healthy oldu. PostgreSQL `SELECT 1` kabul etti; MinIO ve Mailpit health endpoint'leri HTTP 200 döndürdü; Mailpit `.invalid` alıcılı sentetik `DEMO` SMTP mesajını yakaladı. Migration komutu gerçek yerel PostgreSQL üzerinde veritabanını güncel buldu ve model snapshot farkı raporlamadı.

Host gerçek Development yapılandırmasıyla loopback üzerinde çalıştırıldı. Ana sayfa, iki health endpoint'i, sürümlü status API'si, OpenAPI belgesi ve bağlantılı statik asset'ler HTTP 200 döndürdü. Tarayıcıda Türkçe dashboard, `DEMO` etiketleri, internet gereksinimi, örnek metrikler ve durum bileşenleri görüldü. `390 × 844` viewport'ta belge genişliği viewport'u aşmadı; ana başlık/navigasyon görünür kaldı; console warning/error oluşmadı.

## Güvenlik ve kapsam incelemesi

- Secret, token, connection string veya klinik içerik kaynak dosyalara ve test çıktılarına eklenmedi.
- Yalnız sentetik `DEMO` canary ve reserved `.invalid` adresleri kullanıldı.
- Uygulamaya yapay zekâ eklenmedi; gerçek entegrasyon çağrısı yapılmadı.
- Modüller arası yasak Infrastructure/DbContext bağımlılığı oluşmadı.
- Henüz Faz 2 konusu olan kimlik/yetkilendirme işlevi erken eklenmedi.
- GitHub ruleset dış repository ayarıdır; [`../development/ci.md`](../development/ci.md) içindeki `CI / quality` zorunlu kontrol adımı repository oluşturulduğunda uygulanacaktır.

## Açık notlar

- Development console telemetry exporter ayrıntılı trace/metric çıktısı üretir. Bu bilinçli yerel gözlemlenebilirlik davranışıdır; production yapılandırmasında console exporter reddedilir.
- Faz 1 dashboard'u sentetik sunum verisi taşır; gerçek rol/permission, hasta ve klinik akışlar Faz 2 ve sonrasında uygulanacaktır.
- Manuel tekrar kabul adımları kök [`../../F1_Test.md`](../../F1_Test.md) dosyasındadır.

## Son karar

Faz 1'in temiz kurulum, build, container, migration, web/API, observability, test ve CI temelleri uygulanabilir ve birbirleriyle tutarlıdır. Açık kritik veya majör uyarı yoktur. Faz 1 kapısı **geçmiştir**; bir sonraki uygulanabilir görev `F02-G01 — Organizasyon modeli`dir.
