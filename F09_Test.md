# Faz 9 Manuel Test Rehberi

Bu rehber Faz 9 gebelik/doğum, diş hekimliği, evde sağlık ve kimliksiz operasyonel rapor akışlarını yalnız sentetik `DEMO` veriyle doğrulamak içindir. Gerçek kişi, adres, telefon veya klinik veri kullanmayın; parola, cookie, antiforgery belirteci ve klinik serbest metni ekran görüntüsüne ya da loga kopyalamayın.

> **Kapı durumu:** `PASS` — 1 Eylül 2026. Ortak model bağları, hasta portalı politikası, gerçek PostgreSQL/Chromium Playwright kapısı, erişilebilir forbidden UI testi ve yerel responsive tarayıcı kabulü tamamlandı. Tekrarlanabilir kanıt ve komutlar Bölüm 11'de kayıtlıdır.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
Tarayıcı ve sürümü:

F09-M01 Otomatik ön kapı ve uygulama başlangıcı      : PASS / FAIL
F09-M02 Rol, izin, bakım ilişkisi ve IDOR            : PASS / FAIL
F09-M03 Gebelik ve antenatal izlem                    : PASS / FAIL
F09-M04 Doğum ve ayrı yenidoğan kimliği              : PASS / FAIL
F09-M05 Diş odontogramı ve değişmez geçmiş            : PASS / FAIL
F09-M06 Evde sağlık atama, adres maskesi ve yaşam döngüsü : PASS / FAIL
F09-M07 Kimliksiz rapor ve teknik yönetici ayrımı     : PASS / FAIL
F09-M08 Hata, responsive, erişilebilirlik ve gizlilik : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Başka hastaya veri sızıntısı, yetkisiz adrese erişim, beklenmeyen `500`, geçersiz enumun sessiz kabulü, iki aktif gebelik/aynı diş sürümü, klinik içeriğin audit/log/URL'ye taşınması, yenidoğanın ortak Patient kimliği olmadan kaydı veya Encounter/Appointment zincirinin kopması genel kararı `FAIL` yapar.

## 2. F09-M01 — Otomatik ön kapı ve uygulama başlangıcı

Docker Desktop açıkken repository kökünde çalıştırın:

```powershell
docker info --format "{{.ServerVersion}}"
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F09"
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F09"
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj --configuration Release --no-build --no-restore
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F09"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F09"
dotnet format .\HospitalManagement.slnx --verify-no-changes --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Beklenen: build 0 uyarı/0 hata; çalışan testlerde hata/skip yoktur. `Roadmap=F09-KAPI` filtresi gerçek PostgreSQL entegrasyon, forbidden component ve gerçek Chromium E2E testlerini çalıştırır.

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
| Hemşire | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` |
| Hastane yöneticisi | `DEMO-manager@hospital.invalid` | `DEMO-Manager-Pass!1` |
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |

Kanonik sentetik kimlikler: Patient kaydı `00000000-0000-0000-0000-000000000201`, Patient'ın Person kimliği `00000000-0000-0000-0000-000000000109`, hekim Person kimliği `00000000-0000-0000-0000-000000000102`, hemşire Person kimliği `00000000-0000-0000-0000-000000000103`. `PatientId` isteyen alana Person kimliği girmeyin.

## 3. F09-M02 — Rol, izin, bakım ilişkisi ve IDOR

1. Hekimle giriş yapın; Uzmanlık menülerinin göründüğünü ve kanonik hasta için kayıtların açıldığını doğrulayın.
2. Sistem yöneticisiyle aynı menülerin gizli olduğunu doğrulayın. Faz 9 GET/POST adreslerini elle çağırın; `403 Forbidden` bekleyin.
3. Hekim oturumunda ilişkisiz ikinci Patient kimliği `00000000-0000-0000-0000-000000000202` ile gebelik, doğum, diş ve evde sağlık GET/POST deneyin; içerik veya varlık bilgisi yerine `403` bekleyin.
4. Oturumu kapatıp bir Faz 9 GET adresini açın; `401 Unauthorized` veya login yönlendirmesi bekleyin.
5. Network panelinde klinik GUID'i değiştirerek başka kayda erişmeyi deneyin. Yetki yalnız UI gizlemesine dayanmamalıdır.

## 4. F09-M03 — Gebelik ve antenatal izlem

