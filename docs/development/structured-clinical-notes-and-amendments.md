# Yapılandırılmış Klinik Notlar ve Değişiklik/Ek Not Semantiği (F04-G04)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G04 — Klinik notlar` görevine ait mimariyi, yapılandırılmış not yapısını, imza ve ek not (addendum) yaşam döngüsünü ve denetim semantiğini açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablo:** `clinical_notes`
- **Varlık:** `ClinicalNote`
- **Servis Arayüzü:** `IClinicalNoteService`
- **Servis Uygulaması:** `ClinicalNoteService`

---

## 2. Klinik Not Tipleri ve SOAP Yapısı

`ClinicalNote` varlığı hem serbest metin hem de standart tıbbi SOAP (Subjective, Objective, Assessment, Plan) formatını destekler:

- **Not Tipleri (`ClinicalNoteType`):**
  - `GeneralSoap` (Genel Poliklinik SOAP Muayene Notu)
  - `ProgressNote` (Klinik Takip / Vizit Notu)
  - `Consultation` (Konsültasyon Notu)
  - `DischargeSummary` (Taburculuk Özeti)
  - `NurseNote` (Hemşire Bakım Notu)
  - `Addendum` (İmzalı Nota Ek / Düzeltme Notu)

- **SOAP Alanları:**
  - `ChiefComplaint` (Ana Şikayet)
  - `HistoryOfPresentIllness` (Hikaye / Anamnez)
  - `PhysicalExamination` (Fizik Muayene Bulguları)
  - `Assessment` (Klinik Değerlendirme / Ön Tanı)
  - `Plan` (Tedavi ve Takip Planı)
  - `Content` (Serbest veya birleşik not metni)

---

## 3. Not Durum Makinesi ve İmzalı Not Değişmezliği

```
   [Taslak (Draft)]
         │
         ├──► Güncelleme (UpdateDraft) ──► [Draft (Sürüm artar)]
         │
         ├──► İmzalama (Sign) ──────────► [İmzalı (Signed)]
         │                                       │
         ▼                                       ├──► Ek Not Ekleme (CreateAddendum) ──► [Amended (Orijinal)] + [Signed (Yeni Addendum)]
   [Hatalı Giriş (EnteredInError)]               │
                                                 ▼
                                           [Hatalı Giriş (EnteredInError)]
```

### Değişmezlik Kuralları:
1. **Sessiz Güncelleme Yasağı:** Bir not `Signed` durumuna geçtikten sonra `UpdateDraft` çağrılamaz (`409 Conflict`).
2. **Ek Not / Düzeltme (Addendum):** İmzalı bir kayda ekleme veya açıklama yapılması gerektiğinde `CreateAddendum` kullanılır. Orijinal notun durumu `Amended` olur ve `ParentNoteId` ile yeni imzalı bir `Addendum` notu üretilir.
3. **Hatalı Giriş (Entered In Error):** Yanlış hasta veya yanlış dosya durumunda not gerekçesiyle (`EnteredInErrorReason`) kilitlenir.

---

## 4. Güvenlik ve Yetkilendirme

- Taslak, imza ve düzeltme işlemleri sırasıyla `ClinicalNoteEditDraft`, `ClinicalNoteSign` ve `ClinicalNoteCorrect` izniyle birlikte karşılaşma katılımı/bakım ilişkisi gerektirir.
- Düzenleme, imza, ek not ve hatalı giriş istekleri `ExpectedVersion` taşır; kayıp güncelleme girişimi `409 Conflict` olur.
- Hasta yalnız kendi imzalı/değişiklik eklenmiş notlarını görebilir; taslak ve `EnteredInError` notlar gizlenir. Sistem yöneticisi rolü klinik kayda erişim sağlamaz.

---

## 5. Denetim İzi (Audit Trail)

Her not işlemi `IAuditEventPublisher` üzerinden denetlenir:
- `ClinicalRecords.ClinicalNoteDraftCreate`
- `ClinicalRecords.ClinicalNoteDraftUpdate`
- `ClinicalRecords.ClinicalNoteSign`
- `ClinicalRecords.ClinicalNoteAddendum`
- `ClinicalRecords.ClinicalNoteEnteredInError`
- `ClinicalRecords.ClinicalNoteView`

---

## 6. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `ClinicalNoteDomainTests.cs` (7 test) — Taslak oluşturma, güncelleme, imzalama, imzalı notu değiştirme yasağı, ek not üretimi ve orijinal notun `Amended` durumuna geçişi, hatalı giriş kilidi.
- **Entegrasyon Testleri:** `ClinicalNotesIntegrationTests.cs` (2 test) — Hekim taslak oluşturma -> güncelleme -> imzalama -> imzalı notu doğrudan güncelleme denemesinde `409 Conflict` -> ek not ekleme ve `Amended` geçişi; hemşire bakım notu oluşturma, hasta kendi notunu görüntüleme ve yetkisiz erişimde `403 Forbidden`.
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
