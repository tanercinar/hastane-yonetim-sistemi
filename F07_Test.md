# Faz 7 Manuel Test Rehberi

Bu rehber Faz 7 yatış, servis, yatak, hemşirelik, eMAR, transfer, taburculuk ve gerçek zamanlı dashboard akışını yalnız sentetik `DEMO` veriyle elle kabul etmek içindir. Gerçek kişi/kurum verisi kullanmayın; klinik serbest metni, cookie, antiforgery belirtecini veya parolayı log ve ekran görüntüsüne kopyalamayın.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
Tarayıcı ve sürümü:

F07-M01 Otomatik ön kapı                            : PASS / FAIL
F07-M02 Yatış istemi, kabul ve yatak bütünlüğü      : PASS / FAIL
F07-M03 Transfer ve yatak hareketi                  : PASS / FAIL
F07-M04 Klinik pano ve bölüm dışı erişim            : PASS / FAIL
F07-M05 Hemşire gözlemi, düzeltme ve bakım görevi   : PASS / FAIL
F07-M06 Aktif reçeteye bağlı eMAR ve 5 Doğru        : PASS / FAIL
F07-M07 Taburculuk, yatak temizliği ve MOCK sevk    : PASS / FAIL
F07-M08 SignalR dashboard ve yeniden bağlanma       : PASS / FAIL
F07-M09 Responsive, klavye ve hata/gizlilik durumu  : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Başka hastaya/bölüme veri sızıntısı, beklenmeyen `500`, aynı hastaya/yatağa iki aktif kayıt, aktif reçetesiz eMAR planı, iki terminal ilaç uygulaması/taburculuk, klinik içeriğin log/audit/SignalR gövdesine yazılması veya eski yatağın dolu kalması genel kararı `FAIL` yapar.

## 2. F07-M01 — Otomatik ön kapı

Docker Desktop tamamen açılmışken repository kökünde çalıştırın:

```powershell
docker info --format "{{.ServerVersion}}"
dotnet build .\HospitalManagement.slnx --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj --no-build
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --no-build --filter "Roadmap~F07"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj --no-build --filter "Roadmap~F07"
dotnet format .\HospitalManagement.slnx --verify-no-changes --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Beklenen: build 0 uyarı/0 hata; başarısız veya atlanan test yok; format ve Markdown/bağlantı doğrulaması başarılı. Docker erişim hatası ortam engelidir fakat kapı bu durumda kapatılamaz.

## 3. Uygulamayı hazırlama

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` HTTP 200 dönmelidir. Hekim, hemşire ve sistem yöneticisini ayrı gizli pencere veya tarayıcı profillerinde açın.

| Rol | E-posta | Parola |
|---|---|---|
| Hekim | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` |
| Hemşire | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` |
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |

Kanonik kimlikler: hasta `00000000-0000-0000-0000-000000000109`, hekim kişi `00000000-0000-0000-0000-000000000102`, Kardiyoloji bölümü `30000000-0000-0000-0000-000000000003`.

eMAR testi için aynı hastaya ait en az bir aktif imzalı reçete gerekir. Mevcut değilse [`F05_Test.md`](F05_Test.md) içindeki hekim reçete akışını kullanarak `DEMO-Parasetamol 500mg Tablet`, `500 mg`, `Oral` kalemli sentetik reçete oluşturup imzalayın. Serbest metinle eMAR order'ı üretmeyin.

## 4. F07-M02 — Yatış istemi, kabul ve yatak bütünlüğü

1. Hekimle `/inpatient/admissions` sayfasını açıp `Yeni Yatış İstemi` seçin. Kanonik hasta/hekim kimliklerini, Kardiyoloji servisini, sentetik yatış gerekçesini ve isteğe bağlı tanı/risk/diyet alanlarını girin; ilk yatağı boş bırakabilirsiniz.
2. Oluşan kaydın `Requested` olduğunu ve `DEMO-ADM-*` yatış numarası aldığını doğrulayın.
3. Aynı hasta için ikinci aktif yatış istemi oluşturmayı deneyin. Güvenli hata gösterilmeli, ikinci aktif kayıt oluşmamalıdır.
4. Hemşireyle aynı sayfada istemi `Kabul Et`, ardından `Yatağa Al` ile Kardiyoloji'deki müsait bir yatağa yerleştirin. Durum `Admitted`, yatak `Occupied` olmalıdır.
5. İkinci bir yatış kaydını aynı yatağa atamayı REST istemcisi veya ikinci pencereyle eşzamanlı deneyin; yalnız bir işlem başarılı olmalı, diğeri `409 Conflict` almalıdır.
6. Başka servise ait bir yatak kimliğini kabul isteğine enjekte edin; `403`/güvenli `404` bekleyin ve mevcut yatış/yatak değişmemelidir.