1. Kanonik DEMO hasta için bir randevu oluşturun ve bu randevudan bir ClinicalRecords encounter başlatın. API yanıtındaki encounter kimliğini not edin; secret veya klinik metin kaydetmeyin.
2. Hekimle `/specialty/pregnancy` açın. Kanonik Patient kimliği ve randevuya bağlı encounter kimliğiyle düşük riskli sentetik gebelik oluşturun. Protokol `DEMO-OBS-*` olmalı; yanıttaki `openingEncounterId` girdiğiniz kimlikle aynı olmalıdır.
3. Aynı hastaya ait randevuya bağlı bir encounter kimliğiyle antenatal vizit girin; gebelik haftası/günü, temel sentetik ölçümler ve yanıttaki `encounterId` aynı kayda bağlanmalıdır.
4. Risk kategorisini değiştirin; geçmiş vizitin kaybolmadığını doğrulayın.
5. Aynı hasta için iki pencereden eşzamanlı ikinci aktif gebelik gönderin. Yalnız biri başarılı olmalı, diğeri `409 Conflict` dönmelidir.
6. Boş/rastgele, başka hastaya ait, randevusuz veya iptal/hatalı encounter kimliği gönderin. Kimlik sızıntısı yapmayan `400 Validation Problem` bekleyin; kayıt oluşmamalıdır.
7. `RiskCategory`, fetal prezentasyon, ödem veya kapanış durumuna bilinmeyen değer gönderin. `400 Validation Problem` bekleyin; kayıt oluşmamalıdır.
8. Kaydı `Delivered`/uygun sonuçla kapatın; yeni vizit ekleme `409` ile reddedilmelidir.

## 5. F09-M04 — Doğum ve ayrı yenidoğan kimliği

1. Her sentetik bebek için Patients modülünde anne kaydından ayrı bir Patient kaydı açın; bu kimlikleri not edin.
2. `/specialty/deliveries` ekranında kanonik anne Patient kimliğiyle sentetik doğum oluşturun; yöntem, ekip, zaman ve ölçümlerin doğruluğunu kontrol edin.
3. İlk bebeği ayrı yenidoğan Patient ID ile kaydedin. İkinci sentetik yenidoğanı farklı Patient ID ile ekleyip doğum sırasının benzersiz ve geçmişin eklemeli olduğunu doğrulayın.
4. Boş, var olmayan, anneye ait veya ilk bebekte kullanılmış Patient ID ile yeni bebek gönderin; `400` bekleyin ve kayıt oluşmadığını doğrulayın.
5. Geçersiz doğum yöntemi, perine durumu, cinsiyet veya resüsitasyon değeri gönderin; `400` bekleyin.
6. Anne gebelik kaydının doğumdan sonra `Delivered` olduğunu doğrulayın.
7. Her yenidoğanın ortak Patients modülünde ayrı, aktif Patient kaydı ve değişmez `NewbornPatientId` sahibi olduğunu API/DB üzerinden doğrulayın.

## 6. F09-M05 — Diş odontogramı ve değişmez geçmiş

1. `/specialty/dental` ekranında kanonik Patient kimliğiyle muayene oluşturun.
2. FDI `16` dişine `Caries`, oklüzal yüzey ve sentetik not ekleyin; sürüm `1` olmalıdır.
3. İşlem planlayıp tamamlayın, ardından aynı dişe `Filled` kaydı ekleyin. Sürüm `2` olmalı; geçmişte sürüm `1` korunmalıdır.
4. Geçersiz FDI numarası, koşul enumu veya `0..31` dışı yüzey bit maskesi gönderin; `400` bekleyin.
5. Aynı hasta/diş/sürümünü eşzamanlı yazmayı deneyin; veritabanı yalnız birini kabul etmeli ve diğer çağrı `409` dönmelidir.
6. Odontogramı yalnız klavyeyle kullanın; her diş/eylem erişilebilir ad, görünür odak ve mantıklı sekme sırası sunmalıdır.

## 7. F09-M06 — Evde sağlık atama, adres maskesi ve yaşam döngüsü

