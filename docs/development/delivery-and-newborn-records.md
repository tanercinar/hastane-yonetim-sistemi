# Doğum ve Yenidoğan Kayıtları (Delivery and Newborn Records)

## 1. Amaç ve Kapsam

Bu belge, **Faz 9 — Uzmanlık dikey dilimleri** kapsamında `F09-G02 — Doğum kaydı` görevinin teknik mimarisini, veri modelini, API sözleşmelerini ve güvenlik kurallarını açıklar.

Doğum salonu süreci, anne ve yenidoğan kayıtlarının güvenli ilişkilendirilmesini, doğum yöntemi, perineal durum, kan kaybı, Apgar skorları ve kordon kanı parametrelerinin saklanmasını içerir.

> [!WARNING]
> **Klinik Simülasyon Bildirimi:** Bu modül bir eğitim ve yönetim simülasyonu bileşenidir; sertifikalı doğum kayıt sistemi veya otomatik klinik karar desteği değildir. Tıbbi kararlar ve uygulamalar hekim ile ebenin yetki ve sorumluluğundadır.

---

## 2. Mimari ve Modül İzolasyonu

Modüler monolit prensiplerine uygun olarak doğum ve yenidoğan kayıtları `SpecialtyCare` modülü altında `specialty` şemasında konumlandırılmıştır.

```
src/Modules/SpecialtyCare/
├── Domain/Obstetrics/
│   ├── DeliveryMode.cs
│   ├── PerinealTearDegree.cs
│   ├── NewbornGender.cs
│   ├── ResuscitationIntervention.cs
│   ├── NewbornRecord.cs
│   └── DeliveryRecord.cs (Aggregate Root)
├── Application/
│   ├── DeliveryDtos.cs
│   └── IDeliveryRecordService.cs
└── Infrastructure/
    ├── DeliveryRecordService.cs
    └── Persistence/
        ├── Configurations/
        │   ├── DeliveryRecordConfiguration.cs
        │   └── NewbornRecordConfiguration.cs
        └── Migrations/
            └── 20260831000900_AddDeliveryAndNewbornRecords.cs
```

---

## 3. Veri Modeli

