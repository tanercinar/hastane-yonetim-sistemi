# Faz 5 Manuel Test Rehberi

Bu rehber Faz 5 reçete, eczane ve klinik stok akışını yalnız sentetik `DEMO` verilerle elle kabul etmek içindir. Token, cookie, parola veya klinik serbest metni ekran görüntüsü/log kaydına kopyalamayın.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
Tarayıcı:

F05-M01 Otomatik ön kapı                    : PASS / FAIL
F05-M02 Doktor rolü ve encounter kapsamı    : PASS / FAIL
F05-M03 Güvenlik uyarısı ve imza            : PASS / FAIL
F05-M04 Eczacı iş listesi ve izolasyon      : PASS / FAIL
F05-M05 FEFO teslim ve idempotency           : PASS / FAIL
F05-M06 Stok concurrency ve hareket geçmişi : PASS / FAIL
F05-M07 Hasta görünümü ve IDOR               : PASS / FAIL
F05-M08 Mobil/erişilebilirlik/hata durumları : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Beklenmeyen `500`, başka hastaya/bölüme veri sızıntısı, çift stok düşümü veya terminal kaydın değişmesi genel kararı `FAIL` yapar.

## 2. F05-M01 — Otomatik ön kapı

Docker Desktop çalışırken repository kökünde:

```powershell
dotnet build .\HospitalManagement.slnx --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj --no-build
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --no-build --filter "Roadmap~F05"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj --no-build
```

Beklenen: build 0 uyarı/0 hata; unit 162/162, component 43/43, architecture 13/13, Faz 5 integration 16/16 ve E2E 4/4.