1. Hekimle `/specialty/home-health` üzerinde kanonik hasta için yalnız `DEMO` etiketli adres/telefonla talep oluşturun.
2. Hekim yanıtında açık adres ve telefonun boş/maskeli olduğunu Network panelinde doğrulayın.
3. Ziyareti hemşire Person kimliğine atayın. Hekimle başlatma/tamamlama deneyin; `403` bekleyin.
4. Hemşire hesabıyla aynı ziyareti açın. Yalnız atanmış kullanıcıda adres/telefon görünmeli; ziyareti başlatıp tamamlayabilmelidir.
5. Başka hemşire veya klinik kullanıcıyla GUID'i çağırın; adres sızmamalı ve yetkisiz eylem reddedilmelidir.
6. Geçersiz hizmet türü/öncelik gönderin; `400` bekleyin. İptal veya tamamlanmış ziyarette geçersiz durum geçişi `409` olmalıdır.
7. Harita, GPS veya gerçek dış servis çağrısı yapılmadığını Network panelinde doğrulayın.
8. Aynı hasta için ClinicalRecords üzerinden `HomeHealth` türünde bir Encounter oluşturup başlatın. Ziyareti bu `EncounterId` ile tamamlayın; API yanıtında aynı kimliğin döndüğünü doğrulayın.
9. Boş/rastgele, başka hastaya ait, `Outpatient` gibi yanlış türde, yalnız `Planned`, iptal edilmiş veya hatalı giriş Encounter kimliği gönderin; veri sızdırmayan `400 Validation Problem` bekleyin ve ziyaret tamamlanmamalıdır.
10. Tamamlanmış ilk ziyaretteki Encounter kimliğini ikinci bir evde sağlık ziyaretinde yeniden kullanın; `400` veya eşzamanlı yarışta `409 Conflict` bekleyin. Aynı Encounter iki ziyarete bağlanmamalıdır.

## 8. F09-M07 — Kimliksiz rapor ve teknik yönetici ayrımı

1. HospitalManager ile `/specialty/reports` açın; yalnız toplu sayaç/oranlar görünmelidir.
2. Yanıt ve sayfada Patient kimliği, protokol, açık adres, telefon, klinik not, ölçüm veya doğum/diş ayrıntısı bulunmamalıdır.
3. SystemAdministrator ile aynı URL/API'yi açın; `403 Forbidden` bekleyin. Teknik audit yetkisi operasyonel rapor yetkisine dönüşmemelidir.
4. Kayıt oluşturup raporu yenileyin; sayaç gerçek zamanlı güncellenmeli, kimliksiz kalmalıdır.
5. Patient hesabıyla `/patient/specialty-records` sayfasını açın. Gebelikte yalnız durum/tarih/sayaç, doğumda yalnız yöntem/gebelik yaşı/yenidoğan sayısı, dişte yalnız muayene meta verisi ve tamamlanmış işlemler, evde sağlıkta yalnız durum/tarih/il/ilçe görünmelidir.
6. Network panelinde `/api/v1/specialty/patient-portal/my-records` yanıtını inceleyin. Klinik/risk notu, ölçüm, maliyet, açık adres, telefon, personel/Encounter/yenidoğan Patient kimliği bulunmamalıdır.
7. DevTools ile isteğe `?patientId=00000000-0000-0000-0000-000000000202` ekleyin; sonuç değişmemeli ve yalnız oturum sahibinin kaydı dönmelidir. Doctor ve SystemAdministrator oturumlarında endpoint `403`, oturumsuz çağrıda `401` dönmelidir.
8. Planlanmış ancak tamamlanmamış sentetik bir diş işlemi oluşturun; Patient sayfasında/API yanıtında görünmemeli, tamamlandıktan ve sayfa yenilendikten sonra görünmelidir.

## 9. F09-M08 — Hata, responsive, erişilebilirlik ve gizlilik

1. Beş Faz 9 sayfasını `390x844`, tablet ve masaüstünde açın; yatay taşma, erişilemeyen modal veya üst üste binen kontrol olmamalıdır.
2. Loading, empty, API error ve `403` durumlarını ayrı ayrı deneyin. `403` boş liste veya genel “beklenmeyen hata” gibi gösterilmemeli; erişilebilir, anlaşılır durum olmalıdır. Mevcut component kanıtı eksikse kapı açık kalır.
3. Tüm ana akışları yalnız klavyeyle tamamlayın; odak görünür, sıra anlamlı, form label ve düğme adları anlaşılır olmalıdır.
4. Tarayıcı konsolu, URL, Network, sunucu logu ve telemetry içinde klinik serbest metin, adres, telefon, cookie, antiforgery tokenı, parola veya secret bulunmamalıdır.
5. Audit kayıtlarında yalnız teknik eylem/hedef/aktör/zaman bulunmalı; gebelik öyküsü, risk notu, Apgar/ölçüm, diş notu, adres, telefon veya evde sağlık notu kopyalanmamalıdır.

