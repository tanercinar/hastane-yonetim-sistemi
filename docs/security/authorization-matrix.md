# Yetkilendirme Matrisi

## Amaç ve karar modeli

Bu belge ürünün erişim kontrolü kaynağıdır. Rol tek başına erişim vermez. Her istek aşağıdaki bileşimle değerlendirilir:

```text
Karar = Kimlik doğrulandı
      ∧ Hesap aktif ve oturum güvenli
      ∧ Gerekli Permission mevcut
      ∧ Resource Scope eşleşiyor
      ∧ Care Relationship / atama geçerli
      ∧ Amaç ve kayıt durumu eyleme izin veriyor
```

Varsayılan karar **reddet**tir. Arayüzde düğmenin gizlenmesi yetkilendirme sayılmaz; aynı karar API'de uygulanır. UUID kullanmak IDOR önlemi değildir.

## Rol kataloğu

| Kod | Rol | Açıklama |
|---|---|---|
| PAT | Hasta | Yalnızca kendi portal kaynakları |
| DOC | Doktor | Bakım ilişkisi/ataması bulunan hastalarda klinik işlemler |
| NUR | Hemşire | Atandığı/bölümündeki hastalarda hemşirelik işlemleri |
| CHM | Başhekim | Klinik rol + sorumlu olduğu bölümde gözetim ve özel onaylar |
| REG | Kayıt/Danışma Personeli | Demografi, randevu, check-in ve sıra; klinik içerik yok |
| LAB | Laboratuvar Personeli | Laboratuvar iş listesi, numune ve teknik sonuç |
| RAD | Radyoloji Personeli | Görüntüleme iş listesi, çekim, rapor; alt görev permission ile ayrılır |
| PHA | Eczacı | Geçerli reçete, gerekli güvenlik özeti, teslim ve eczane stoğu |
| ADM | Sistem Yöneticisi | Hesap, rol, izin, organizasyon; varsayılan klinik içerik yok |
| MGR | Hastane Yöneticisi | Kimliksiz/minimum operasyonel rapor; klinik düzenleme yok |
| FIN | Muhasebe Personeli | Gelecek rolü; bu sürümde işlev ve uygulama permission'ı yok |
| HR | İnsan Kaynakları Personeli | Gelecek rolü; bu sürümde işlev ve uygulama permission'ı yok |

Bir kullanıcı birden çok role sahip olabilir; permission birleşir ancak kaynak kapsamı genişlemez. Örneğin `DOC + ADM`, doktorun ilgisiz hastalara erişmesini sağlamaz ve ADM rolüne klinik veri vermez.

## Kaynak kapsamları

| Kod | Kapsam | Anlam |
|---|---|---|
| OWN | Kendi | Kaynağın konusu oturum açan hastadır |
| ASSIGNED | Atanmış | Kullanıcı encounter, yatış, iş listesi veya görev üzerinden atanmıştır |
| CARE_TEAM | Bakım ekibi | Aktif bakım ilişkisi ve gereken klinik amaç vardır |
| DEPARTMENT | Bölüm | Kaynak, kullanıcının aktif görev yaptığı/sorumlu olduğu bölümdedir |
| FACILITY | Şube | Kaynak, kullanıcının atanmış olduğu şubededir |
| ORGANIZATION | Hastane | Kurum çapı; özellikle verilmedikçe kullanılmaz |
| DEIDENTIFIED | Kimliksiz | Kimlik ve klinik ayrıntı içermeyen aggregate/read model |
| SYSTEM | Sistem | Hasta verisine gerek duymayan teknik yönetim kaynağı |

## Eylem ayrımı

Her kaynak için tek `Manage` izni kullanılmaz. Gereken ölçüde şu fiiller ayrılır:

- `View`, `Search`, `Create`, `EditDraft`, `Sign`, `Correct`, `Cancel`
- `Assign`, `Accept`, `Complete`, `Approve`, `Reopen`
- `Dispense`, `Administer`, `Publish`, `Export`, `AuditView`
- `RoleAssign`, `PermissionAssign`, `AccountDisable`

## Özet rol–alan matrisi

İşaretler: `✓` izin + kapsam koşuluyla, `R` salt okuma, `A` özel onay/gerekçe/MFA ile, `—` erişim yok.

