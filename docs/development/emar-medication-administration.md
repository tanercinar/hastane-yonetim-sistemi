# Elektronik İlaç Uygulama Kaydı (eMAR - Medication Administration Record)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G06 — İlaç uygulama kaydı (eMAR)** görevi için geliştirilen aktif hekim order/reçetesinden planlanan ilaç dozları, hemşire tarafından ilaç uygulama akışı, 5 Doğru Kuralı doğrulaması, atlama/red/erteleme durumları ve denetim izi mekanizmalarını açıklar.

## Mimari ve İş Kuralları

1. **İlaç Dozu Planlama (Scheduling):**
   - Serbest ilaç adı/doz/yol girişi kabul edilmez. Planlama; hastaya ait, imzalı ve süresi dolmamış aktif reçeteden seçilen ilaç, doz ve uygulama yoluyla oluşturulur.
   - Reçete `Signed` veya `PartiallyDispensed` durumda olmalı; hasta, ilaç/etken madde, doz ve yol seçilen yatışla birebir eşleşmelidir. Eksik, başka hastaya ait veya pasif reçete `400 Validation Problem` ile reddedilir.
   - Yalnızca aktif yatışı bulunan hastalar (`Admitted` veya `Transferring`) için planlanabilir.

2. **5 Doğru Kuralı (5 Rights of Medication Administration):**
   - İlaç uygulaması (`Administer`) kaydedilmeden önce hemşirenin aşağıdaki 5 güvenlik kuralını açıkça doğrulaması zorunludur:
     1. **Doğru Hasta:** Hasta kimliği ve kol bandı/barkodu doğrulandı.
     2. **Doğru İlaç:** Order edilen ilaç adı, etken madde ve ambalajı kontrol edildi.
     3. **Doğru Doz:** Çekilen ve hazırlanan doz miktarı kontrol edildi.
     4. **Doğru Zaman:** Planlanan saat dilimi kontrol edildi.
     5. **Doğru Yol:** Uygulama yolu (Oral/IV/IM vs.) doğrulandı.
   - Sistem klinik karar desteği sağlamaz; yalnızca hemşire güvenlik kontrolünü ve uygulama kaydını denetler.

3. **Doz Durum Makinesi:**
   - `Scheduled` (Planlandı)
   - `Administered` (Uygulandı - uygulayan hemşire, uygulama zamanı ve 5 Doğru onayı ile)
   - `Skipped` (Atlandı - zorunlu atlanma gerekçesi ile)
   - `Refused` (Hasta Reddetti - hastanın red gerekçesi ile)
   - `Delayed` (Ertelendi - yeni planlanan zaman ve erteleme gerekçesi ile)
   - Uygulanmış (`Administered`) bir doz üzerinde tekrar atlama/red/erteleme işlemi yapılamaz.

4. **Denetim İzi (Audit Log):**
   - Tüm eMAR aksiyonları denetim izine kaydedilir:
     - `Inpatient.MedicationSchedule`
     - `Inpatient.MedicationAdminister`
     - `Inpatient.MedicationSkip`
     - `Inpatient.MedicationRefuse`
     - `Inpatient.MedicationDelay`

## API Endpoint'leri

- `POST /api/v1/inpatient/emar/schedule` — Yeni ilaç dozu planlama
- `POST /api/v1/inpatient/emar/{id}/administer` — 5 Doğru onayı ile ilacı uygulama
- `POST /api/v1/inpatient/emar/{id}/skip` — İlaç dozunu atlama
- `POST /api/v1/inpatient/emar/{id}/refuse` — Hastanın ilacı reddettiğini kaydetme
- `POST /api/v1/inpatient/emar/{id}/delay` — İlaç dozunu erteleme
- `GET /api/v1/inpatient/emar/orders/admission/{admissionId}` — Yatış hastasına ait aktif, imzalı reçete kalemleri
- `GET /api/v1/inpatient/emar/admission/{admissionId}` — Yatışın tüm eMAR kayıtları
- `GET /api/v1/inpatient/emar/due` — Yaklaşan/bekleyen planlanmış dozlar

## Web UI

- `/inpatient/emar` — Blazor eMAR sayfası:
  - Hasta seçimi ve hızlı aksiyonlar.
  - Aktif reçete kalemi seçimi; aktif order bulunmadığında planlama işleminin güvenli biçimde devre dışı bırakılması.
  - "Bekleyen & Planlanan Dozlar", "Uygulanan Dozlar", "Atlanan & Reddedilenler" sekmeleri.
  - "5 Doğru Güvenlik Doğrulaması" modali (5 checkbox işaretlenmeden buton aktif olmaz).
  - "Yeni Doz Planla", "Ertele", "Atla", "Hasta Reddetti" modalları.

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/EmarDomainTests.cs` (7 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/EmarComponentTests.cs` (3 test — order yok durumu, aktif order seçimi ve planlama isteği)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/EmarIntegrationTests.cs` (1 kapsamlı test — aktif reçete doğrulaması, yatış -> planlama -> 5 Doğru ile uygulama -> atlama -> red -> erteleme, iki istemcili uygulama yarışı ve yetkisiz negatif yol)
