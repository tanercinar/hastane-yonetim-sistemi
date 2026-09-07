# Yoğun Bakım Kabul ve Yatak Yönetimi (ICU Admission & Bed Management)

Bu doküman, Faz 8 kritik alanlar kapsamında yoğun bakım kabul süreçleri, yatak yönetimi, dinamik bakım planı revizyonları ve servis transfer/taburculuk mimarisini detaylandırır.

> **Bağımsız inceleme notu (31 Ağustos 2026):** Görev yeniden açılmıştır. Mevcut API `InpatientStayId` değerinin gerçekten var olduğunu ve aynı hastaya ait olduğunu doğrulamamakta; ICU kabul/çıkışı ana Inpatient yatak hareketi, transfer ve taburculuk kayıtlarını güncellememektedir. Aşağıdaki tek-zincir iddiası bu orkestrasyon ve regresyon testleri eklenene kadar karşılanmış sayılmaz.

## 1. Mimari ve Alan Sınırları

- **Modül:** `HospitalManagement.Modules.SurgeryCriticalCare`
- **Klinik Güvenlik İlkesi:** Yoğun bakım ünitesi eğitim ve klinik simülasyon amaçlıdır. Klinik karar hekim sorumluluğundadır; yapay zekâ veya otomatik karar desteği içermez.
- **Yatış Hareket Zinciri:** Yoğun bakım kabulü, hastanın tek ve bütünleşik hastane yatışını (`InpatientStayId`) referans alır. Böylece hasta acil/poliklinik -> yataklı servis -> yoğun bakım -> cerrahi servis hareketlerinde tek yatış protokolü üzerinden izlenir.

## 2. Temel Alan Kavramları ve Modeller

### 2.1. Yoğun Bakım Kritiklik Basamakları (`IcuAcuityLevel`)
- `Level3MultiOrganSupport`: Basamak 3 - Çoklu organ yetmezliği, gelişmiş invaziv destek (mekanik ventilasyon, ECMO, diyaliz).
- `Level2IntensiveMonitoring`: Basamak 2 - Yoğun hemodinamik izlem, tek organ disfonksiyonu veya yüksek riskli post-operatif yakın takip.
- `Level1HighDependency`: Basamak 1 - Yüksek bağımlı bakım, sık vital bulgu takibi, servise geçiş öncesi stabilizasyon.

### 2.2. Ventilasyon Modları (`IcuVentilationMode`)
- `NoneSpontaneous`: Spontan solunum (oda havası veya düşük akışlı nazal kanül).
- `HighFlowNasalCannula`: Yüksek akışlı nazal oksijen (HFNC).
- `NonInvasiveCpapBiPap`: Non-invaziv mekanik ventilasyon (NIV/CPAP/BiPAP).
- `InvasiveMechanical`: Endotrakeal tüp veya trakeostomi ile invaziv mekanik ventilasyon.

### 2.3. Yatak ve Kabul Durumu
- `IcuBed`: Yatak kodu (`DEMO-ICU-01` .. `DEMO-ICU-06`), birim adı ve aktiflik durumu.
- `IcuAdmission`: Protokol formatı `DEMO-ICU-YYYYMMDD-XXXXXX`, kabul gerekçesi, basamak seviyesi, izlem sıklığı (dk), sorumlu hekim/hemşire ve dinamik bakım planı.
- Çıkış / Sevk durumları (`IcuAdmissionStatus`): `Active`, `TransferredToWard`, `Discharged`, `Deceased`.

## 3. Güvenlik ve Eşzamanlılık

- **Yatak Çakışması Önleme:** Seçilen yoğun bakım yatağında hâlihazırda `Active` durumda bir hasta varsa veya hasta zaten aktif bir yoğun bakım yatağındaysa sistem `409 Conflict` döner.
- **İmmutability & Yaşam Döngüsü:** Sonlandırılmış (`TransferredToWard`, `Discharged`) yatış kayıtları üzerinde doğrudan bakım planı değişikliği yapılamaz.
- **Yetkilendirme:**
  - Yoğun bakım kabulü ve bakım planı güncelleme: `HospitalPermissions.Inpatient.CriticalCareRecord` izni gerektirir.
  - Sorumlu roller: `Doctor`, `Nurse`, `ChiefMedicalOfficer`. Sistem yöneticisi (`SystemAdministrator`) klinik işlem yapamaz (`403 Forbidden`).
- **Audit Günlüğü:** Tüm kabul, plan revizyonu ve servis devir işlemleri `Surgery.IcuAdmit`, `Surgery.IcuCarePlanUpdate` ve `Surgery.IcuDischargeOrTransfer` audit aksiyonları olarak yayınlanır.

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/icu/beds` | Oturum Açmış Kullanıcı | ICU yatakları ve anlık doluluk durumunu listeler |
| `POST` | `/api/v1/icu/admissions` | `critical-care.record` | Yoğun bakıma hasta kabulü ve yatak ataması yapar |
| `POST` | `/api/v1/icu/admissions/{id}/care-plan` | `critical-care.record` | Basamak seviyesi, ventilasyon ve bakım planını günceller |
| `POST` | `/api/v1/icu/admissions/{id}/discharge-or-transfer` | `critical-care.record` | Servise devir veya taburculuk işlemini gerçekleştirir |
| `GET` | `/api/v1/icu/admissions/active` | Oturum Açmış Kullanıcı | Aktif yoğun bakım yatışlarını listeler |
| `GET` | `/api/v1/icu/admissions/{id}` | Oturum Açmış Kullanıcı | Belirtilen kabul kaydının detaylarını getirir |

## 5. Doğrulama ve Testler

- **Birim Testleri (`IcuAdmissionDomainTests.cs`):** Aggregate yaşam döngüsü, yatak çakışması ve mükerrer aktif yatış engelleme testleri.
- **Bileşen Testleri (`IcuBedManagementComponentTests.cs`):** Yatak panosu, doluluk KPI göstergeleri, basamak kartları ve modal butonları.
- **Entegrasyon Testleri (`IcuAdmissionIntegrationTests.cs`):** Gerçek PostgreSQL üzerinde kabul -> çakışma kontrolü -> plan revizyonu -> servis devri -> RBAC yetkisiz erişim kontrolü doğrulaması.
