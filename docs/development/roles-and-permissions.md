# Rol ve İzin Kataloğu (F02-G03)

Bu belge, Hastane Yönetim Sistemi projesinde uygulanan rol ve izin kataloğunu, yetkilendirme ilkelerini (policy) ve çalışma mekanizmasını açıklar.

## 1. Genel Mimari ve Karar Modeli

Yetkilendirme mimarisi [`docs/security/authorization-matrix.md`](../security/authorization-matrix.md) ve [`docs/adr/ADR-0004-identity-and-native-oidc.md`](../adr/ADR-0004-identity-and-native-oidc.md) belgelerine dayanır:

- **Roller izin gruplarıdır (Permission Bundles):** Roller doğrudan kaynak erişimi vermez; izinlerin gruplanmasını sağlar.
- **Kanonik İzinler:** Sistemde 65 kanonik izin tanımlıdır (`HospitalPermissions`).
- **Varsayılan Ret (Default Deny):** Bilinmeyen ya da sistemde tanımlı olmayan herhangi bir izin ilkesi (`policy`) istendiğinde, sistem varsayılan olarak talebi `403 Forbidden` ile reddeder.
- **Kullanıcı İzinleri:** Kullanıcı oturum açtığında, `HospitalUserClaimsPrincipalFactory` aracılığıyla kullanıcının rollerine ait izinler `permission` claim'i olarak `ClaimsPrincipal` nesnesine yüklenir.

## 2. Rol Kataloğu (`HospitalRoles`)

Sistemde 12 rol bulunur ve `identity_access.roles` tablosuna deterministik UUID'lerle seed edilir:

| Kod | Rol Adı | Açıklama |
|---|---|---|
| `PAT` | Hasta | Yalnızca kendi portal kaynaklarına erişebilir. |
| `DOC` | Doktor | Bakım ilişkisi ve ataması bulunan hastalarda klinik işlemleri yürütür. |
| `NUR` | Hemşire | Atandığı veya bölümündeki hastalarda hemşirelik ve bakım işlemlerini yürütür. |
| `CHM` | Başhekim | Klinik rol yetkilerine ek olarak bölüm gözetimi ve özel onayları yürütür. |
| `REG` | Kayıt/Danışma Personeli | Demografi, randevu, check-in ve sıra işlemlerini yürütür; klinik içerik göremez. |
| `LAB` | Laboratuvar Personeli | Laboratuvar iş listesi, numune ve teknik sonuç işlemlerini yürütür. |
| `RAD` | Radyoloji Personeli | Görüntüleme iş listesi, çekim ve rapor işlemlerini yürütür. |
| `PHA` | Eczacı | Reçete doğrulama, ilaç teslimi ve eczane stok işlemlerini yürütür. |
| `ADM` | Sistem Yöneticisi | Hesap, rol, izin ve organizasyon yönetimini yürütür; klinik içerik göremez. |
| `MGR` | Hastane Yöneticisi | Kimliksiz ve minimum operasyonel raporları görüntüler; klinik düzenleme yapamaz. |
| `FIN` | Muhasebe Personeli | Gelecek rolü; bu sürümde işlev ve izni bulunmaz. |
| `HR` | İnsan Kaynakları Personeli | Gelecek rolü; bu sürümde işlev ve izni bulunmaz. |

## 3. İzin Kataloğu (`HospitalPermissions`)

İzinler alan bazlı sınıflandırılmıştır:
- `Identity`: `identity.profile.view-own`, `identity.profile.edit-own`, `identity.user.invite-staff`, `identity.user.disable`, `identity.role.assign`, `identity.permission.assign`
- `Organization`: `organization.view`, `organization.manage`
- `Patient`: `patient.view-own`, `patient.search`, `patient.demographics.view`, `patient.demographics.create`, `patient.demographics.edit`
- `Appointment`: `appointment.view-own`, `appointment.book-own`, `appointment.manage-own`, `appointment.manage`, `appointment.schedule.manage`, `appointment.check-in`
- `ClinicalRecords`: `encounter.view`, `encounter.start`, `encounter.complete`, `observation.record-vital`, `clinical-note.edit-draft`, `clinical-note.sign`, `clinical-note.correct`, `clinical-note.reopen`, `diagnosis.record`, `consultation.request`, `consultation.respond`, `clinical-attachment.upload`
- `Pharmacy`: `prescription.create`, `prescription.sign`, `prescription.cancel`, `prescription.dispense`, `inventory.pharmacy.view`, `inventory.pharmacy.adjust`
- `Diagnostics`: `diagnostic-order.create`, `laboratory.worklist.view`, `laboratory.specimen.transition`, `laboratory.result.edit-draft`, `laboratory.result.finalize`, `radiology.worklist.view`, `radiology.study.complete`, `radiology.report.finalize`, `blood-bank.transfusion.record`, `diagnostic-result.view-final-own`
- `Inpatient`: `admission.request`, `admission.accept`, `bed.assign`, `bed.transfer`, `care-plan.manage`, `medication.administer`, `discharge.complete`, `emergency.triage.record`, `surgery.schedule`, `critical-care.record`
- `ReportingAndAudit`: `report.operations.view`, `report.operations.export`, `audit.technical.view`, `audit.clinical-access.view`, `audit.own-access.view`

## 4. Yetkilendirme Bileşenleri

- **`HospitalAuthorizationPolicyProvider`:** İstenen policy adı `HospitalPermissionCatalog.IsKnown` ile doğrulanırsa `PermissionAuthorizationRequirement(permission, isKnown: true)` ile policy üretilir. Bilinmeyen izinler için `isKnown: false` ile oluşturularak varsayılan olarak reddedilir.
- **`PermissionAuthorizationHandler`:** `isKnown == false` ise veya kullanıcının ilgili `permission` claim'i yoksa yetkilendirme başarısız olur (`403 Forbidden`).
- **`AuthorizationEndpointExtensions`:** Minimal API ve controller endpoint'leri `.RequirePermission(HospitalPermissions.Identity.ProfileViewOwn)` biçiminde korunabilir.
