# Klinik Ekler ve Güvenli Blob Depolama (F04-G07)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G07 — Klinik ekler` görevine ait mimariyi, blob depolama soyutlamasını, güvenlik ve dosya doğrulama kurallarını (path traversal, MIME spoofing, magic byte kontrolleri) ve denetim modelini açıklar.

---

## 1. Mimari Genel Bakış

- **Soyutlama:** `HospitalManagement.BuildingBlocks.Storage.IBlobStorageService`
- **Uygulama:** `InMemoryBlobStorageService` (Prodüksiyon için S3/Azure Blob/MinIO ile değiştirilebilir)
- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablo:** `clinical_attachments`
- **Varlık:** `ClinicalAttachment`
- **Güvenlik Doğrulayıcı:** `AttachmentSecurityValidator`
- **Servis Arayüzü:** `IClinicalAttachmentService`
- **Servis Uygulaması:** `ClinicalAttachmentService`

---

## 2. Güvenlik ve Dosya Doğrulama Mekanizması (`AttachmentSecurityValidator`)

1. **Path Traversal Koruması (`SanitizeFileName`):**
   - Kullanıcı tarafından gönderilen dosya adlarındaki `../`, `..\`, `:`, `*`, `?`, `"`, `<`, `>`, `|` ve `\0` (null byte) gibi zararlı karakterler temizlenir.
   - Sadece güvenli dosya taban adı ve uzantısı saklanır.
   - Depolama anahtarı sunucu tarafında GUID ve hasta kimliği ile güvenli biçimde üretilir: `{patientId}/{attachmentId}_{sanitizedFileName}`.

2. **Boyut Sınırı:**
   - Tek dosya için maksimum izin verilen boyut **15 MB** (15,728,640 bayt) olarak sınırlandırılmıştır.

3. **İzin Verilen Formatlar ve Magic Byte Doğrulaması:**
   - İzin verilen uzantılar: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.webp`, `.dcm`, `.dicom`.
   - **MIME / İmza Sahteciliği (Spoofing) Koruması:**
     - PDF: `%PDF-` (`0x25 0x50 0x44 0x46`) kontrolü.
     - JPEG: `0xFF 0xD8 0xFF` kontrolü.
     - PNG: `0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A` kontrolü.
     - WebP: `RIFF....WEBP` kontrolü.
     - DICOM: 128 bayt preamble sonrasındaki `DICM` Part 10 imza kontrolü.
   - **Zararlı Kod / Çalıştırılabilir Dosya Engeli:** `MZ` (DOS/PE Executable) başlığı taşıyan tüm dosyalar uzantısı ne olursa olsun derhal reddedilir.
   - Bildirilen MIME türü doğrulanan dosya türüyle birebir eşleşmelidir.
   - `MockAttachmentMalwareScanner`, DEMO/MOCK ortamında PE başlığı ve EICAR test işaretini reddeder; gerçek antivirüs entegrasyonu değildir.

4. **Bütünlük Kontrolü:**
   - Yüklenen her dosyanın `SHA-256` özeti hesaplanarak veritabanında saklanır.

---

## 3. Klinik Ek Veri Modeli (`ClinicalAttachment`)

- `Id`: Benzersiz ek kimliği
- `EncounterId`: Karşılaşma kimliği
- `PatientId`: Hasta kimliği
- `UploadedByPractitionerId`: Dosyayı yükleyen sağlık personeli
- `AttachmentType`: Ek türü (`LabReport = 1`, `RadiologyImage = 2`, `DischargeReport = 3`, `ConsentForm = 4`, `Other = 5`)
- `FileName`: Güvenli dosya adı
- `StorageKey`: Blob depolama anahtarı
- `ContentType`: Doğrulanmış MIME türü
- `ByteSize`: Dosya boyutu
- `Sha256Checksum`: SHA-256 sağlama özeti
- `Description`: Açıklama
- `UploadedAtUtc`: Yüklenme zamanı
- `IsEnteredInError` & `EnteredInErrorReason`: Hatalı giriş durumu ve gerekçesi
- `Version`: Optimistik eşzamanlılık sürümü

---

## 4. Yetkilendirme ve Gizlilik

- **Yükleme:** `ClinicalAttachmentUpload` izni, açık karşılaşma ve katılım/bakım ilişkisi birlikte gerekir.
- **İndirme ve Görüntüleme:**
  - `EncounterView + kaynak kapsamı` veya kendi ekini görüntüleyen hasta.
  - Başka hastaya ait ek indirilmeye çalışıldığında IDOR engellenir (`403 Forbidden`).
- **Hatalı Giriş İşaretleme:** `ClinicalNoteCorrect + kaynak kapsamı` ve güncel `ExpectedVersion` gerekir. Sistem yöneticisine klinik kestirme erişim yoktur.

---

## 5. Denetim İzi (Audit Trail)

- `ClinicalRecords.AttachmentUpload`
- `ClinicalRecords.AttachmentDownload`
- `ClinicalRecords.AttachmentView`
- `ClinicalRecords.AttachmentEnteredInError`

---

## 6. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `AttachmentDomainTests.cs` (7 test) — Path traversal temizliği, geçerli PDF kabulü ve SHA-256 hesaplama, sahte PDF (MIME spoofing) reddi, çalıştırılabilir dosya (EXE) reddi, izin verilmeyen uzantı reddi, 15 MB aşımı reddi, hatalı giriş kilidi.
- **Entegrasyon Testleri:** `ClinicalAttachmentsIntegrationTests.cs` (2 test) — `multipart/form-data` ile dosya yükleme, path traversal temizliğinin doğrulanması, byte-for-byte stream indirme, EXE sahteciliği engelleme (`400 Bad Request`), hasta kendi ekini indirme ve IDOR engelleme (`403 Forbidden`).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
