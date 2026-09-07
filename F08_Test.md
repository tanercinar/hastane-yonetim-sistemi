# Faz 8 Manuel Test Rehberi

Bu rehber Faz 8 acil servis, ameliyathane, perioperatif kayıt, yoğun bakım ve alanlar arası devir teslim akışlarını yalnız sentetik `DEMO` veriyle elle doğrulamak içindir. Gerçek kişi/kurum verisi kullanmayın; klinik serbest metni, cookie, antiforgery belirtecini veya parolayı log ve ekran görüntüsüne kopyalamayın.

> **Kapı durumu:** Kod incelemesi `F08-G03` ve `F08-G06` kabul ölçütlerinde iki mimari engel buldu. M03 ve M06 düzeltilip otomatik regresyon testleri geçmeden `F08-KAPI` kapatılamaz. Diğer manuel senaryolar bu arada uygulanabilir.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
Tarayıcı ve sürümü:

F08-M01 Otomatik ön kapı ve uygulama başlangıcı       : PASS / FAIL
F08-M02 Yetki, kaynak kapsamı ve klinik menü          : PASS / FAIL
F08-M03 Acil kabul, insan triyajı ve mevcut modüller   : PASS / FAIL
F08-M04 Acil takip panosu ve gerçek zamanlılık         : PASS / FAIL
F08-M05 Cerrahi plan, çakışma ve perioperatif kayıt    : PASS / FAIL
F08-M06 Tek yatış zincirli ICU kabul/transfer/taburcu  : PASS / FAIL
F08-M07 ICU flowsheet, birim/zaman ve simülasyon       : PASS / FAIL
F08-M08 ISBAR devir teslim ve sahiplik                 : PASS / FAIL
F08-M09 Hata, responsive, erişilebilirlik ve gizlilik  : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Başka hastaya/bölüme veri sızıntısı, beklenmeyen `500`, aynı oda/ekip/yatak için iki aktif kayıt, imzalı perioperatif kaydın üzerine yazılması, klinik içeriğin audit/SignalR/log gövdesine taşınması, acil tetkik/konsültasyonunun ana modüllerde görünmemesi veya ICU hareketinin yatan hasta zincirinden kopması genel kararı `FAIL` yapar.

## 2. F08-M01 — Otomatik ön kapı ve uygulama başlangıcı

Docker Desktop açıkken repository kökünde çalıştırın:

```powershell
docker info --format "{{.ServerVersion}}"
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --configuration Release --no-build --no-restore
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj --configuration Release --no-build --no-restore
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj --configuration Release --no-build --no-restore
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F08"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F08"
dotnet format .\HospitalManagement.slnx --verify-no-changes --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Beklenen: build 0 uyarı/0 hata; başarısız veya atlanan test yok; format ve Markdown/bağlantı doğrulaması başarılı. `Roadmap~F08` filtresinde gerçek Playwright testi bulunmuyorsa bu da kapı engelidir; entegrasyon testini E2E saymayın.

Uygulamayı hazırlayın:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` HTTP 200 dönmelidir. Rolleri ayrı gizli pencere veya tarayıcı profillerinde açın.

| Rol | E-posta | Parola |
|---|---|---|
| Hekim | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` |
| Anestezist hekim | `DEMO-anesthesiologist@hospital.invalid` | `DEMO-Anesth-Pass!1` |
| Hemşire | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` |
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |

Kanonik kimlikler: hasta `00000000-0000-0000-0000-000000000121`, hekim `00000000-0000-0000-0000-000000000102`, anestezist `00000000-0000-0000-0000-000000000111`, hemşire `00000000-0000-0000-0000-000000000103`, Genel Cerrahi bölümü `30000000-0000-0000-0000-000000000009`.

## 3. F08-M02 — Yetki, kaynak kapsamı ve klinik menü

1. Hekim hesabında yalnız verilen izinlere ait Acil, Ameliyat, ICU ve Klinik Devir bağlantılarının göründüğünü doğrulayın.
2. Sistem yöneticisinde bu klinik bağlantılar görünmemelidir. Adresleri elle açmayı ve Faz 8 GET/POST endpoint'lerini çağırmayı deneyin; `403 Forbidden` bekleyin.
3. Başka bölüme atanmış fakat aynı rol/izne sahip bir hekimle cerrahi kayıt GUID'ini çağırın. Kayıt içeriği yerine `403` veya varlık oracle'ı sızdırmayan güvenli `404` bekleyin.
4. Yetkisiz API cevabının arayüzde boş liste gibi görünmediğini; açık bir hata/forbidden durumu gösterdiğini doğrulayın.

