# Yatan Hasta Taburculuk ve Kurum Dışı Sevk (Discharge & Referral)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G07 — Taburculuk ve sevk** görevi için geliştirilen taburculuk özeti (epikriz), kesin tanı ve tedavi önerileri, taburculuk reçetesi özeti, poliklinik kontrol randevusu, kurum dışı sevk (MOCK) ve otomatik yatak tahliyesi/temizliğe alma mekanizmalarını açıklar.

## Mimari ve İş Kuralları

1. **Zorunlu Epikriz ve Taburculuk Doğrulaması:**
   - Bir yatış, hekim tarafından ayrıntılı bir taburculuk özeti (epikriz) ve kesin tanı açıklaması girilmeden kapatılamaz (`DischargeSummary` en az 20 karakter uzunluğunda olmalıdır).
   - Yalnızca aktif yatakta olan (`Admitted` veya `Transferring`) yatışlar taburcu edilebilir.

2. **Taburculuk Türleri (`DischargeType`):**
   - `Home`: Şifa / klinik düzelme ile evine taburcu.
   - `TransferToOtherFacility`: İleri düzey tetkik, cerrahi girişim veya yoğun bakım ihtiyacı nedeniyle başka bir sağlık kuruluşuna sevk (MOCK entegrasyon kaydı; hedef kurum adı ve sevk gerekçesi zorunludur).
   - `AgainstMedicalAdvice`: Tıbbi tavsiyeye rağmen hastanın/yakınının kendi isteği ve imzasıyla ayrılışı.
   - `Deceased`: Ex / vefat.

3. **Otomatik Yatak Tahliyesi ve Temizlik Durumu Geçişi:**
   - Taburculuk işlemi başarıyla onaylandığında, hastaya tahsisli olan yatak (`AssignedBedId`) atomik olarak `Occupied -> Cleaning` durumuna geçirilir.
   - Yatış kaydı `Status = AdmissionStatus.Discharged` olarak güncellenir ve yatak referansı temizlenir (`ClearBed()`).

4. **Denetim İzi (Audit Log):**
   - Hekim tarafından gerçekleştirilen taburculuk işlemi `Inpatient.AdmissionDischarge` denetim olayı ile kaydedilir.
   - Dış kuruma sevk durumunda ek olarak `Inpatient.ExternalReferralMock` denetim olayı fırlatılır.

## API Endpoint'leri

- `POST /api/v1/inpatient/discharges` — Taburculuk veya sevk işlemini onaylama ve yatağı temizliğe alma (Yetki: `HospitalPermissions.Inpatient.DischargeComplete`)
- `GET /api/v1/inpatient/discharges/{admissionId}` — Belirli bir yatışın taburculuk epikriz detayını getirme
- `GET /api/v1/inpatient/discharges` — Tamamlanmış taburculuk ve sevk kayıtlarını listeleme (hasta, tarih filtreleri ile)

## Web UI

- `/inpatient/discharges` — Blazor Taburculuk & Sevk Sayfası:
  - "Yatan Hastalar & Taburculuk İşlemleri" ve "Taburculuk & Sevk Geçmişi" sekmeleri.
  - "Taburculuk ve Sevk Formu" modali (Taburculuk türü seçimi, kesin tanı, epikriz metni, öneriler, reçete özeti, kontrol randevusu ve kurum dışı sevk alanları).
  - "Epikriz Özeti Görüntüleme" modali (Tamamlanmış taburculuk belgesinin salt-okunur formatlı görünümü).

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/InpatientDischargeDomainTests.cs` (4 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/InpatientDischargesComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/InpatientDischargeIntegrationTests.cs` (1 test — Yatış -> Yatak -> Şifa ile Taburculuk -> Yatak Temizlik Durumu Doğrulaması -> Dış Kurum Sevk Akışı -> Epikriz Sorgulama -> Yetkisiz Negatif Test)