| Alan | PAT | DOC | NUR | CHM | REG | LAB | RAD | PHA | ADM | MGR | FIN | HR |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Kendi portal profili | ✓ OWN | — | — | — | — | — | — | — | — | — | — | — |
| Hasta demografi arama/görme | — | R CARE_TEAM | R ASSIGNED | R DEPARTMENT | ✓ FACILITY | Minimum iş listesi | Minimum iş listesi | Minimum teslim | — | — | — | — |
| Hasta demografi düzenleme | Sınırlı OWN | — | — | A DEPARTMENT | ✓ FACILITY | — | — | — | — | — | — | — |
| Randevu | ✓ OWN | R/✓ ASSIGNED | R ASSIGNED | R DEPARTMENT | ✓ FACILITY | — | R iş listesi | — | — | Aggregate | — | — |
| Check-in/sıra | R OWN | R ASSIGNED | R ASSIGNED | R DEPARTMENT | ✓ FACILITY | — | R iş listesi | — | — | Aggregate | — | — |
| Klinik özet/zaman çizelgesi | Sınırlı OWN | R CARE_TEAM | R ASSIGNED | R DEPARTMENT | — | Minimum | Minimum | Minimum | — | — | — | — |
| Vital/ön değerlendirme | R yayımlanırsa | ✓ CARE_TEAM | ✓ ASSIGNED | ✓ DEPARTMENT | — | — | — | — | — | Aggregate | — | — |
| Klinik not/tanı | Sınırlı OWN | ✓ CARE_TEAM | Sınırlı R/✓ | ✓ DEPARTMENT | — | — | R rapor bağlamı | — | — | — | — | — |
| İmzalı kaydı yeniden açma | — | — | — | A DEPARTMENT | — | — | — | — | — | — | — | — |
| Konsültasyon | Sınırlı OWN | ✓ CARE_TEAM | R ASSIGNED | ✓ DEPARTMENT | — | — | — | — | — | Aggregate | — | — |
| Reçete yazma/imzalama | R OWN | ✓ CARE_TEAM | R ASSIGNED | ✓ DEPARTMENT | — | — | — | R | — | Aggregate | — | — |
| Reçete teslimi | R OWN | R CARE_TEAM | R ASSIGNED | R DEPARTMENT | — | — | — | ✓ ASSIGNED | — | Aggregate | — | — |
| Klinik ilaç/sarf stoğu | — | R CARE_TEAM | R ASSIGNED | R DEPARTMENT | — | — | — | ✓ DEPARTMENT | — | Aggregate | — | — |
| Laboratuvar istemi | R OWN | ✓ CARE_TEAM | R ASSIGNED | ✓ DEPARTMENT | — | — | — | — | — | Aggregate | — | — |
| Numune/teknik sonuç | R final OWN | R CARE_TEAM | R ASSIGNED | R DEPARTMENT | — | ✓ ASSIGNED | — | — | — | Aggregate | — | — |
| Radyoloji istemi/raporu | R final OWN | ✓/R CARE_TEAM | R ASSIGNED | ✓/R DEPARTMENT | — | — | ✓ ASSIGNED | — | — | Aggregate | — | — |
| Yatış/yatak/transfer | R OWN | ✓ CARE_TEAM | ✓ ASSIGNED | ✓ DEPARTMENT | Sınırlı ✓ | — | — | — | — | Aggregate | — | — |
| Bakım planı/eMAR | Sınırlı R OWN | R/✓ CARE_TEAM | ✓ ASSIGNED | ✓ DEPARTMENT | — | — | — | Minimum | — | Aggregate | — | — |
| Acil/ameliyat/ICU | Sınırlı OWN | ✓ CARE_TEAM | ✓ ASSIGNED | ✓ DEPARTMENT | Demografi | İş listesi | İş listesi | Minimum | — | Aggregate | — | — |
| Operasyonel dashboard | Kendi | Kapsamlı R | Kapsamlı R | R DEPARTMENT | R FACILITY | R DEPARTMENT | R DEPARTMENT | R DEPARTMENT | Teknik | R DEIDENTIFIED | — | — |
| Kullanıcı/rol/izin | Kendi hesap | — | — | — | — | — | — | — | ✓ SYSTEM | — | — | — |
| Audit görüntüleme | Kendi güvenlik olayları | Sınırlı | Sınırlı | A DEPARTMENT | Sınırlı | Sınırlı | Sınırlı | Sınırlı | Teknik audit | Kimliksiz | — | — |
| Klinik/rapor export | Kendi belge | A kapsamlı | — | A DEPARTMENT | Sınırlı | A iş sonucu | A rapor | A teslim | — | A DEIDENTIFIED | — | — |

“Minimum” ilgili görevi yapmak için gereken kimlik/uyarı/istem alanlarıdır; tüm hasta profili veya encounter notu değildir.

## Permission kataloğu

### Kimlik ve organizasyon

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `identity.profile.view-own` | Tümü | OWN |
| `identity.profile.edit-own` | Tümü | OWN; güvenlik kritik alan step-up doğrulama ister |
| `identity.user.invite-staff` | ADM | SYSTEM, MFA |
| `identity.user.disable` | ADM | SYSTEM, MFA; son ADM korunur |
| `identity.role.assign` | ADM | SYSTEM, MFA, audit |
| `identity.permission.assign` | ADM | SYSTEM, MFA; izin kataloğunda tanımlı değer |
| `organization.view` | Personel | Atandığı FACILITY/DEPARTMENT |
| `organization.manage` | ADM | SYSTEM; klinik sorumluluk vermez |

