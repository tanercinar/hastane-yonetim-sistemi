# Yatan Hasta Yatış Kabul İş Akışı (Inpatient Admission Workflow)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G02 — Yatış kabulü** görevi için geliştirilen klinik yatış iş akışını, durum geçişlerini, iş kurallarını ve güvenlik/denetim mekanizmalarını açıklar.

## Durum Modeli (AdmissionStatus)

Yatış kaydı yaşam döngüsü aşağıdaki durumlardan geçer:

```
[Requested] (Hekim yatış istemi oluşturdu)
   │
   ├─► [Cancelled] (İptal edildi, gerekçe zorunlu)
   │
   ▼
[Accepted] (Servis/Yetkili yatışı onayladı, yatak atanması bekleniyor)
   │
   ├─► [Cancelled] (İptal edildi, rezerve yatak varsa serbest bırakılır)
   │
   ▼
[Admitted] (Hasta yatağa kabul edildi, yatak Occupied durumuna geçti)
   │
   ├─► [Transferring] (Transfer/Nakil sürecinde)
   │
   ▼
[Discharged] (Hasta taburcu edildi, taburculuk özeti ve epikriz eklendi)
```

## İş Kuralları ve Değişmezler (Invariants)

1. **Tek Aktif Yatış Kuralı (Single Active Admission):**
   - Bir hastanın sistemde aynı anda yalnızca bir adet aktif veya beklemede (`Requested`, `Accepted`, `Admitted`, `Transferring`) yatış kaydı bulunabilir.
   - İkinci bir aktif yatış uygulama kontrolü ve PostgreSQL kısmi unique index'i ile engellenir; eşzamanlı yarışın kaybedeni `409 Conflict` alır.

2. **Yatak Rezervasyonu ve Doluluk:**
   - İstem aşamasında opsiyonel olarak ilk yatak seçilebilir; seçilirse yatak `Reserved` durumuna geçer.
   - Hasta servise ulaşıp `AdmitPatientAsync` çağrıldığında yatak `Occupied` durumuna geçer, `CurrentAdmissionId` ve `CurrentPatientId` set edilir.
   - Yatış iptal edilirse, rezerve veya atanmış yatak otomatik olarak serbest bırakılır.
   - İstem bölümü seçilen servisin organizasyon bölümüyle, kabul yatağı da yatışın güncel servisiyle eşleşmelidir.

3. **Klinik Bakım Bilgileri:**
   - Diyet türü (`Standard`, `Diabetic`, `LowSodium`, `Renal`, `NPO`, `Soft`).
   - Düşme riski skoru (`FallRiskScore`, 0-100 ölçeği).
   - İzolasyon gereksinimi (`None`, `Contact`, `Droplet`, `Airborne`, `Protective`).

4. **Klinik Denetim İzi (Audit Logging):**
   - Her durum değişikliği ve bakım detayı güncellemesi `audit_privacy.audit_logs` tablosuna kaydedilir:
     - `Inpatient.AdmissionRequest`
     - `Inpatient.AdmissionAccept`
     - `Inpatient.AdmissionAdmit`
     - `Inpatient.AdmissionCancel`
     - `Inpatient.AdmissionCareDetailsUpdate`

5. **Eşzamanlılık (Optimistic Concurrency):**
   - `InpatientAdmission` ve `Bed` varlıkları `Version` concurrency token'ı ile korunur. Aktif hasta/yatak tekilliği veritabanı constraint'iyle desteklenir.

6. **Kaynak Yetkisi:**
   - API kararları rol adına göre değil permission + bölüm/bakım ilişkisine göre verilir. Sorumlu hekim istem bölümü dışında bir personel kimliğiyle değiştirilemez.

## API Endpoint'leri

- `POST /api/v1/inpatient/admissions` — Yeni yatış istemi oluşturma (`admission.request`)
- `POST /api/v1/inpatient/admissions/{id}/accept` — Yatış istemini kabul etme (`admission.accept`)
- `POST /api/v1/inpatient/admissions/{id}/admit` — Hastayı yatağa yerleştirme ve aktif etme (`bed.assign`)
- `POST /api/v1/inpatient/admissions/{id}/cancel` — Yatış istemini iptal etme
- `POST /api/v1/inpatient/admissions/{id}/care-details` — Bakım detaylarını güncelleme (`care-plan.manage`)
- `GET /api/v1/inpatient/admissions/{id}` — Yatış detayı sorgulama
- `GET /api/v1/inpatient/admissions/active/by-patient/{patientId}` — Hastanın aktif yatışını sorgulama
- `GET /api/v1/inpatient/admissions` — Servis, departman veya duruma göre yatış listesi filtreleme

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/AdmissionDomainTests.cs` (7 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/InpatientAdmissionsComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/InpatientAdmissionIntegrationTests.cs` ve `Phase7InpatientGateIntegrationTests.cs` (bölüm/servis ve servis/yatak uyumu, sorumlu hekim kapsamı, iki istemcili aktif yatış/yatak yarışları, kabul, bakım detayı ve audit)
