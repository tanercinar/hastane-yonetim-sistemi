# Yönetim Ekranları ve Rol/Kullanıcı Yönetimi (F02-G08)

Bu belge, Hastane Yönetim Sistemi projesinde Sistem Yöneticisi (`ADM` / `HospitalRoles.SystemAdministrator`) için geliştirilen kullanıcı etkinleştirme/devre dışı bırakma, rol atama, son yönetici koruması (`Last Admin Invariant`) ve denetim izi mekanizmalarını açıklar.

## 1. Mimari Kararlar ve Güvenlik Kuralları

- **Son Sistem Yöneticisi Koruması (`Last Admin Protection`):** Sistemde yetki boşluğu ve kilitlenme (lockout) oluşmaması için en az bir aktif `ADM` kullanıcısının bulunması zorunludur. Sistem yöneticisi kendi hesabını devre dışı bırakamaz ve sistemde başka aktif sistem yöneticisi kalmayacak şekilde `ADM` rolünü kaldıramaz.
- **Tüm Yönetimsel İşlemlerde Denetim İzi:** Kullanıcı durumunun değiştirilmesi (`Identity.UserEnable`, `AuditAction.UserDisable`) ve rollere atama yapılması (`AuditAction.RoleAssign`) her durumda `IAuditEventPublisher` aracılığıyla kriptografik olarak zincirlenen `audit_privacy.audit_logs` tablosuna kaydedilir.
- **Erişim Kontrolü:** Yönetim API uç noktaları (`/api/v1/identity/users*`), `HospitalPermissions.Identity.RoleAssign` ve `HospitalPermissions.Identity.UserDisable` izinleri gerektirir. Klinisyen, yardımcı sağlık veya hasta hesapları bu uç noktalara erişmeye çalıştığında sistem `403 Forbidden` döner.

## 2. API Uç Noktaları

| HTTP Metodu | URL Şablonu | Gerekli İzin | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/identity/users` | `Identity.RoleAssign` | Kullanıcı listesini sayfalama ve filtreleme ile döner. |
| `POST` | `/api/v1/identity/users/{id}/status` | `Identity.UserDisable` | Kullanıcıyı etkinleştirir veya devre dışı bırakır (Son yönetici korumalı). |
| `POST` | `/api/v1/identity/users/{id}/roles` | `Identity.RoleAssign` | Kullanıcının rollerini günceller (Son yönetici korumalı). |

## 3. Kullanıcı Arayüzü (`UserManagement.razor`)

- **Erişim Yolu:** `/admin/users`
- **Bileşen Özellikleri:**
  - `LoadingState`, `EmptyState`, `ErrorState` ve `ForbiddenState` durumlarını tam olarak yönetir.
  - E-posta ile dinamik filtreleme formu.
  - Rol etiketleri (badges) ve durum göstergesi.
  - Tek tıkla hesap etkinleştirme / devre dışı bırakma ve geri bildirim mesajları.

## 4. Test Kapsamı ve Doğrulama

- **Birim Testleri (`AdminUserManagementUnitTests`):**
  - Tek sistem yöneticisinin kendini devre dışı bırakması veya `ADM` rolünü kaldırmasının `ValidationFailed` ile reddedildiği kanıtlanır.
  - Normal personelin durum ve rol güncellemelerinin başarıyla `IAuditEventPublisher` logu ürettiği doğrulanır.
- **Bileşen Testleri (`UserManagementComponentTests`):**
  - bUnit ile `/admin/users` sayfasının yükleme, veri tablosu ve yetkisiz erişim (`ForbiddenState`) durumlarında doğru DOM ürettiği test edilir.
- **Entegrasyon Testleri (`AdminUserManagementIntegrationTests`):**
  - Canlı PostgreSQL üzerinde admin giriş akışı, kullanıcı listeleme, doktor durum değiştirme, son admin engeli (400 Bad Request) ve audit log kayıtlarının `audit_privacy.audit_logs` tablosundaki varlığı doğrulanır.
  - Klinisyen (doktor) hesabının yönetim uç noktalarına erişemediği (`403 Forbidden`) kanıtlanır.
