# Tanı Kataloğu ve Tanı Girişi (F04-G05)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G05 — Tanı kataloğu ve tanı girişi` görevine ait mimariyi, ICD-10 katalog modelini, kodlanmış ve serbest metin tanı semantiğini ve denetim modelini açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablolar:** `diagnosis_catalog_items`, `encounter_diagnoses`
- **Varlıklar:** `DiagnosisCatalogItem`, `EncounterDiagnosis`
- **Servis Arayüzü:** `IDiagnosisService`
- **Servis Uygulaması:** `DiagnosisService`

---

## 2. Demo ICD-10 Tanı Kataloğu (`DiagnosisCatalogItem`)

Sistemde uluslararası standartlara uygun demo ICD-10 alt kümesi bulunur:

- **Katalog Sürümü:** `ICD-10-TR-2026.1`
- **Alanlar:**
  - `Code`: Tanı kodu (Örn: `J03.9`, `I10`, `E11.9`, `M54.5`)
  - `NameTurkish`: Resmi Türkçe tanı adı
  - `NameEnglish`: Resmi İngilizce tanı adı
  - `Chapter`: ICD-10 Bölüm başlığı (Örn: `X - Solunum Sistemi Hastalıkları`)
  - `Block`: ICD-10 Blok başlığı (Örn: `J00-J06 Akut üst solunum yolu enfeksiyonları`)
  - `IsActive`: Kataloğun aktiflik durumu
- **Arama:** `GET /api/v1/clinical-records/diagnosis-catalog?query=...` uç noktası üzerinden `EF.Functions.ILike` ile büyük/küçük harf duyarsız katalog araması yapılır.

---

## 3. Karşılaşma Tanı Girişi (`EncounterDiagnosis`)

Hekim bir karşılaşma sırasında hem katalogdan standart kodlanmış tanı hem de klinik gereksinime göre serbest metin tanı girebilir:

- **Tanı Tipleri (`DiagnosisType`):**
  - `Preliminary` (1 - Ön Tanı)
  - `Differential` (2 - Ayırıcı Tanı)
  - `Final` (3 - Kesin Tanı)
  - `Secondary` (4 - İkincil / Ek Tanı)

- **Kodlanmış vs Serbest Metin Ayrımı:**
  - **Kodlanmış Tanı (`IsCoded = true`):** `Icd10Code` zorunludur ve katalogda doğrulanarak `CatalogVersion` damgalanır.
  - **Serbest Metin Tanı (`IsCoded = false`):** Kodlanması henüz mümkün olmayan veya nadir durumlar için `DiagnosisTitle` ve `Notes` ile kaydedilir; `Icd10Code = null` tutulur.

---

## 4. Değişiklik ve Hatalı Giriş Semantiği

- **Sessiz Silme Yasağı:** Tanı kayıtları tıbbi ve yasal kayıt olduğundan doğrudan silinemez.
- **Güncelleme (`Update`):** Yalnız kesinleşmemiş tanı güncellenebilir. `Final` tanı sessizce değiştirilemez; gerekçeli `EnteredInError` ve yeni kayıt akışı gerekir. Her mutasyon güncel `ExpectedVersion` taşır.
- **Hatalı Giriş (`MarkEnteredInError`):** Yanlış hasta veya hatalı kayıt durumunda gerekçeli (`EnteredInErrorReason`) olarak kilitlenir; kilitli tanı güncellenemez (`409 Conflict`).

---

## 5. Güvenlik ve Yetkilendirme

- **Katalog arama ve tanı yazma:** `DiagnosisRecord` izni gerekir; yazmada ayrıca karşılaşma katılımı/bakım ilişkisi aranır.
- **Görüntüleme:** `EncounterView + kaynak kapsamı` veya hastanın kendi kaydı. `EnteredInError` tanılar hastadan gizlenir; sistem yöneticisi klinik erişimi atlayamaz.

---

## 6. Denetim İzi (Audit Trail)

Her tanı işlemi `IAuditEventPublisher` üzerinden denetlenir:
- `ClinicalRecords.DiagnosisRecord`
- `ClinicalRecords.DiagnosisUpdate`
- `ClinicalRecords.DiagnosisEnteredInError`
- `ClinicalRecords.DiagnosisView`

---

## 7. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `DiagnosisDomainTests.cs` (6 test) — Kodlanmış tanı ICD-10 zorunluluğu, serbest metin tanı üretimi, güncelleme sürüm artışı, hatalı giriş kilidi (`InvalidOperationException`).
- **Entegrasyon Testleri:** `ClinicalDiagnosisIntegrationTests.cs` (2 test) — Katalogda "tonsillit" araması ve `J03.9` bulunması, kesin kodlu tanı ve serbest ayırıcı tanı ekleme, güncelleme, hatalı giriş kilidi ile güncelleme engeli (`409 Conflict`), hasta kendi tanılarını görüntüleme ve IDOR engelleme (`403 Forbidden`).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
