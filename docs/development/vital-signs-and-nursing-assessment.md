# Vital Bulgular ve Hemşire Ön Değerlendirmesi (F04-G03)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G03 — Vital bulgular ve hemşire ön değerlendirmesi` görevine ait mimariyi, fizyolojik doğrulama kurallarını, veri modellerini ve denetim semantiğini açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablo:** `vital_sign_observations`
- **Varlık:** `VitalSignObservation`
- **Servis Arayüzü:** `IVitalSignsService`
- **Servis Uygulaması:** `VitalSignsService`

---

## 2. Vital Bulgu Tipleri ve Doğrulama Kuralları

`VitalSignValidationRules` sınıfı, fizyolojik olarak imkânsız veya hatalı birim/değer girişlerini API ve domain düzeyinde reddeder.

| Ölçüm Tipi (`VitalSignType`) | Birim | Kabul Edilen Fizyolojik Aralık | Standart Klinik Yorumlama (`VitalInterpretation`) |
|---|---|---|---|
| `BodyTemperature` | °C | 25.0 – 45.0 | <35.0 CriticalLow, 35.0-35.9 Low, 36.0-37.5 Normal, 37.6-38.5 High, >38.5 CriticalHigh |
| `BloodPressureSystolic` | mmHg | 40 – 300 | <70 CriticalLow, 70-89 Low, 90-120 Normal, 121-140 High, >140 CriticalHigh |
| `BloodPressureDiastolic` | mmHg | 20 – 200 | <50 CriticalLow, 50-59 Low, 60-80 Normal, 81-90 High, >90 CriticalHigh |
| `HeartRate` | bpm | 20 – 300 | <50 CriticalLow, 50-59 Low, 60-100 Normal, 101-130 High, >130 CriticalHigh |
| `RespiratoryRate` | /dk | 4 – 80 | <8 CriticalLow, 8-11 Low, 12-20 Normal, 21-30 High, >30 CriticalHigh |
| `OxygenSaturation` | % | 40 – 100 | <90 CriticalLow, 90-94 Low, 95-100 Normal |
| `BodyWeight` | kg | 0.2 – 500 | Antropometrik kayıt |
| `BodyHeight` | cm | 20 – 260 | Antropometrik kayıt |
| `BodyMassIndex` (BMI) | kg/m² | 5 – 100 | Boy ve kilo girildiğinde otomatik hesaplanır |
| `BloodGlucose` | mg/dL | 10 – 1200 | <54 CriticalLow, 54-69 Low, 70-140 Normal, 141-250 High, >250 CriticalHigh |
| `PainScore` | skor | 0 – 10 | 0-3 Normal, 4-7 High, 8-10 CriticalHigh |

---

## 3. Hemşire Panel Değerlendirmesi (`VitalSignsPanel`)

Hemşire veya poliklinik personeli, tek bir panel isteğiyle (`/api/v1/clinical-records/vital-signs/panel`) hastanın tüm vital bulgularını (Ateş, TA, Nabız, Solunum, SpO2, Kilo, Boy, Şeker, Ağrı, Bilinç Durumu) tek seferde ve atomik olarak kaydedebilir. Boy ve kilo birlikte girildiğinde BMI değeri otomatik hesaplanarak panele eklenir.

---

## 4. Değişiklik ve Hatalı Giriş Semantiği

- **Sessiz Silme/Üzerine Yazma Yasağı:** Vital bulgular klinik delil niteliğinde olduğundan doğrudan silinemez.
- **Hatalı Giriş (`MarkEnteredInError`):** Hatalı prob veya hasta karışıklığı durumunda gerekçe (`EnteredInErrorReason`) ile işaretlenir; `IsEnteredInError = true` olarak kilitlenir.
- **Denetim İzi (Audit Trail):** Her panel ve tekil kayıt, görüntüleme ve hatalı giriş işlemi `IAuditEventPublisher` ile denetlenir:
  - `ClinicalRecords.VitalSignRecord`
  - `ClinicalRecords.VitalSignPanelRecord`
  - `ClinicalRecords.VitalSignEnteredInError`
  - `ClinicalRecords.VitalSignView`

---

## 5. Güvenlik ve Yetkilendirme

- Kayıt için `ObservationRecordVital` izni ve karşılaşma katılımı/bakım ilişkisi birlikte gerekir; rol adı tek başına yeterli değildir.
- Hatalı giriş işlemi güncel `ExpectedVersion` ister ve bayat yazmayı `409 Conflict` ile reddeder.
- Okuma `EncounterView + kaynak kapsamı` ile, hasta okuması yalnız kendi kaydıyla sınırlıdır. `EnteredInError` ölçümler hastaya gösterilmez; sistem yöneticisi klinik kapsamı atlayamaz.

---

## 6. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `VitalSignDomainTests.cs` (19 test) — Fizyolojik aralık dışı değerlerin tespiti, otomatik yorumlama (`VitalInterpretation`) hesaplaması, hatalı giriş kilidi.
- **Entegrasyon Testleri:** `ClinicalVitalSignsIntegrationTests.cs` (3 test) — Hemşire panel kaydı ve otomatik BMI üretimi, fizyolojik imkânsız değer reddi (400 Bad Request), doktor ve hasta yetkili okuma, IDOR engelleme (403 Forbidden).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
