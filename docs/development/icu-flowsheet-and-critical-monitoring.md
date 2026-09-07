# Yoğun Bakım Akış Sayfası ve Kritik İzlem (ICU Flowsheet & Monitoring)

Bu doküman, Faz 8 kritik alanlar kapsamında yoğun bakım hastaları için saatlik vital bulgu izlemi, ventilatör parametreleri simülasyonu, 24 saatlik sıvı dengesi (Intake & Output / I&O) hesaplaması ve klinik akış sayfası mimarisini detaylandırır.

## 1. Mimari ve Alan Sınırları

- **Modül:** `HospitalManagement.Modules.SurgeryCriticalCare`
- **Klinik Simülasyon ve Sorumluluk İlkesi:** Sistem eğitim ve simülasyon amaçlı bir klinik kayıt platformudur. Donanım/cihaz entegrasyonu (monitör veya ventilatör fiziksel bağlantısı) içermez; tüm gözlem ve skorlar klinik personel (yoğun bakım hekimi ve hemşiresi) tarafından doğrulanarak sisteme girilir.
- **Tek Yatış Hareket Zinciri:** Akış sayfası gözlemleri, doğrudan `IcuAdmission` köküne (`IcuAdmissionId`) bağlı olarak kaydedilir.

## 2. Temel Parametreler ve Hesaplamalar

### 2.1. Vital Bulgular ve Klinik Skorlar
- **Kalp Tepe Atımı (HR):** 20 - 300 bpm fizyolojik aralık doğrulaması.
- **Kan Basıncı (NIBP / İnvaziv Arter):** Sistolik ve Diyastolik tansiyon (mmHg).
- **Ortalama Arter Basıncı (MAP):** Otomatik hesaplama:
  $$\text{MAP} = \frac{2 \times \text{Diastolik} + \text{Sistolik}}{3}$$
- **Solunum Sayısı (RR):** bpm (soluk/dk).
- **SpO2:** %40 - %100 aralığı.
- **Vücut Sıcaklığı:** 25.0 - 45.0 °C.
- **Glasgow Koma Skalası (GCS):** 3 - 15 aralığı.
- **Richmond Agitasyon-Sedasyon Skalası (RASS):** -5 (derin sedasyon) ile +4 (saldırgan/ajite) aralığı.

### 2.2. Ventilatör Parametreleri (Simülasyon)
- **Mod:** `NoneSpontaneous`, `HighFlowNasalCannula`, `NonInvasiveCpapBiPap`, `InvasiveMechanical`.
- **FiO2:** Solunan oksijen fraksiyonu (%21 - %100).
- **PEEP:** Pozitif ekspirasyon sonu basınç (cmH2O).
- **Tidal Hacim (Vt):** ml cinsinden tidal volüm.
- **PIP:** Tepe inspiratuar basınç (cmH2O).

### 2.3. Sıvı Dengesi (Intake & Output - I&O)
- **Giriş (Intake):** IV Sıvılar (ml) + Enteral/Oral Beslenme (ml).
- **Çıkış (Output):** İdrar Çıkışı (ml) + Dren/NG/Stoma Çıkışı (ml).
- **Net Sıvı Dengesi (Net Balance):** $\text{Toplam Giriş} - \text{Toplam Çıkış}$ (ml).
- **24 Saatlik Özet (`IcuFluidBalanceSummary`):** İlgili zaman aralığındaki tüm giriş ve çıkışların kümülatif toplamını ve net sıvı dengesini hesaplar.

## 3. Güvenlik, İmmutability ve Denetim İzi

- **Yetkilendirme:**
  - Akış sayfası verisi ekleme ve izleme: `HospitalPermissions.Inpatient.CriticalCareRecord` izni (`Doctor`, `Nurse`, `ChiefMedicalOfficer`).
  - Sistem yöneticisi ve idari personel klinik akış verisi giremez (`403 Forbidden`).
- **Audit Günlüğü:** Her saatlik gözlem kaydında `Surgery.IcuFlowsheetRecord` audit olayı yayınlanır; kaydeden personel, hasta kabul numarası, vital özet ve net sıvı bakiyesi güvenli bir şekilde denetlenir.

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/icu/admissions/{id}/flowsheet` | `critical-care.record` | Saatlik vital, ventilatör ve sıvı takibi gözlemi ekler |
| `GET` | `/api/v1/icu/admissions/{id}/flowsheet` | Oturum Açmış Kullanıcı | Belirtilen kabulün saatlik akış verilerini listeler (zaman azalan) |
| `GET` | `/api/v1/icu/admissions/{id}/flowsheet/fluid-balance` | Oturum Açmış Kullanıcı | Son 24 saatlik kümülatif sıvı dengesi özetini hesaplar |

## 5. Doğrulama ve Testler

- **Birim Testleri (`IcuFlowsheetDomainTests.cs`):** MAP ve net sıvı dengesi hesaplama, fizyolojik aralık sınır kontrolleri, kümülatif sıvı özeti doğrulaması.
- **Bileşen Testleri (`IcuFlowsheetComponentTests.cs`):** Akış tablosu sütunları, 24 saatlik sıvı özet kartları, simülasyon uyarı banner'ı ve gözlem ekleme modalı.
- **Entegrasyon Testleri (`IcuFlowsheetIntegrationTests.cs`):** Gerçek PostgreSQL üzerinde kabul -> akış gözlemi ekleme -> akış listesi çekme -> 24 saatlik sıvı özeti doğrulama -> yetkisiz erişim kontrolü.
