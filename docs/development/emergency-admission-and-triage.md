# Acil Başvuru ve Triyaj Yönetimi Geliştirme Notları

Bu belge, **F08-G01 — Acil başvuru ve triyaj** görevinin mimari tasarımını, veri modelini, API sözleşmelerini, iş kurallarını ve test kapsamını açıklar.

---

## 1. Mimari ve Kapsam

Acil servis modülü (`HospitalManagement.Modules.Emergency`), hastaneye ayaktan (walk-in), ambulans (112) veya dış kurumdan sevk ile gelen hastaların acil başvuru kaydını, klinik simülasyon ve eğitim destekli triyaj seviyelendirmesini ve alan takibini yönetir.

### Temel Prensipler
- **İnsan Kararlı Triyaj:** Sistem sertifikalı HBYS veya yapay zeka klinik karar desteği değildir. Triyaj kategorisi (`Red1Resuscitation`, `Red2Emergency`, `YellowUrgent`, `GreenStandard`, `BlackExpectant`) ve gerekçesi sağlık personeli (hekim/hemşire) tarafından girilir (`EducationalClassificationAssisted: true`).
- **Sentetik DEMO Verisi:** Tüm acil protokolleri `DEMO-EMG-YYYYMMDD-XXXXXX` biçimindedir.
- **Aktif Başvuru İnvaryantı:** Bir hastanın aynı anda yalnızca tek bir aktif acil başvurusu bulunabilir (`WaitingTriage`, `TriagedWaitingDoctor`, `InEvaluation`, `InObservation`).

---

## 2. Veri Modeli ve Veritabanı Şeması

- **Veritabanı Şeması:** `emergency`
- **Geçmiş Tablosu:** `__EFMigrationsHistory_Emergency`
- **Tablo:** `EmergencyAdmissions`

### `EmergencyAdmission` Aggregate Root Özellikleri
- `Id: Guid`
- `EmergencyProtocolNumber: string (64)` (Tekil indeks)
- `PatientId: Guid` (İndeks: `(PatientId, Status)`)
- `ArrivalType: EmergencyArrivalType` (`WalkIn`, `Ambulance`, `TransferFromOtherFacility`)
- `ChiefComplaint: string (1000)` (Zorunlu)
- `AdmissionNotes: string? (2000)`
- `Status: EmergencyAdmissionStatus` (`WaitingTriage`, `TriagedWaitingDoctor`, `InEvaluation`, `InObservation`, `AdmittedToInpatient`, `AdmittedToIcu`, `Discharged`, `TransferredOut`, `LeftWithoutBeingSeen`, `Deceased`)
- `AdmittedAtUtc: DateTime`
- `AdmittingStaffId: Guid`
- `Triage: EmergencyTriageInfo` (Owned Entity):
  - `TriageLevel: TriageLevel` (`Red1Resuscitation`, `Red2Emergency`, `YellowUrgent`, `GreenStandard`, `BlackExpectant`)
  - `TriageCategoryReason: string (1000)`
  - `TriagedAtUtc: DateTime`
  - `TriageNurseId: Guid`
  - `EducationalClassificationAssisted: bool`
  - Vital Bulgular: `SystolicBp`, `DiastolicBp`, `HeartRate`, `BodyTemperatureCelsius`, `RespiratoryRate`, `OxygenSaturationPercent`, `PainScale`, `Consciousness`, `TriageClinicalNotes`
- `AssignedDoctorId: Guid?`
- `AssignedBedOrZone: string? (128)`
- `CompletedAtUtc: DateTime?`
- `DischargeOrDispositionNotes: string? (2000)`
- `xmin: uint` (PostgreSQL Optimistic Concurrency RowVersion)

---

## 3. API Uç Noktaları

| Metot | Yol | Yetki | Açıklama |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/emergency/admissions` | `emergency.triage.record` | Yeni acil başvuru kaydı oluşturur (`WaitingTriage`). |
| `POST` | `/api/v1/emergency/admissions/{id}/triage` | `emergency.triage.record` | Triyaj ve vital bulguları kaydeder (`TriagedWaitingDoctor`). |
| `POST` | `/api/v1/emergency/admissions/{id}/assign-doctor` | `emergency.triage.record` | Hekim ve alan/yatak atar (`InEvaluation`). |
| `POST` | `/api/v1/emergency/admissions/{id}/status` | `emergency.triage.record` | Durum günceller veya sonuçlandırır. |
| `GET` | `/api/v1/emergency/admissions` | Giriş Yapmış Kullanıcı | Durum, triyaj seviyesi ve hasta filtreli başvuru listesi. |
| `GET` | `/api/v1/emergency/admissions/{id}` | Giriş Yapmış Kullanıcı | Başvuru detayını ve triyaj bilgilerini getirir. |
| `GET` | `/api/v1/emergency/admissions/active/by-patient/{patientId}` | Giriş Yapmış Kullanıcı | Hastanın aktif acil başvurusunu getirir. |

---

## 4. Kullanıcı Arayüzü

- Sayfa: `src/HospitalManagement.Web.Client/Pages/Emergency/EmergencyAdmissions.razor` (`/emergency/admissions`)
- Özellikler:
  - Durum filtre kartları (Tümü, Triyaj Bekleyen, Hekim Bekleyen, Değerlendirmede, Gözlemde, Sonuçlananlar).
  - Renkli görsel triyaj rozetleri (Kırmızı 1, Kırmızı 2, Sarı, Yeşil, Siyah).
  - Modal pencereleri: Yeni Başvuru Kaydı, Triyaj Değerlendirme, Hekim/Alan Atama, Durum Güncelleme, Detay Görüntüleme.

---

## 5. Doğrulama ve Testler

- **Birim Testleri (`EmergencyAdmissionDomainTests.cs`):**
  - Başvuru oluşturma ve protokol formatı doğrulama.
  - Zorunlu şikâyet alanı kontrolü.
  - Triyaj kaydı ve eğitim simülasyon bayrağının atanması.
  - Hekim ataması ve `InEvaluation` durum geçişi.
  - Sonuçlanmış başvuru üzerinde değişiklik engeli.
- **Bileşen Testleri (`EmergencyAdmissionsComponentTests.cs`):**
  - Blazor arayüzünün listeleme, triyaj rozeti ve sayaç render doğrulaması.
- **Entegrasyon Testleri (`EmergencyAdmissionIntegrationTests.cs`):**
  - Tam PostgreSQL yaşam döngüsü: Ayaktan başvuru -> Triyaj kaydı -> Hekim atama -> Gözlem -> Şifa ile taburcu.
  - İnvaryant testleri: Aynı hasta için mükerrer aktif başvuru engeli (`409 Conflict`), boş şikâyet doğrulama hatası (`400 Bad Request`).
