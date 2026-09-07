# Veri ve Denetim Bütünlüğü (F13-G05)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin klinik veri değişmezliğini (immutability), gerekçeli düzeltme mekanizmasını (ADR-0005), kriptografik denetim izi bütünlüğünü (audit hash chain), eşzamanlılık çakışma yönetimini (optimistic concurrency) ve KVKK/GDPR uyumlu veri saklama/anonimleştirme (retention & anonymization) süreçlerini belgeler.

## 2. Mimari ve Güvenlik Mekanizmaları

### 2.1. Klinik Kayıt Değişmezliği ve Gerekçeli Düzeltme (ADR-0005)
- **Taslak Sonrası Değişmezlik**: Taslak klinik notlar imzalandığında (`Status = Signed`) doğrudan güncelleme (`UpdateDraft`) veya silme işlemi yazılım seviyesinde kesin olarak engellenir (`InvalidOperationException`).
- **Gerekçeli Ek Not (Addendum)**: İmzalı bir not üzerinde yapılan tüm ilave veya düzeltmeler, orijinal nota (`ParentNoteId`) bağlı yeni bir ek not kaydı oluşturur. Hekimden zorunlu gerekçe (`CorrectionReason`) ve imza alınır.
- **Hatalı Giriş (Entered in Error)**: Yanlış hasta dosyasına girilen veya hatalı kaydedilen notlar veri tabanından fiziksel olarak silinmez. Durumu `EnteredInError` olarak işaretlenir; zorunlu gerekçe (`EnteredInErrorReason`) ve uygulayıcı kimliği kaydedilir. Hukuki ve adli denetimler için orijinal klinik metin korunur.

### 2.2. Kriptografik Denetim İzi Zinciri (Audit Tamper-Resistance)
- **Append-Only Denetim Günlüğü**: `audit_logs` tablosuna yalnızca ekleme yapılabilir (`PublishAsync`). Güncelleme veya silme API'si bulunmaz.
- **Kriptografik Blok Zinciri Modeli (Hash Chaining)**: Her denetim kaydı, kendi alanlarının (Id, zaman damgası, kullanıcı, kişi, eylem, hedef kaynak, sonuç, korelasyon) yanı sıra bir önceki kaydın özetini (`PreviousRecordHash`) de kapsayan bir SHA-256 özeti (`RecordHash`) ile imzalanır (`ComputeRecordHash`).
- **Kurcalama Tespiti (Tamper Verification)**: `entry.VerifyHashIntegrity()` fonksiyonu kaydın orijinal alanlarını yeniden özetleyerek saklanan `RecordHash` ile karşılaştırır. Veri tabanına doğrudan müdahale edilerek bir satır veya aktör değiştirilirse bütünlük kontrolü anında başarısız olur (`Assert.False`).

### 2.3. Eşzamanlı Güncelleme Çakışma Koruması (Optimistic Concurrency)
- Tüm kritik domain varlıkları (`Patient`, `ClinicalNote`, vb.) `IHasConcurrencyVersion` arayüzünü ve sayısal `Version` alanını uygular.
- Eşzamanlı yazma durumlarında çakışan istek `DbUpdateConcurrencyException` fırlatarak yakalanır ve istemciye güvenli `409 Conflict` ("Kayıt başka bir kullanıcı tarafından değiştirildi") dönülür.

### 2.4. Veri Saklama ve KVKK Anonimleştirme (Retention & Anonymization)
- **Klinik Bağlam ve İlişkisel Bütünlük**: Bir hastanın yasal saklama süresi dolduğunda veya KVKK uyarınca anonimleştirme talebi geldiğinde hasta kaydı silinmez; çünkü fiziksel silme geçmiş klinik karşılaşma, epikriz ve denetim kayıtlarını öksüz (orphan) bırakır.
- **Geri Döndürülemez Kimliksizleştirme (`Patient.Anonymize`)**:
  - `FirstName` → `"ANONİM"`, `LastName` → `"HASTA"`
  - `NationalIdSynthetic`, `PhoneNumber`, `Email`, `Address`, `EmergencyContact` alanları tamamen `null` yapılır.
  - İletişim tercihleri kapatılır ve hesap pasifleştirilir (`IsActive = false`).
  - `Id`, `PersonId` ve `MedicalRecordNumber` korunarak adli/klinik rapor bütünlüğü ve denetim izi bozulmadan veri imhası gerçekleştirilir.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Security/DataAndAuditIntegrityTests.cs`:
  - `ClinicalSignedNoteCannotBeMutatedDirectlyAndRequiresAddendum`
  - `ClinicalNoteEnteredInErrorRequiresExplicitReasonAndPreservesRecord`
  - `AuditLogChainedHashIntegrityDetectsTampering`
  - `AuditLogPreviousRecordHashChainsSequentialEntries`
  - `PatientDataRetentionAnonymizationWipesPiiWhilePreservingRelationalIntegrity`
- 472 birim testi ve 13 mimari testi %100 başarılıdır.