## 5. F07-M03 — Transfer ve yatak hareketi

1. Hemşireyle `/inpatient/transfers` açın; aktif yatışı ve aynı servis içindeki farklı müsait yatağı hedefleyerek transfer isteği oluşturun.
2. İkinci aktif transfer isteği açmayı deneyin; `409 Conflict` ve tek aktif transfer bekleyin.
3. Talebi kabul edin, hedef yatağı seçin ve transferi tamamlayın.
4. `/inpatient/beds` ve `/inpatient/admissions` üzerinde eski yatağın `Cleaning`, yeni yatağın `Occupied`, yatışın `Admitted` ve yeni yatağa bağlı olduğunu doğrulayın. Transfer geçmişi kaybolmamalıdır.
5. Ayrı bir transfer oluşturup kabulden önce veya sonra zorunlu gerekçeyle iptal edin; hedef rezervasyon serbest kalmalı ve hasta eski yatağında kalmalıdır.
6. Bölümler arası kaynak/hedef ekip devri otomatik PostgreSQL testinde ayrı personel atamalarıyla doğrulanır. Manuel ortamda hedef bölüm kullanıcısı hazırlamadıysanız bu adımı veritabanı atamasını elle değiştirerek taklit etmeyin; aynı servis içi akışla UI'yi, F07 integration testiyle kapsam devrini kabul edin.

## 6. F07-M04 — Klinik pano ve bölüm dışı erişim

1. Hekim ve hemşireyle `/inpatient/board` açın. Aktif yatış yalnız yetkili Kardiyoloji kapsamında görünmeli; servis, düşme riski ve izolasyon filtreleri doğru çalışmalıdır.
2. Hasta özetini açın; görev için gerekli minimum alanlar görünmeli, ilgisiz tam demografi veya başka modülün klinik notları taşınmamalıdır.
3. Sistem yöneticisiyle giriş yapın. Faz 7 klinik menü bağlantıları görünmemeli; `/api/v1/inpatient/board`, `/admissions` ve `/dashboard` doğrudan çağrıları `403 Forbidden` dönmelidir.
4. Rastgele yatış/servis/yatak GUID'i veya başka bölüm kimliği ile ayrıntı endpoint'ini deneyin; varlık içeriği ve var/yok oracle'ı sızmamalıdır.

## 7. F07-M05 — Hemşire gözlemi, düzeltme ve bakım görevi

1. Hemşireyle `/inpatient/nursing` açıp aktif yatışı seçin. `Vital & Gözlem Gir` ile yalnız sentetik değerler kaydedin.
2. Kaydı doğrudan değiştirme/silme seçeneği bulunmamalıdır. `Düzeltme` ile zorunlu gerekçe ve düzeltilmiş değer ekleyin; eski gözlem korunmalı, yeni kayıt ona referans vermelidir.
3. `Bakım Planı Ekle` ile sentetik problem/hedef oluşturun; gelecekteki bir görev ekleyip tamamlayın.
4. Geçmiş zamanlı ikinci görev ekleyin ve `Geciken Görevler` sekmesine geçin. UI `Overdue/Gecikmiş` göstermeli; salt GET işlemi klinik kaydı sessizce kalıcı olarak değiştirmemelidir.
5. Aynı görevi iki pencereden aynı anda tamamlayın; tek terminal sonuç oluşmalı, diğer işlem güvenli conflict/hata göstermelidir.

## 8. F07-M06 — Aktif reçeteye bağlı eMAR ve 5 Doğru