Bildirim tercihleri de bu iki OWN iznini kullanır: `GET /api/v1/notifications/preferences`
`identity.profile.view-own`, `PUT /api/v1/notifications/preferences` ise
`identity.profile.edit-own` gerektirir. Hedef `PersonId` istemciden alınmaz; oturum claim'inden çözülür.

### Hasta ve randevu

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `patient.view-own` | PAT | OWN, portal projection |
| `patient.search` | REG, DOC, NUR, CHM | Minimum filtre, rate limit, FACILITY/CARE_TEAM |
| `patient.demographics.view` | REG, DOC, NUR, CHM | Görev için minimum, kapsam koşullu |
| `patient.demographics.create` | REG | FACILITY, sentetik veri kontrolü |
| `patient.demographics.edit` | REG, CHM | FACILITY/DEPARTMENT, gerekçe/audit |
| `appointment.view-own` | PAT | OWN |
| `appointment.book-own` | PAT | OWN, uygun slot |
| `appointment.manage-own` | PAT | OWN, zaman/durum kuralı |
| `appointment.manage` | REG | FACILITY |
| `appointment.schedule.manage` | DOC, CHM, REG | Kendi takvimi / DEPARTMENT / FACILITY |
| `appointment.check-in` | REG | FACILITY |

### Klinik kayıt

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `encounter.view` | DOC, NUR, CHM | CARE_TEAM/ASSIGNED/DEPARTMENT |
| `encounter.start` | DOC | ASSIGNED/CARE_TEAM, geçerli randevu/acil |
| `encounter.complete` | DOC | Katılımcı, zorunlu kayıtlar |
| `observation.record-vital` | NUR, DOC | ASSIGNED/CARE_TEAM |
| `clinical-note.edit-draft` | DOC, NUR | Kendi yetkili not tipi |
| `clinical-note.sign` | DOC, NUR | Kendi not tipi, step-up gerekebilir |
| `clinical-note.correct` | DOC, CHM | Yazar/DEPARTMENT; gerekçe ve sürüm |
| `clinical-note.reopen` | CHM | DEPARTMENT, MFA, gerekçe |
| `diagnosis.record` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `consultation.request` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `consultation.respond` | DOC | ASSIGNED |
| `clinical-attachment.upload` | DOC, NUR | CARE_TEAM/ASSIGNED, dosya politikası |

### Reçete, eczane ve stok

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `prescription.create` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `prescription.sign` | DOC, CHM | Kendi taslağı, geçerli encounter |
| `prescription.cancel` | DOC, CHM | Yazar/DEPARTMENT, gerekçe |
| `prescription.dispense` | PHA | ASSIGNED/FACILITY, geçerli imzalı reçete |
| `inventory.pharmacy.view` | PHA, CHM | DEPARTMENT |
| `inventory.pharmacy.adjust` | PHA | DEPARTMENT, gerekçe/audit |

### Tanısal hizmetler

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `diagnostic-order.create` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `laboratory.worklist.view` | LAB | ASSIGNED/DEPARTMENT |
| `laboratory.specimen.transition` | LAB | ASSIGNED; durum makinesi |
| `laboratory.result.edit-draft` | LAB | ASSIGNED |
| `laboratory.result.finalize` | Yetkili LAB | ASSIGNED, rol içi credential/permission |
| `radiology.worklist.view` | RAD | ASSIGNED/DEPARTMENT |
| `radiology.study.complete` | Yetkili RAD | ASSIGNED |
| `radiology.report.finalize` | Radyolog alt izni | ASSIGNED |
| `blood-bank.transfusion.record` | DOC, NUR, CHM | CARE_TEAM/ASSIGNED; yalnız tahsis edilmiş ve çıkışı yapılmış ürün |
| `diagnostic-result.view-final-own` | PAT | OWN, yayın politikası |

### Yatış ve kritik bakım

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `admission.request` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `admission.accept` | DOC, NUR, CHM, sınırlı REG | ASSIGNED/DEPARTMENT; işlev ayrıştırılır |
| `bed.assign` | NUR, CHM, yetkili REG | DEPARTMENT/FACILITY |
| `bed.transfer` | NUR, CHM | ASSIGNED/DEPARTMENT |
| `care-plan.manage` | NUR | ASSIGNED |
| `medication.administer` | NUR | ASSIGNED, aktif order |
| `discharge.complete` | DOC, CHM | CARE_TEAM/DEPARTMENT |
| `emergency.triage.record` | Yetkili NUR/DOC | ASSIGNED/DEPARTMENT, insan kararı |
| `surgery.schedule` | DOC, CHM | DEPARTMENT, ekip/oda uygunluğu |
| `critical-care.record` | DOC, NUR | ASSIGNED/CARE_TEAM |

