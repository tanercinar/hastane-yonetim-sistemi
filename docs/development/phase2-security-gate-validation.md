# Faz 2 Güvenlik Kapısı Doğrulaması (F02-KAPI)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 2 (Kimlik, Yetkilendirme ve Güvenlik Altyapısı) için tamamlanan güvenlik kapısı testlerini, rol/izin sınırlarını ve kanıtlanan güvenlik güvencelerini açıklar.

## 1. Güvenlik Kapısı Hedefi ve Kapsamı

[`ROADMAP.md`](../../ROADMAP.md) Faz 2 kabul kriterleri uyarınca, sistemdeki tüm aktör rolleri (`Anonymous`, `Patient`, `Doctor`, `Nurse`, `Pharmacist`, `LaboratoryStaff`, `RadiologyStaff`, `RegistrationStaff`, `SystemAdministrator`, `HospitalManager`) için kimlik ve yetkilendirme sınırları API düzeyinde kanıtlanmıştır.

## 2. Kanıtlanan Güvenlik Sınırları ve Matris Çiftleri

| Aktör | Uç Nokta / İşlem | Beklenen Sonuç | Güvenlik Gerekçesi |
|---|---|---|---|
| **Anonymous** | `GET /api/v1/identity/session` | `401 Unauthorized` | Oturum açmamış istekler reddedilir (Default Deny). |
| **Anonymous** | `GET /api/v1/identity/users` | `401 Unauthorized` | Yönetim uç noktalarına anonim erişim engellenir. |
| **Anonymous** | `POST (CSRF headersız)` | `400 Bad Request` | `IdentityAntiforgeryEndpointFilter` CSRF korumasını zorunlu tutar. |
| **Patient** | `GET /api/v1/identity/session` | `200 OK` (`AccountKind=Patient`) | Hasta kendi portal oturumunu ve profilini görebilir. |
| **Patient** | `GET /api/v1/identity/users` | `403 Forbidden` | Hasta yönetim uç noktalarına erişemez. |
| **Patient** | `GET /api/v1/platform/auth-probes/clinical-note-sign` | `403 Forbidden` | Hasta klinik not imzalayamaz. |
| **Doctor / Nurse** | `GET /api/v1/platform/auth-probes/clinical-note-sign` | `200 OK` | Klinisyen klinik bakım işlemlerini yürütebilir. |
| **Doctor / Nurse** | `GET /api/v1/identity/users` | `403 Forbidden` | Klinisyen kullanıcı ve rol yönetimi yapamaz. |
| **Allied Health** (Eczacı, Laborant, vb.) | `GET /api/v1/identity/users` | `403 Forbidden` | Yardımcı sağlık personeli yönetim işlemlerine erişemez. |
| **Allied Health** (Eczacı, Danışma, vb.) | `GET /api/v1/platform/auth-probes/clinical-note-sign` | `403 Forbidden` | Yardımcı sağlık personeli doğrudan hekim klinik notu imzalayamaz. |
| **System Administrator** | `GET /api/v1/identity/users` | `200 OK` | Yönetici kullanıcı listesi ve rol atamalarını yönetebilir. |
| **System Administrator** | `GET /api/v1/platform/auth-probes/clinical-note-sign` | `403 Forbidden` | Yönetici klinik hasta verisi göremez ve klinik not imzalayamaz (ADR-0002 / ADR-0004). |
| **Tüm Roller** | `audit_privacy.audit_logs` | SHA-256 Tamper Free | Tüm kimlik ve yetki olayları zincirlenir; tahrifat doğrulama testi (`VerifyHashIntegrity`) %100 geçer. |

## 3. Otomatik Test Kanıtı

- **Entegrasyon Testi (`Phase2SecurityGateIntegrationTests`):** Canlı PostgreSQL üzerinde tüm allow/deny çiftleri, CSRF token zorunluluğu, çerez güvenlik bayrakları ve SHA-256 denetim izi kurcalama bütünlüğü uçtan uca doğrulanmıştır.
