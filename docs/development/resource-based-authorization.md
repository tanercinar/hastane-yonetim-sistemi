# Kaynak Kapsamlı Yetkilendirme (F02-G04)

Bu belge, Hastane Yönetim Sistemi projesinde uygulanan kaynak kapsamlı yetkilendirme (`Resource-Based Authorization`), bakım ilişkisi (`Care Relationship`) değerlendiricisi ve yatay IDOR/rol yükseltme önleme mekanizmalarını açıklar.

## 1. Karar Modeli

Yetkilendirme mimarisi [`docs/security/authorization-matrix.md`](../security/authorization-matrix.md) belgesine dayanır:

```text
Karar = Kimlik doğrulandı
      ∧ Hesap aktif ve oturum güvenli
      ∧ Gerekli Permission mevcut
      ∧ Resource Scope eşleşiyor
      ∧ Care Relationship / atama geçerli
      ∧ Amaç ve kayıt durumu eyleme izin veriyor
```

- **UUID IDOR Önlemi Değildir:** Kaynak tanımlayıcısının tahmin edilemez bir GUID olması tek başına güvenlik sağlamaz.
- **Rol Yetki Genişletmez:** `DOC + ADM` gibi birden çok role sahip kullanıcılarda izinler birleşir ancak kaynak kapsamı genişlemez. Doktor olan bir sistem yöneticisi, bakım ilişkisi bulunmayan bir hastanın klinik kaydına erişemez.

## 2. Kaynak Kapsamları (`ResourceScope`)

| Kapsam | Kod | Anlam ve Kontrol |
|---|---|---|
| Kendi | `Own` | Kaynağın konusu oturum açan hastadır (`PatientId == user.PersonId` veya `OwnerUserId == user.PersonId`). |
| Atanmış | `Assigned` | Personel kaynağa doğrudan görev/iş listesi üzerinden atanmıştır (`AssignedStaffId == user.PersonId`). |
| Bakım Ekibi | `CareTeam` | Hasta için `Own`; klinisyen (`DOC`, `NUR`, `CHM`) için aktif bakım ilişkisi (`CareRelationship`) gereklidir. |
| Bölüm | `Department` | Personel, kaynağın ait olduğu bölüme (`DepartmentId`) aktif atanmış olmalıdır (`StaffDepartmentAssignment`). |
| Şube | `Facility` | Personel, kaynağın ait olduğu şubeye (`FacilityId`) aktif atanmış olmalıdır. |
| Hastane | `Organization` | Kurum çapı yetki; açıkça tanımlanmadıkça verilmez. |
| Kimliksiz | `Deidentified` | Kimlik ve klinik ayrıntı içermeyen toplu/rapor modeli. |
| Sistem | `System` | Hasta verisi içermeyen teknik yönetim kaynağı. |

## 3. Bileşenler

- **`IResourceScoped` (`HospitalManagement.Contracts.Authorization`):** Kaynağın `PatientId`, `DepartmentId`, `FacilityId`, `AssignedStaffId`, `OwnerUserId` boyutlarını sağlayan sözleşme.
- **`ResourceScopedData` (`HospitalManagement.Contracts.Authorization`):** Scoped kaynak verilerini taşımak için immutable record.
- **`ICareRelationshipEvaluator` (`HospitalManagement.Contracts.Authorization`):** Personel-bölüm atamalarını (`OrganizationDbContext`) ve klinisyen-hasta bakım ilişkilerini denetleyen servis arayüzü.
- **`CareRelationshipRegistry` (`HospitalManagement.Host.Authorization`):** Aktif bakım ilişkilerini yöneten thread-safe kayıt defteri.
- **`ResourceScopeAuthorizationHandler` (`HospitalManagement.Host.Authorization`):** ASP.NET Core `AuthorizationHandler<ResourceScopeAuthorizationRequirement, IResourceScoped>` uygulaması.

## 4. Güvenlik Testleri ve IDOR Koruması

Otomatik testlerde şu negatif güvenlik senaryoları doğrulanır:

1. **Yatay IDOR (Hasta A -> Hasta B):** Hasta A, Hasta B'nin kimliği ile kaynak talep ettiğinde `403 Forbidden` döner.
2. **Klinik Sınır (İlişkisiz Doktor):** Bakım ilişkisi olmayan hekim hasta kaydına erişmeye çalıştığında `403 Forbidden` döner.
3. **Bölüm Sınırı (Farklı Bölüm Personeli):** Bir bölüme atanmış hemşire, başka bir bölümün kaynağına müdahale etmeye çalıştığında `403 Forbidden` döner.
4. **Yönetici Klinik Erişim Yasağı:** Sistem yöneticisi (`ADM`) klinik verilere erişmeye çalıştığında `403 Forbidden` döner.