### Uzmanlık bakımı

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `specialty-care.view` | DOC, NUR, CHM | CARE_TEAM/ASSIGNED; yalnız yetkili klinik projeksiyon |
| `specialty-care.record` | DOC, NUR, CHM | CARE_TEAM/ASSIGNED; dikey iş kuralı ve geçerli Encounter |
| `specialty-care.manage` | DOC, CHM | CARE_TEAM/DEPARTMENT; bakım planı ve sevk yönetimi |
| `specialty-care.view-own` | PAT | OWN; sunucuda Person→Patient çözümleme, yalnız yayınlanmış/minimum portal projeksiyonu |

### Rapor, audit ve export

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `report.projection.manage` | ADM | SYSTEM; yalnız teknik checkpoint/rebuild, rapor içeriği yok |
| `report.operations.view` | Klinik roller, MGR | Rol kapsamı veya DEIDENTIFIED |
| `report.operations.export` | CHM, MGR, belirli bölüm rolleri | MFA, satır sınırı, audit, kapsam |
| `audit.technical.view` | ADM | SYSTEM; klinik içerik yok |
| `audit.clinical-access.view` | CHM, güvenlik inceleme rolü ileride | DEPARTMENT, gerekçe/MFA |
| `audit.own-access.view` | PAT | OWN için güvenli özet |

### Birlikte çalışabilirlik MOCK yönetimi ve export

| Permission | Roller | Kapsam/koşul |
|---|---|---|
| `interoperability.mock.manage` | ADM | SYSTEM; yalnız MOCK ayarı, circuit ve minimize teknik log; klinik içerik yok |
| `interoperability.fhir.export` | CHM | DEMO hasta düzeyi export; hasta/ADM deny, gerçek veri bağlantısından önce CARE_TEAM/DEPARTMENT ve audit/MFA zorunlu |

## Alan bazlı veri minimizasyonu

- **REG:** Tanı, not, reçete ayrıntısı, laboratuvar sonucu ve görüntü görmez.
- **LAB:** Numune için hasta doğrulama alanları, istem ve gerekli klinik gerekçe; genel notları görmez.
- **RAD:** Çekim güvenliği için gerekli istem/uyarı bilgisi; ilgisiz klinik geçmişi görmez.
- **PHA:** Reçete, hasta doğrulama, alerji/etkileşim için minimum özet; genel encounter notu görmez.
- **ADM:** Kullanıcı kimliği, hesap durumu, rol ve atama; klinik kayıt ve rapor görmez.
- **MGR:** Aggregate/kimliksiz operasyon verisi; permission olmadan isimli klinik detaya geçemez.

## Yüksek riskli işlemler

Aşağıdakiler MFA veya yakın zamanda doğrulanmış oturum, açık gerekçe ve audit ister:

- Rol/permission atama, personel daveti ve hesap devre dışı bırakma
- İmzalı klinik kaydı yeniden açma veya hatalı giriş sayma
- Onaylı sonucu düzeltme
- Büyük veya hassas veri export'u
- Audit erişimi
- Retention/imha işlemi
- Acil erişim (ileride ayrıca onaylanırsa)

## Bilinçli olarak uygulanmayan break-glass

Bu sürümde “acil durumda tüm hasta kayıtlarını aç” mekanizması yoktur. Acil serviste de normal bakım ilişkisi ve atama oluşturulur. Gerçek ürün ihtiyacı doğarsa break-glass ayrı tehdit modeli, MFA, gerekçe, anlık bildirim ve sonradan zorunlu incelemeyle tasarlanmalıdır.

## Zorunlu negatif test çiftleri

Her izin için en az bir allow ve bir deny testi bulunur. Özellikle:

1. Hasta A, Hasta B'nin profil/randevu/reçete/sonuç kimliğini kullanamaz.
2. Doktor, bakım ilişkisi olmayan hastayı yalnız rolü sayesinde göremez.
3. Hemşire başka bölümün hastasına vital/eMAR yazamaz.
4. Eczacı klinik not endpoint'ine erişemez.
5. LAB, radyoloji görüntüsüne; RAD, laboratuvar teknik düzenleme endpoint'ine erişemez.
6. ADM klinik veri endpoint'ini açamaz; DOC rol atayamaz.
7. MGR aggregate rapordan kimlikli detay endpoint'ine geçemez.
8. Kullanıcı istemci alanına başka `PatientId/DepartmentId/Role` yazarak kapsamı genişletemez.
9. SignalR group adı göndererek başka bölüm kanalına katılamaz.
10. Devre dışı/oturumu iptal edilmiş kullanıcı eski bağlantı/token ile işlem yapamaz.