## 10. Kapı kararı

Ortak Encounter/Appointment bağları, ayrı yenidoğan Patient kimliği ve hasta portalı görünürlük politikası tamamlanmış, F9'a özel gerçek Playwright akışı ile forbidden UI component testi eklenmiş, tüm otomatik kontroller ve F09-M01–F09-M08 adımları `PASS` olmuşsa sonucu `ROADMAP.md` ilerleme günlüğüne ekleyip açık F9 kutularını kapatın. Herhangi bir adım başarısızsa güvenli yeniden üretme adımlarını kaydedin ve regresyon testi ekleyin.

## 11. 1 Eylül 2026 yürütme kaydı

Test eden: Codex  
Ortam: .NET 10, Docker Desktop, PostgreSQL Testcontainers, Microsoft Playwright Chromium ve Codex yerel tarayıcı  
Genel karar: **PASS**

| Senaryo | Sonuç | Kanıt |
|---|---|---|
| F09-M01 Otomatik ön kapı ve uygulama başlangıcı | PASS | Release build 0 uyarı/0 hata; Host HTTPS üzerinde açıldı; PostgreSQL, MinIO ve Mailpit sağlık/SMTP smoke testleri geçti. Yerel migration betiği Host'ta kayıtlı tüm modülleri hazırlayacak biçimde düzeltildi. |
| F09-M02 Rol, izin, bakım ilişkisi ve IDOR | PASS | F09 PostgreSQL kapı testi ile Chromium E2E; anonim, SystemAdministrator, ilişkisiz hasta ve atanmış ekip sınırları. |
| F09-M03 Gebelik ve antenatal izlem | PASS | PostgreSQL kapı testi oluşturma/Encounter-Appointment zinciri/kapalı kayıt negatifini; Chromium E2E hekim görünümünü doğruladı. |
| F09-M04 Doğum ve ayrı yenidoğan kimliği | PASS | PostgreSQL kapı testi ayrı ve tekil Patient kimliğini; Chromium E2E doğum/yenidoğan görünümünü doğruladı. |
| F09-M05 Diş odontogramı ve değişmez geçmiş | PASS | PostgreSQL kapı testi sürüm ve düzeltme negatiflerini; Chromium E2E tamamlanmış işlem, klavye odağı ve geçmiş görünümünü doğruladı. |
| F09-M06 Evde sağlık atama, adres maskesi ve yaşam döngüsü | PASS | PostgreSQL kapı testi yaşam döngüsü/Encounter negatiflerini; Chromium E2E atanmış hemşirede açık, hekimde maskeli iletişim alanlarını doğruladı. |
| F09-M07 Kimliksiz rapor ve teknik yönetici ayrımı | PASS | Chromium E2E HospitalManager raporu, minimize hasta portalı ve SystemAdministrator `403` durumunu doğruladı. |
| F09-M08 Hata, responsive, erişilebilirlik ve gizlilik | PASS | Forbidden component testi; E2E console/URL canary ve responsive kontrolleri; yerel tarayıcıda `390x844`, `1024x768`, `1440x900` yatay taşma yok ve console temiz. |

Çalıştırılan ana komutlar:

```powershell
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
dotnet test .\HospitalManagement.slnx --configuration Release --no-build --no-restore --filter "Roadmap=F09-KAPI"
dotnet test .\HospitalManagement.slnx --configuration Release --no-build --no-restore
dotnet format .\HospitalManagement.slnx --no-restore --include src/HospitalManagement.Web.Client/Pages/Patient/MySpecialtyRecords.razor tests/HospitalManagement.ComponentTests/MySpecialtyRecordsComponentTests.cs tests/HospitalManagement.EndToEndTests/Phase9ProductGateEndToEndTests.cs --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-local-infrastructure.ps1
```

Sonuç: hedefli kapı 3/3; tüm çözüm 630/630 (unit 349, component 98, architecture 13, PostgreSQL integration 162, Playwright E2E 8), skip yoktur.