### 3.1 `DeliveryRecord` (Doğum Kaydı)
- `Id`: `Guid` (Primary Key, client/domain generated)
- `PregnancyEpisodeId`: `Guid?` (İlgili gebelik takibi episode'u)
- `MotherPatientId`: `Guid` (Anne hasta kimliği)
- `EncounterId`: `Guid?` (İlgili klinik başvuru/encounter)
- `DeliveryProtocolNumber`: `string` (`DEMO-DEL-YYYYMMDD-XXXXXX`)
- `DeliveryMode`: `DeliveryMode` (`SpontaneousVaginal`, `AssistedVaginalVacuum`, `AssistedVaginalForceps`, `CesareanElective`, `CesareanEmergency`, `Vbac`)
- `DeliveryTimeUtc`: `DateTime`
- `GestationalAgeWeeks`: `int` (20-45)
- `GestationalAgeDays`: `int` (0-6)
- `PerinealTear`: `PerinealTearDegree` (`None`, `FirstDegree`, `SecondDegree`, `ThirdDegree`, `FourthDegree`, `Episiotomy`)
- `EstimatedBloodLossMl`: `decimal`
- `AttendingDoctorId`: `Guid`
- `AssistingMidwifeId`: `Guid?`
- `PediatricianDoctorId`: `Guid?`
- `MaternalComplicationsNotes`: `string?`
- `DeliverySummaryNotes`: `string?`
- `Newborns`: `IReadOnlyCollection<NewbornRecord>`

### 3.2 `NewbornRecord` (Yenidoğan Kaydı)
- `Id`: `Guid` (Primary Key)
- `DeliveryRecordId`: `Guid` (Foreign Key -> `DeliveryRecords.Id`)
- `NewbornPatientId`: `Guid` (Patients modülünde önceden açılmış, aktif ve anne kaydından ayrı sentetik yenidoğan Patient kimliği; zorunlu ve sistem genelinde benzersiz)
- `BirthOrder`: `int` (1: Tekiz/1. ikiz, 2: 2. ikiz)
- `BirthTimeUtc`: `DateTime`
- `Gender`: `NewbornGender` (`Male`, `Female`, `Undetermined`)
- `BirthWeightGrams`: `decimal` (300 - 7000 g)
- `BirthLengthCm`: `decimal` (20 - 70 cm)
- `HeadCircumferenceCm`: `decimal` (15 - 50 cm)
- `ApgarScore1Min`: `int` (0 - 10)
- `ApgarScore5Min`: `int` (0 - 10)
- `ApgarScore10Min`: `int?` (0 - 10)
- `ResuscitationGiven`: `ResuscitationIntervention` (`None`, `Stimulation`, `Suction`, `SupplementalOxygen`, `BagValveMask`, `Intubation`, `ChestCompressions`, `Medication`)
- `CordBloodPh`: `string?`
- `ComplicationsNotes`: `string?`

---

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/specialty/deliveries` | `specialty-care.record` | Yeni doğum kaydı ve ilk yenidoğan(lar)ı oluşturur |
| `GET` | `/api/v1/specialty/deliveries/{id}` | Yetkili Kullanıcı | ID'ye göre doğum kaydı ve yenidoğan listesini getirir |
| `GET` | `/api/v1/specialty/deliveries/mother/{motherPatientId}` | Yetkili Kullanıcı | Anne hasta ID'sine göre doğum kayıtlarını listeler |
| `POST` | `/api/v1/specialty/deliveries/{id}/newborns` | `specialty-care.record` | Mevcut doğum kaydına ek yenidoğan (çoğul gebelik) ekler |

### Yenidoğan Patient ön koşulu

Her bebek, doğum/yenidoğan kaydı gönderilmeden önce normal hasta kayıt süreciyle Patients modülünde ayrı bir sentetik Patient kaydına sahip olmalıdır. Doğum API'si bu Patient kaydını otomatik üretmez; verilen kimliğin aktif, anne Patient kimliğinden farklı ve daha önce başka bir `NewbornRecord` tarafından kullanılmamış olduğunu doğrular. Kimlik eksik, geçersiz veya tekrar kullanılmışsa kayıt oluşturulmaz.

`NewbornPatientId` oluşturulduktan sonra değiştirilemez. `UX_NewbornRecords_NewbornPatientId` veritabanı indeksi eşzamanlı isteklerde de aynı Patient kimliğinin iki bebeğe bağlanmasını engeller. Böylece SpecialtyCare yalnız Patient kimliğini referans eder; Patients tablosunu çoğaltmaz ve modül sınırını korur.

---

## 5. Denetim İzi (Audit Logging)

Her doğum ve yenidoğan kaydı denetim izine hassas kişisel sağlık verisi sızdırılmaksızın protokolle kaydedilir:
- `Specialty.DeliveryRecordCreate`
- `Specialty.NewbornRecordAdd`

---

## 6. Doğrulama ve Testler

- **Unit Testleri:** `DeliveryRecordDomainTests` (Apgar sınırları, fizyolojik ölçüm aralıkları, zorunlu/değişmez/tekil yenidoğan Patient kimliği, protokol formatı).
- **Component Testleri:** `DeliveryManagementComponentTests` (BUnit arayüz render, simülasyon uyarısı, zorunlu Patient alanı, yenidoğan listesi ve modal etkileşimi).
- **Entegrasyon Testleri:** `DeliveryRecordIntegrationTests` (PostgreSQL üzerinde gebelik takibi tamamlama, ayrı Patient kimlikli ikiz yenidoğan, eksik/anne/tekrar kullanılan kimlik negatifleri, benzersiz DB indeksi ve 403 Forbidden testi).

`RequireNewbornPatientIdentity` migration'ı boş Patient kimliğine sahte bir `Guid.Empty` atamaz. Önceki yalnız-DEMO veritabanında bağlantısız yenidoğan satırı bulunuyorsa güvenilir Patient eşlemesi otomatik üretilemeyeceği için migration öncesinde bu sentetik veriler kontrollü olarak yeniden oluşturulmalıdır.