## 3. Uygulamayı hazırlama

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` HTTP 200 dönmelidir. Her rolü ayrı gizli tarayıcı profili veya ayrı tarayıcı bağlamında açın.

| Rol | E-posta | Parola |
|---|---|---|
| Hekim | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` |
| Eczacı | `DEMO-pharmacist@hospital.invalid` | `DEMO-Pharm-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |

Kanonik kimlikler: hasta `00000000-0000-0000-0000-000000000109`, hekim `00000000-0000-0000-0000-000000000102`, Kardiyoloji `30000000-0000-0000-0000-000000000003`, Eczane `30000000-0000-0000-0000-000000000007`.

Reçete ekranı açık bir encounter gerektirir. F04 testinden kalan `InProgress` bir DEMO encounter kullanın veya REST istemcisinde hekim oturumuyla şu hazırlığı yapın:

1. `GET /api/v1/identity/antiforgery` çağrısından token alın ve yazma isteklerinde `X-HMS-CSRF` başlığına koyun.
2. `POST /api/v1/clinical-records/encounters` ile yukarıdaki hasta/hekim/bölüm için `Outpatient` encounter oluşturun.
3. Dönen `id` ve `version` ile `POST /api/v1/clinical-records/encounters/{id}/start` çağırın; `InProgress` bekleyin.
4. Encounter kimliğini aşağıdaki adımlarda kullanın. Gerçek hasta verisi girmeyin.

## 4. F05-M02 — Doktor rolü ve kaynak kapsamı

1. Hekim hesabıyla giriş yapın; sol menüde `Reçete Yaz / Yönet` görünmelidir.
2. `/doctor/prescriptions/{encounterId}` adresini açın. Hasta ve bölüm kimlikleri encounter üzerinden salt okunur dolmalıdır.
3. `Parasetamol` arayıp bir kalem ekleyin; DEMO tanı/talimat girip taslak kaydedin.
4. Rastgele encounter kimliğiyle aynı sayfayı açın; veri yerine bulunamadı/yetkisiz mesajı görünmeli, reçete oluşturulamamalıdır.
5. Sistem yöneticisiyle doktor reçete ekranını açın; `Bu alana erişiminiz yok` görünmelidir.

## 5. F05-M03 — Güvenlik uyarısı ve imza

1. Yetkili encounter'da aynı etken maddeli iki kalem veya sentetik alerjiyle eşleşen `DEMO-MED-AMX500` ekleyin.
2. Güvenlik panelinde deterministik uyarı ve sistemin klinik karar desteği/AI olmadığına dair feragat görünmelidir.
3. Gerekçesiz imza engellenmelidir. En az beş karakterli sentetik gerekçe ve gösterilen tüm uyarıların kabulüyle imza başarılı olmalıdır.
4. İmzalı reçetede klinik alanlar salt okunur olmalı; bayat `ExpectedVersion` ile API güncellemesi `409` dönmelidir.
5. Audit görüntüsünde override olayı bulunmalı fakat yazdığınız ham klinik gerekçe bulunmamalıdır.

## 6. F05-M04 — Eczacı iş listesi ve klinik izolasyon

1. Eczacı hesabıyla giriş yapın; ana rota `/pharmacy/worklist` olmalı ve imzalı reçete listede görünmelidir.
2. Reçeteyi inceleyin. Hasta doğrulama kimliği, ilaç ve kullanım talimatları görünmelidir.
3. Tanı özeti, encounter/bölüm/hekim kimlikleri, SOAP notları ve iptal/düzeltme gerekçeleri görünmemelidir.
4. Eczacıyla `/api/v1/clinical-records/notes/by-encounter/{encounterId}` çağrısı `403` dönmelidir.
5. Süresi dolmuş, taslak veya başka tesis kapsamındaki reçete aktif iş listesinde görünmemelidir.

## 7. F05-M05 — FEFO teslim ve idempotency

1. Reçete ayrıntısından `İlaç Teslim Et` açın. Lotlar en yakın geçerli miattan başlayarak sıralanmalıdır.
2. Kısmi teslim yapın; reçete `Kısmen Karşılandı`, stok ve kalan miktar doğru görünmelidir.
3. Aynı HTTP isteğini aynı `IdempotencyKey` ile tekrar gönderin; başarılı sonuç tekrar dönmeli fakat miktar/stok ikinci kez değişmemelidir.
4. Aynı anahtarı farklı miktarla gönderin; `409 Conflict` bekleyin.
5. Kalan miktarı güncel reçete ve stok sürümüyle teslim edin; durum `Tamamı Karşılandı` olmalıdır.
6. Reçete miktarını aşan veya miadı dolmuş lotla teslim `409` dönmelidir.

## 8. F05-M06 — Stok concurrency ve hareket geçmişi

1. Eczacı stok görünümünde bir lotun miktarını ve `Version` değerini kaydedin.
2. Aynı eski `ExpectedVersion` ile iki farklı stok düzeltmesi gönderin; ilki başarılı, ikincisi `409` olmalıdır.
3. Başarılı teslim sonrası stok hareketlerinde yalnız bir `Dispense` kaydı ve doğru miktar bulunmalıdır.
4. Rezerve miktarın üzerindeki kullanılabilir stoku tüketmeye çalışın; işlem reddedilmeli ve stok negatif olmamalıdır.
5. Başka bölümdeki stok kimliğiyle görüntüleme/düzeltme denemesi veri sızdırmamalıdır.

## 9. F05-M07 — Hasta görünümü ve IDOR

1. Hasta hesabıyla `/patient/prescriptions` açın; yalnız kendi imzalı/geçmiş reçeteleri görünmelidir.
2. Teslim edilen reçetede `Eczaneden Alındı`, `Ağızdan`, `günde N kez` gibi sade ifadeler ve sentetik DEMO uyarısı görünmelidir.
3. Başka hasta kimliğiyle `/api/v1/pharmacy/prescriptions/by-patient/{id}` çağrısı `403` dönmelidir.
4. Taslak ve `EnteredInError` kayıtları hasta listesinde görünmemelidir.

## 10. F05-M08 — Mobil, erişilebilirlik ve hata durumları

1. Hasta reçete sayfasını 390x844 viewport'ta açın; yatay taşma olmamalı ve ayrıntı modalı kullanılabilmelidir.
2. Doktor ve eczacı akışlarını yalnız klavye ile tamamlayın; odak kaybolmamalı, label ve buton adları anlaşılır olmalıdır.
3. API'yi geçici durdurup sayfaları yenileyin; loading sonsuza kadar kalmamalı, anlaşılır hata durumu görünmelidir.
4. Tarayıcı konsolunda işlenmemiş hata, klinik veri, token veya secret bulunmamalıdır.

## 11. Kapı kararı

Tüm adımlar `PASS` ise sonucu `ROADMAP.md` ilerleme günlüğüne ekleyip `F05-KAPI` kutusunu işaretleyin ve aktif görevi `F06-G01` yapın. Bir adım başarısızsa `F05-KAPI` açık kalır; güvenli yeniden üretme adımlarını kaydedin ve düzeltmeden sonra ilgili otomatik regresyon testini ekleyin.
