# Hasta Portalı Reçete Görünümü ve Gizlilik İlkeleri (Patient Prescription Portal & Privacy)

## 1. Genel Bakış ve Amaç

Bu belge, **Faz 5 (Eczane & İlaç Yönetimi)** kapsamında uygulanan `F05-G08 — Hasta reçete görünümü` mimarisini, hasta portalındaki reçete ve ilaç kullanım talimatları deneyimini, IDOR korumasını ve sentetik demo uyarısı kurallarını açıklar.

---

## 2. Tasarım ve Güvenlik İlkeleri

1. **IDOR ve Erişim Kısıtlamaları:**
   - Hasta yalnızca kendi kimliği (`PersonId`) ile eşleşen reçeteleri listeleyebilir ve inceleyebilir.
   - Başka bir hastanın `patientId` parametresi veya `prescriptionId` bilgisiyle yapılan yetkisiz sorgulamalar API katmanında `403 Forbidden` ile engellenir.
2. **Klinik Statü Filtreleme (Taslak İzolasyonu):**
   - Hekimin hazırlık aşamasındaki taslak (`Draft`) ve sehven girilmiş (`EnteredInError`) reçeteleri hasta portalından filtrelenir. Hasta yalnızca imzalanmış (`Signed`), kısmen karşılanmış (`PartiallyDispensed`), tamamı karşılanmış (`Dispensed`), iptal edilmiş (`Cancelled`) veya süresi dolmuş (`Expired`) reçeteleri görür.
3. **Hasta Dostu Tıbbi Dil Çevirisi:**
   - Uygulama yolları ve doz sıklıkları hastanın anlayabileceği açık ifadelerle sunulur (Örn: `Oral` -> `Ağızdan (Oral)`, `IV` -> `Damar Yoluyla (IV)`).
4. **Zorunlu Sentetik Demo Uyarısı:**
   - Sayfa başında, verilerin eğitim ve simülasyon amaçlı sentetik veriler olduğuna ve gerçek tıbbi tedavi için kullanılamayacağına dair açık bir uyarı paneli yer alır.

---

## 3. Sayfa ve Bileşenler

- **Sayfa:** `src/HospitalManagement.Web.Client/Pages/Patient/MyPrescriptions.razor` (`/patient/prescriptions`)
- **Menü Erişimi:** Hasta rolü (`SessionState.IsPatient`) için `MainLayout.razor` içerisinde `Reçetelerim` menü linki.

---

## 4. Doğrulama ve Test Kapsamı

- **Bileşen Testleri (`MyPrescriptionsComponentTests`):** bUnit ile hasta reçete listesi, sentetik demo uyarısı, anlaşılır tıbbi dil ve yetkisiz personel engeli (`ForbiddenState`).
- **Entegrasyon Testleri (`PatientPrescriptionPortalIntegrationTests`):** PostgreSQL üzerinde hastanın imzalı reçetelerini görmesi, taslak reçetelerin gizlenmesi ve IDOR yetkisiz sorgulama engellemesi.