## 4. F08-M03 — Acil kabul, insan triyajı ve mevcut klinik modüller

1. Hekimle `/emergency/admissions` açın; kanonik hasta için sentetik şikâyetli acil kabul oluşturun. Protokol `DEMO-EMG-*` olmalıdır.
2. Hemşireyle triyaj seviyesini kendiniz seçin. Arayüzün otomatik tıbbi karar vermediğini ve eğitim/simülasyon uyarısını gösterdiğini doğrulayın.
3. Geçersiz geliş şekli, triyaj seviyesi, order türü/önceliği veya konsültasyon aciliyeti gönderin. API `400 Validation Problem` dönmeli; değer sessizce varsayılana çevrilmemelidir.
4. Acil encounter ekranından STAT laboratuvar ve radyoloji istemi oluşturun. Aynı istemler ana Diagnostics iş listesi ve hasta tanısal zaman çizelgesinde aynı kayıt kimliğiyle görünmelidir; ikinci bir “acil sonuç” kopyası oluşmamalıdır.
5. Acil encounter ekranından branş konsültasyonu açın. Kayıt ana Clinical Records konsültasyon iş listesinde aynı yaşam döngüsüyle görünmelidir.

Mevcut inceleme bulgusu: 4 ve 5 şu anda ayrı `EmergencyCareOrder`/`EmergencyConsultation` modelleri nedeniyle beklenen biçimde çalışmaz. Bu durum düzeltilmeden M03 ve Faz 8 kapısı `FAIL` kalır.

## 5. F08-M04 — Acil takip panosu ve gerçek zamanlılık

1. İki yetkili klinik pencere açın; birinde `/emergency/board`, diğerinde acil kayıt/triyaj ekranı olsun.
2. İkinci pencerede triyaj, hekim atama ve durum değişikliği yapın. Pano yenilenmeden birkaç saniye içinde kanonik REST verisini tekrar çekip güncellenmelidir.
3. Network/SignalR mesaj gövdesini inceleyin. Yalnız değişiklik zamanı/yenileme sinyali bulunmalı; hasta, başvuru, protokol, yatak/alan, hekim veya triyaj bilgisi taşınmamalıdır.
4. Kayıt personeli veya sistem yöneticisinin acil SignalR grubuna katılmadığını doğrulayın.

## 6. F08-M05 — Cerrahi plan, çakışma ve perioperatif kayıt

1. `/surgery/scheduling` üzerinde Genel Cerrahi bölümü, hekim, anestezist ve `DEMO-OR-01` ile gelecekte bir ameliyat planlayın.
2. Aynı oda, sorumlu cerrah veya anestezist için çakışan zaman aralığını iki pencereden eşzamanlı gönderin. Yalnız biri başarılı, diğeri `409 Conflict` olmalıdır.
3. Hekim/anestezist için hem meslek hem bölüm ataması doğrulanmalıdır; hemşire kimliğini anestezist alanına göndermek `403`/validation ile reddedilmelidir.
4. Altı pre-op maddesinden biri eksikken hazır durumuna geçiş uyarısı alın; tümünü tamamlayınca kayıt temiz biçimde ilerlesin.
5. Perioperatif zamanların sırasını bozmayı deneyin; `400` bekleyin. Geçersiz anestezi veya post-op hedef değeri de `400` dönmelidir.
6. Kaydı imzalayın. Doğrudan değiştirme reddedilmeli; yalnız gerekçeli correction/addendum eklenmeli ve eski değer korunmalıdır.

## 7. F08-M06 — Tek yatış zincirli ICU kabul, transfer ve taburculuk

