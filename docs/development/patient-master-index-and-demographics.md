# Hasta Ana Kaydı ve Demografik Veri Modeli (F03-G01)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 (Hasta Kaydı, Randevu ve Hasta Portalı) kapsamındaki **Hasta Ana Kaydı (Patient Master Index / Demographics)** mimarisini, veri modelini, mükerrer kayıt kontrolünü ve erişim yetkilendirme kurallarını açıklar.

## 1. Mimari Prensipler ve Güvenlik Sınırları

- **Modül Ayrımı (ADR-0001):** Hasta kimliği ve demografik verileri `src/Modules/Patients/` altında izoledir. Yalnızca `HospitalManagement.BuildingBlocks` referansına sahiptir.
- **Kişi ve Kullanıcı Ayrımı (`PersonId` vs `UserId`):** `Patient` kaydı `PersonId` ile bağlanır. Kimlik doğrulama hesabı (`ApplicationUser`) ile demografik hasta kaydı `PersonId` üzerinden ilişkilendirilir.
- **Hasta Numarası (`MedicalRecordNumber`):** Kurum içi benzersiz sentetik format (`MRN-YYYY-XXXXXX`).
- **Kaynak Kapsamlı Yetkilendirme ve IDOR Koruması:**
  - Hasta (`Patient` rolü / `HospitalPermissions.Patient.ViewOwn`) yalnızca kendi `PersonId`'sine ait profil kaydını görüntüleyebilir (`/api/v1/patients/by-person/{personId}`).
  - Başka bir hastanın `id` veya `personId` değeriyle yapılan sorgulamalar `403 Forbidden` ile engellenir.
  - Kayıt personeli (`RegistrationStaff` / `HospitalPermissions.Patient.DemographicsCreate`) ve hekim/hemşire (`HospitalPermissions.Patient.DemographicsView`) yetkisi kapsamında arama yapabilir ve detayları inceleyebilir.
- **Eşzamanlılık Koruması (`Optimistic Concurrency`):** `IHasConcurrencyVersion` üzerinden her güncellemede `Version` alanı artırılır. İstemci `If-Match` başlığı veya `ExpectedVersion` ile çakışmaları (`409 Conflict`) tespit eder.
- **Denetim İzi:** Hasta oluşturma (`AuditAction.PatientRegister`), güncelleme (`AuditAction.PatientDemographicsEdit`), görüntüleme (`AuditAction.PatientView`) ve arama (`AuditAction.PatientSearch`) olayları `IAuditEventPublisher` aracılığıyla kriptografik hash'li log tablosuna kaydedilir.

## 2. API Uç Noktaları

| HTTP Metodu | URL Şablonu | Gerekli İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/patients` | `Patient.DemographicsCreate` | Yeni hasta kaydı oluşturur. |
| `POST` | `/api/v1/patients/duplicate-check` | `Patient.DemographicsCreate` | Ad/soyad/doğum tarihi veya sentetik kimlik ile mükerrer kontrolü yapar. |
| `GET` | `/api/v1/patients/{id}` | `Patient.DemographicsView` veya `Patient.ViewOwn` | Hasta detayını getirir (yetkisiz hastaya 403). |
| `GET` | `/api/v1/patients/by-person/{personId}` | `Patient.ViewOwn` veya `Patient.DemographicsView` | Kişi kimliği üzerinden hasta profilini getirir. |
| `PUT` | `/api/v1/patients/{id}` | `Patient.DemographicsEdit` | Hasta demografik bilgilerini günceller (`If-Match` destekli). |
| `GET` | `/api/v1/patients` | `Patient.Search` | Sayfalamalı ve terim filtreli hasta arama listesi döner. |

## 3. Test ve Doğrulama

- **Birim Testleri (`PatientDomainUnitTests`):**
  - Hasta varlığı oluşturma, alan kırpma ve büyük harf MRN normalizasyonu.
  - Demografi güncelleme ve `UpdatedAtUtc` zaman damgası.
  - Sentetik TC ve telefon maskeleme (`PatientMaskingHelper`).
  - Deterministik MRN formatlayıcı.
- **Entegrasyon Testleri (`PatientIntegrationTests`):**
  - Canlı PostgreSQL üzerinde kayıt personeli ile mükerrer tespiti ve yeni hasta kaydı oluşturma (`201 Created`).
  - `If-Match` ile versiyon çakışması (`409 Conflict`) senaryosu.
  - Hekim tarafından arama ve detay görüntüleme.
  - Hasta portalı üzerinden kendi kaydına erişim (`200 OK`) ve başka hastaya IDOR erişim denemesinin engellenmesi (`403 Forbidden`).
  - Denetim loglarının `audit_privacy.audit_logs` tablosundaki varlığı.