1. Hemşireyle `/inpatient/emar` açıp aktif yatışı seçin. `Yeni İlaç Dozu Planla` modalında yalnız hastaya ait aktif imzalı reçete kalemleri görünmelidir.
2. Aktif reçetesi olmayan başka bir yatışta modal serbest metin sunmamalı ve `Dozu Kaydet` devre dışı kalmalıdır.
3. Parasetamol kalemini seçip dozu planlayın. İlaç, doz ve yol seçilen reçeteyle aynı olmalıdır.
4. `Uygula` modalında beş onayın tamamı işaretlenmeden onay düğmesi etkinleşmemelidir. Beşini işaretleyip uygulayın; uygulayan hemşire ve zaman görünmelidir.
5. Yeni dozlar üzerinde `Ertele`, `Atla` ve `Hasta Reddetti` durumlarını zorunlu sentetik gerekçelerle ayrı ayrı doğrulayın. Terminal durumdaki doz tekrar uygulanamamalıdır.
6. Aynı planlı dozu iki pencereden eşzamanlı uygulamayı deneyin; yalnız bir `Administered` kaydı oluşmalıdır.

## 9. F07-M07 — Taburculuk, yatak temizliği ve MOCK sevk

1. Hekimle `/inpatient/discharges` açın; aktif yatışı `Taburcu Et / Sevk` ile seçin.
2. 20 karakterden kısa epikrizle gönderme düğmesi etkinleşmemeli veya API `400` dönmelidir.
3. En az 20 karakterlik sentetik epikriz, tanı ve isteğe bağlı öneri/takip alanlarıyla `Home` taburculuğunu tamamlayın.
4. Yatış `Discharged`, yatak `Cleaning`, geçmiş taburculuk özeti görünür olmalıdır. Aynı yatış ikinci kez taburcu edilememelidir.
5. Ayrı bir demo yatışta `Kurum Dışı Sevk` seçin. Kurum adı `DEMO-*` olmalı; kayıt ve arayüz açıkça `MOCK` niteliğini taşımalı, gerçek endpoint çağrısı yapılmamalıdır.

## 10. F07-M08 — SignalR dashboard ve yeniden bağlanma

1. Hemşireyle iki pencere açın: birinde `/inpatient/dashboard`, diğerinde yatış/yatak işlemi. Dashboard'daki canlı bağlantı göstergesini doğrulayın.
2. İkinci pencerede yatış kabulü, transfer veya taburculuk yapın. İlk pencereyi yenilemeden birkaç saniye içinde sayaç/doluluk değişmelidir.
3. Tarayıcı Network panelini çevrimdışı yapıp tekrar çevrimiçi alın. Reconnect sonrası dashboard canonical REST verisini yeniden çekmeli ve çift sayım göstermemelidir.
4. SignalR mesaj gövdesinde hasta/yatış/yatak/servis kimliği, klinik not veya serbest metin bulunmamalıdır; yalnız yenileme sinyali/zaman bilgisi taşımalıdır.
5. Sistem yöneticisi SignalR klinik grubuna katılmamalı ve Faz 7 dashboard verisini alamamalıdır.

## 11. F07-M09 — Responsive, klavye ve hata/gizlilik durumu

1. Sekiz Faz 7 sayfasını `390x844`, tablet ve geniş masaüstü viewport'larında açın. Yatay taşma, üst üste binen modal veya erişilemeyen işlem olmamalıdır.
2. Yatış, vital, eMAR ve taburculuk akışlarını yalnız klavyeyle deneyin. Odak görünür, sıra anlamlı, form label'ları ve modal düğmeleri erişilebilir ad taşımalıdır.
3. Boş veride anlaşılır empty state; yavaş bağlantıda loading; API durduğunda ham exception/stack trace içermeyen error state; yetkisiz rolde forbidden davranışı doğrulayın.
4. Tarayıcı konsolu, Network URL'leri ve sunucu loglarında klinik serbest metin, cookie, antiforgery tokenı, parola veya secret arayın; hiçbiri bulunmamalıdır.
5. Audit kayıtlarında eylem, hedef kimliği, aktör ve zaman bulunmalı; yatış gerekçesi, transfer/hemşire notu, epikriz veya ilaç gerekçesi kopyalanmamalıdır.

## 12. Kapı kararı

Tüm otomatik kontroller ve `F07-M01`–`F07-M09` adımları `PASS` ise sonucu `ROADMAP.md` ilerleme günlüğüne ekleyin, `F07-KAPI` kutusunu işaretleyin ve aktif görevi `F08-G01` yapın. Bir adım başarısızsa `F07-KAPI` açık kalır; güvenli yeniden üretme adımlarını kaydedin ve düzeltmeden sonra ilgili otomatik regresyon testini ekleyin.