1. Önce [`F07_Test.md`](F07_Test.md) ile kanonik hasta için gerçek bir aktif yatan hasta kabulü ve yatak ataması oluşturun; gerçek yatış GUID'ini not edin.
2. ICU kabulünde bu GUID'i ve aynı hastayı kullanın. Rastgele veya başka hastaya ait `InpatientStayId` göndermek `400`/`409` ile reddedilmelidir.
3. ICU yatağına kabul sonrası yatan hasta hareket zincirinde ICU servisi/yatağına tek hareket görünmelidir; önceki yatak serbest/temizlik durumuna geçmelidir.
4. Aynı ICU yatağına iki hastayı eşzamanlı kabul edin. Yalnız biri başarılı, diğeri `409 Conflict` olmalıdır.
5. Servise devirde gerçek hedef servis/yatak seçin. Aynı `InpatientStayId` altında yeni hareket oluşmalı, ICU yatağı boşalmalı ve yatan hasta hedef yatakta görünmelidir.
6. Doğrudan taburculukta hem ICU kaydı hem ana yatan hasta kaydı tek işlem zinciri içinde kapanmalı; ikinci taburculuk reddedilmelidir.

Mevcut inceleme bulgusu: API rastgele `InpatientStayId` kabul ediyor ve ICU çıkışında ana Inpatient transfer/taburculuk servisini güncellemiyor. Bu durum düzeltilmeden M06 ve Faz 8 kapısı `FAIL` kalır.

## 8. F08-M07 — ICU flowsheet, birim/zaman ve simülasyon

1. Aktif ICU yatışından flowsheet ekranını açın. Loading, empty ve error durumlarını ayrı ayrı gözlemleyin.
2. Sentetik HR, tansiyon, solunum, SpO2, sıcaklık, GCS/RASS, ventilasyon ve sıvı giriş/çıkış değerleri girin.
3. Her değerin birimini ve kaynağın kayıt zamanını tabloda doğrulayın; 24 saatlik net sıvı dengesi elle yaptığınız hesapla aynı olmalıdır.
4. Ekranda cihaz entegrasyonu olmadığı, değerlerin insan girişi ve eğitim/simülasyon verisi olduğu açıkça görünmelidir.
5. Geçersiz ventilasyon modu veya klinik aralık dışı değer gönderin; güvenli validation hatası bekleyin.

## 9. F08-M08 — ISBAR devir teslim ve sahiplik

1. Acil → ameliyathane ve ameliyathane → ICU için sentetik ISBAR kaydı açın. Kaynak/hedef alan, sorumlu kişi, açık görev ve kritik uyarı alanlarını doğrulayın.
2. Devreden kişi kendi devrini kabul edememeli; hedef ekip kabul edene kadar kaynak ekibin sahipliği görünür kalmalıdır.
3. Aynı hasta için ikinci açık devir girişimi `409 Conflict` olmalıdır.
4. Ret ve iptal işlemlerinde zorunlu gerekçe, audit eylemi ve geçmiş korunmalıdır.
5. Geçersiz kaynak/hedef alanı `400` dönmeli ve sessiz varsayılan üretmemelidir.

## 10. F08-M09 — Hata, responsive, erişilebilirlik ve gizlilik

1. Faz 8 ekranlarını `390x844`, tablet ve geniş masaüstünde açın. Yatay taşma, ulaşılamayan modal veya üst üste binen kontrol olmamalıdır.
2. Temel akışları yalnız klavyeyle tamamlayın. Odak görünür, sıra anlamlı; form label'ları ve düğmeler erişilebilir ad taşımalıdır.
3. API'yi durdurup sayfaları yenileyin. Boş klinik liste yerine ham stack trace/klinik içerik içermeyen error state görünmelidir.
4. Tarayıcı konsolu, URL, Network, sunucu logu ve telemetry içinde klinik serbest metin, protokol, cookie, antiforgery tokenı, parola veya secret bulunmamalıdır.
5. Audit kayıtlarında teknik eylem/hedef/aktör/zaman bulunmalı; şikâyet, vital, ameliyat notu, ISBAR metni veya ICU gözlemi kopyalanmamalıdır.

## 11. Kapı kararı

`F08-G03` ve `F08-G06` mimari engelleri giderilmiş, Faz 8'e özel gerçek Playwright akışı eklenmiş, tüm otomatik kontroller ve F08-M01–F08-M09 adımları `PASS` olmuşsa sonucu `ROADMAP.md` ilerleme günlüğüne ekleyip `F08-KAPI` kutusunu işaretleyin. Bir adım başarısızsa kapı açık kalır; güvenli yeniden üretme adımlarını kaydedin ve düzeltmeden sonra otomatik regresyon testi ekleyin.
