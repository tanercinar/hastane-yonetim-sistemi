# Hemşire Gözlem ve Bakım Planı (Nursing Observations & Care Plans)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G05 — Hemşire gözlem ve bakım planı** görevi için geliştirilen periyodik vital bulgular, sıvı dengesi (giriş/çıkış - I&O), hemşirelik tanıları, bakım hedefleri, görev takibi ve düzeltme (correction) akışlarını açıklar.

## Mimari ve İş Kuralları

1. **Vital Bulgular ve Fizyolojik Sınırlar:**
   - Sistolik / Diyastolik Tansiyon (mmHg)
   - Nabız (bpm), Solunum Sayısı (/dk), Vücut Sıcaklığı (°C), Oksijen Satürasyonu (%SpO2), Ağrı Skalası (0-10), Bilinç Durumu (`Alert`, `Voice`, `Pain`, `Unresponsive` - AVPU).
   - Geçersiz değerler (örn. Sistolik < 30 veya > 300) domain seviyesinde doğrulanır.

2. **Sıvı Dengesi (Giriş / Çıkış Takibi):**
   - Oral Giriş (mL), IV Sıvı Girişi (mL)
   - İdrar Çıkışı (mL), Dren Çıkışı (mL), Diğer Çıkışlar (mL).

3. **Klinik Kayıt Değiştirilemezliği ve Düzeltmeler (Corrections):**
   - Klinik kayıtlar doğrudan güncellenemez veya sessizce silinemez.
   - Hatalı giriş durumunda hemşire zorunlu bir gerekçe (`CorrectionReason`) belirterek düzeltme kaydı (`IsCorrection = true`, `CorrectedObservationId`) oluşturur.
   - Tüm işlemler denetim izine (`Inpatient.ObservationRecord`, `Inpatient.ObservationCorrection`) kaydedilir.

4. **Hemşirelik Bakım Planı ve Görev Yaşam Döngüsü:**
   - **Bakım Planı:** Hemşirelik Tanısı (örn. "Düşme Riski", "Doku Bütünlüğü", "Sıvı Volüm Fazlalığı"), Bakım Hedefi ve Durum (`Active`, `Resolved`, `Discontinued`).
   - **Bakım Görevleri:** Sıklık (`Q2H`, `Q4H`, `Q8H`, `Daily`, `PRN`), Planlanan Vakit (`DueTimeUtc`), Durum (`Pending`, `Completed`, `Overdue`, `Cancelled`).
   - Geç kalmış görevler otomatik olarak `Overdue` durumuna çekilir ve klinik panoda/arayüzde acil uyarı olarak sunulur.

## API Endpoint'leri

- `POST /api/v1/inpatient/nursing/observations` — Vital ve klinik gözlem kaydı oluşturma
- `POST /api/v1/inpatient/nursing/observations/{id}/correct` — Gözlem düzeltme kaydı ekleme
- `GET /api/v1/inpatient/nursing/observations` — Yatış bazlı gözlem geçmişi
- `POST /api/v1/inpatient/nursing/care-plans` — Bakım planı oluşturma
- `POST /api/v1/inpatient/nursing/care-plans/{id}/tasks` — Bakım planına görev ekleme
- `POST /api/v1/inpatient/nursing/tasks/{id}/complete` — Görevi tamamlama
- `POST /api/v1/inpatient/nursing/tasks/{id}/cancel` — Görevi iptal etme
- `GET /api/v1/inpatient/nursing/care-plans` — Yatış bazlı bakım planları ve görevleri
- `GET /api/v1/inpatient/nursing/tasks/overdue` — Gecikmiş bakım görevleri listesi

## Web UI

- `/inpatient/nursing` — Blazor hemşire gözlem ve bakım sayfası:
  - Hasta seçimi ve hızlı aksiyon butonları.
  - "Vital Bulgular & Sıvı Dengesi" sekmesi, gözlem geçmişi tablosu ve "Düzeltme Ekle" modali.
  - "Bakım Planları & Görevler" sekmesi, tanı/hedef kartları, görev listesi ve "Görevi Tamamla" / "Görevi İptal Et" aksiyonları.
  - "Geciken Görevler" uyarı sekmesi ve hızlı tamamlama.

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/NursingCareDomainTests.cs` (7 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/NursingCareComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/NursingCareIntegrationTests.cs` (1 test — Yatış -> Vital Kaydı -> Düzeltme -> Bakım Planı -> Görev Tamamlama -> Yetkisiz erişim kontrolü)
