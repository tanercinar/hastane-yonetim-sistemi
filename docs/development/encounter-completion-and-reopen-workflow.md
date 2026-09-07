# Muayene Tamamlama ve Yeniden Açma Akışı (Encounter Completion & Reopen Workflow - F04-G09)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G09 — Muayene tamamlama` görevine ait klinik kuralları, tamamlama için zorunlu alan doğrulamasını, kapanış kilidini ve gerekçeli yeniden açma (`Reopen`) mekanizmasını açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablo:** `encounters`
- **Varlık:** `Encounter`
- **Servis Arayüzü:** `IEncounterService`
- **Servis Uygulaması:** `EncounterService`
- **İlgili Uç Noktalar:**
  - `POST /api/v1/clinical-records/encounters/{id:guid}/complete`
  - `POST /api/v1/clinical-records/encounters/{id:guid}/reopen`

---

## 2. Muayene Tamamlama Kuralları (`CompleteEncounterAsync`)

Klinik karşılaşmanın sonlandırılması (`Completed`) için aşağıdaki kurallar zorunludur:

1. **Durum Uygunluğu:**
   - Yalnızca `InProgress` (Devam Ediyor) durumundaki karşılaşmalar tamamlanabilir.
2. **Klinik Belgelendirme Zorunluluğu (Completeness Check):**
   - Karşılaşmaya ait **en az bir İmzalı Klinik Not (`Signed` / `Amended`)** VEYA **en az bir Tanı (`EncounterDiagnosis`)** bulunmalıdır.
   - İmzalı not veya tanı olmadan boş karşılaşmanın kapatılması `400 Bad Request (clinicalCompleteness)` ile engellenir.
3. **Taslak Not Koruması:**
   - Karşılaşma bünyesinde henüz imzalanmamış taslak not (`Draft`) bulunuyorsa, tamamlama isteği `400 Bad Request (clinicalNotes)` hatası verir: *"Karşılaşmada henüz imzalanmamış taslak klinik notlar bulunmaktadır. Lütfen muayeneyi tamamlamadan önce tüm taslak notları imzalayın."*
4. **Zaman Damgası:**
   - `ActualEndTimeUtc` tamamlanma anı olarak mühürlenir ve versiyon artırılır.

---

## 3. Muayeneyi Yeniden Açma Mekanizması (`ReopenEncounterAsync`)

Tamamlanmış bir klinik karşılaşma sessizce değiştirilemez veya doğrudan üzerine yazılamaz. Yeniden açılması için:

1. **Yetki Denetimi:**
   - İstek `HospitalPermissions.ClinicalRecords.ClinicalNoteReopen` iznini gerektirir.
   - Yalnız `ChiefMedicalOfficer` rolündeki, karşılaşmanın bölümüne aktif atanmış kullanıcı yeniden açabilir; hekim veya sistem yöneticisi rolü tek başına klinik kaynağa erişim sağlamaz.
   - Oturumda son 5 dakika içinde yapılmış MFA doğrulamasını gösteren `amr=mfa` claim'i bulunmalıdır.
2. **Klinik Gerekçe Zorunluluğu:**
   - `ReopenReason` alanı boş bırakılamaz. Açıklayıcı gerekçe (örn: "Laboratuvar sonucu incelenerek tanı ve tedavi revize edilecek") zorunludur.
3. **Durum Değişimi:**
   - `Status: Completed -> InProgress`
   - `ReopenReason`, `ReopenedAtUtc`, `ReopenedByPractitionerId` alanları kalıcı olarak işlenir.
   - `ActualEndTimeUtc` sıfırlanır.
4. **Eşzamanlılık:**
   - İstek güncel `ExpectedVersion` değerini taşır. Eski sürümle yapılan yazma `409 Conflict` döndürür ve kayıt üzerine yazılmaz.

---

## 4. Denetim İzi (Audit Trail)

Her tamamlama ve yeniden açma işlemi `IAuditEventPublisher` aracılığıyla kaydedilir. Audit olayı işlem türü ve kaynak kimliğini taşır; klinik gerekçe veya not içeriği log mesajına kopyalanmaz:
- `ClinicalRecords.EncounterComplete`
- `ClinicalRecords.EncounterReopen`

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `EncounterDomainTests.cs` (13 test) — `Reopen` durum geçişi, gerekçesiz açma engeli, tamamlanmamış muayeneyi yeniden açma denemesi hatası.
- **Entegrasyon Testleri:** `ClinicalEncounterIntegrationTests.cs` (4 test) — boş muayene ve taslak not engelleri, imzadan sonra tamamlama, normal hekimin yeniden açma isteğinin `403 Forbidden` ile reddi ve sürüm kontrollü durum geçişleri.
- **Manuel Doğrulama:** Başhekim hesabında MFA etkinleştirme, son 5 dakika içinde MFA ile giriş ve gerekçeli yeniden açma akışı [`F04_Test.md`](../../F04_Test.md) üzerinden ayrıca doğrulanmalıdır.
- **Otomatik Paket Sonucu (2026-08-29):** Faz 4 filtreli PostgreSQL entegrasyon testleri 21/21; tam entegrasyon paketi 75/75 başarılıdır.
