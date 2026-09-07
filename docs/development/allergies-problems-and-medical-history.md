# Alerji, Problem ve Özgeçmiş Modeli (F04-G02)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G02 — Alerji, problem ve özgeçmiş` görevine ait mimariyi, veri modellerini, durum semantiğini ve güvenlik ilkelerini açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Varlıklar:**
  - `AllergyIntolerance` (`allergy_intolerances` tablosu)
  - `ClinicalProblem` (`clinical_problems` tablosu)
- **Servis Arayüzü:** `IAllergyProblemService`
- **Servis Uygulaması:** `AllergyProblemService`

---

## 2. Alerji ve İntolerans Modeli (`AllergyIntolerance`)

Alerji ve intolerans kayıtları, hastanın bilinen ilaç, gıda, çevresel veya biyolojik alerjenlerini izler:

- **Kategoriler (`AllergyCategory`):** `Food`, `Medication`, `Environment`, `Biologic`, `Other`
- **Kritiklik Seviyesi (`AllergyCriticality`):** `Low`, `High`, `UnableToAssess`
- **Klinik Durum (`AllergyClinicalStatus`):** `Active`, `Inactive`, `Resolved`
- **Doğrulama Durumu (`AllergyVerificationStatus`):** `Suspected`, `Confirmed`, `Refuted`, `EnteredInError`

### Değişiklik ve Hatalı Giriş Semantiği
- **Sessiz Güncelleme/Silme Yasağı:** Kayıtlar doğrudan veritabanından silinmez.
- **Durum Güncelleme:** Hekim/klinisyen tarafından durum `Active` durumundan `Inactive` veya `Resolved` durumuna geçirilebilir; her işlemde `Version` artırılır ve denetim izi üretilir.
- **Hatalı Giriş (`EnteredInError`):** Yanlış hastaya girilen veya hatalı tespit edilen alerjiler gerekçe (`EnteredInErrorReason`) ile işaretlenir ve kilitlenir.

---

## 3. Problem Listesi ve Tıbbi Geçmiş (`ClinicalProblem`)

Hastanın aktif sorunları, kronik hastalıkları, geçmiş cerrahi ve tıbbi operasyonları ile aile öyküsü yapılandırılmış olarak tutulur:

- **Kategoriler (`ProblemCategory`):**
  - `ActiveProblem`: Aktif poliklinik / servis problemi
  - `ChronicCondition`: Kronik hastalık (ör. Tip 2 DM, Hipertansiyon)
  - `PastMedicalHistory`: Geçmiş hastalık öyküsü
  - `SurgicalHistory`: Geçmiş ameliyat / operasyon öyküsü
  - `FamilyHistory`: Aile öyküsü (ör. ailede koroner arter hastalığı)
- **Klinik Durum (`ProblemClinicalStatus`):** `Active`, `Inactive`, `Resolved`, `InRemission`
- **Doğrulama Durumu (`ProblemVerificationStatus`):** `Provisional`, `Differential`, `Confirmed`, `Refuted`, `EnteredInError`

---

## 4. Yetkilendirme ve Denetim İzi (Audit)

- Rol adı tek başına klinik erişim sağlamaz. Yazma için `DiagnosisRecord` veya `ObservationRecordVital` izni ile aktif karşılaşma katılımı/bakım ilişkisi birlikte aranır.
- Durum güncelleme ve hatalı giriş işlemleri güncel `ExpectedVersion` ister; bayat yazma `409 Conflict` olur.
- Okuma için `EncounterView` ve kaynak kapsamı gerekir. Hasta yalnız kendi kaydını görebilir; `EnteredInError` kayıtları hastadan gizlenir. Sistem yöneticisine klinik kayıt kestirmesi verilmez.
- **Denetim Olayları:**
  - `ClinicalRecords.AllergyCreate`, `ClinicalRecords.AllergyUpdate`, `ClinicalRecords.AllergyEnteredInError`, `ClinicalRecords.AllergyView`
  - `ClinicalRecords.ProblemCreate`, `ClinicalRecords.ProblemUpdate`, `ClinicalRecords.ProblemEnteredInError`, `ClinicalRecords.ProblemView`

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri:**
  - `AllergyDomainTests.cs` (5 test) — Durum geçişleri, invariantlar, hatalı giriş kilidi.
  - `ProblemDomainTests.cs` (5 test) — Durum geçişleri, çözülme tarihi doğrulaması, hatalı giriş kilidi.
- **Entegrasyon Testleri:**
  - `ClinicalAllergyAndProblemIntegrationTests.cs` (3 test) — Doktor tarafından alerji/problem kaydı, çözümleme, hatalı giriş işaretleme, hasta portalından kendi kayıtlarını görüntüleme, yetkisiz hasta yazma ve çapraz hasta IDOR engelleme (403 Forbidden).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
