# Ameliyat Planlama ve Cerrahi Çakışma Yönetimi Geliştirme Notları

Bu belge, **F08-G04 — Ameliyat planlama** görevinin mimari tasarımını, ameliyathane salon yönetimini, cerrahi ekip planlamasını, atomik çakışma önleme kurallarını, pre-op kontrol listesini ve test kapsamını açıklar.

---

## 1. Mimari ve Kapsam

Ameliyat Planlama ve Cerrahi Süreç Modülü (`HospitalManagement.Modules.SurgeryCriticalCare`), cerrahi randevuların ve ameliyathanelerin yaşam döngüsünü yönetir:
- **Ameliyathane Salonları (`OperatingRoom`):** Salon kodu (`DEMO-OR-01`, `DEMO-OR-02`, `DEMO-OR-03`), salon adı, kapasite, branş kısıtlaması ve aktiflik durumu.
- **Cerrahi Randevu & İstemi (`SurgeryBooking`):** Protokol numarası (`DEMO-SURG-YYYYMMDD-XXXXXX`), hasta ID, cerrahi bölüm, işlem adı ve kodu, aciliyet (`Elective`, `Expedited`, `Emergency`), salon ataması, sorumlu cerrah ve anestezi hekimi ataması, planlanan başlangıç ve bitiş zamanları.
- **Pre-Op Kontrol Listesi (`PreOpChecklistInfo`):** 6 zorunlu pre-op güvenlik kontrolü:
  1. Bilgilendirilmiş hasta onamı alındı (`ConsentSigned`).
  2. Anestezi pre-op uygunluk viziti tamamlandı (`AnesthesiaClearance`).
  3. Açlık (NPO) süresi teyit edildi (`NpoConfirmed`).
  4. Kan ve rezerve ürün hazırlığı teyit edildi (`BloodProductsReserved`).
  5. Cerrahi alan işaretlemesi yapıldı (`SiteMarked`).
  6. İlaç ve lateks alerjisi kontrol edildi (`AllergyChecked`).
  Tüm kontroller onaylandığında randevu durumu `PreOpCleared` statüsüne terfi eder.

### Atomik Çakışma Önleme Kuralları (Conflict Prevention)
- **Kabul Kriteri:** *Oda veya zorunlu ekip çakışması atomik reddedilir.*
- **Oda Çakışması (Room Conflict):** Seçilen ameliyathane salonunda aynı zaman aralığında (`start < otherEnd && end > otherStart`) planlanmış başka bir aktif ameliyat (`Scheduled`, `PreOpCleared`, `InProgress`) bulunuyorsa istek `409 Conflict` ile reddedilir.
- **Cerrah Çakışması (Lead Surgeon Conflict):** Sorumlu cerrah hekim aynı zaman aralığında başka bir ameliyathane veya operasyonda görevli ise istek `409 Conflict` ile reddedilir.
- **Anestezist Çakışması (Anesthesiologist Conflict):** Anestezi hekimi aynı zaman aralığında başka bir ameliyathanede görevli ise istek `409 Conflict` ile reddedilir.
- **Klinik Güvenlik Doğrulaması:** Sorumlu cerrah ile anestezi hekimi aynı hekim olamaz (`400 Validation Problem`).

---

## 2. API Uç Noktaları

| Metot | Yol | Yetki | Açıklama |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/surgery/operating-rooms` | Giriş Yapmış Kullanıcı | Ameliyathane salonlarını listeler. |
| `POST` | `/api/v1/surgery/bookings` | `surgery.schedule` | Yeni cerrahi randevu oluşturur (atomik çakışma kontrollü). |
| `POST` | `/api/v1/surgery/bookings/{id}/reschedule` | `surgery.schedule` | Randevuyu yeni salon/saate taşır (çakışma kontrollü). |
| `POST` | `/api/v1/surgery/bookings/{id}/pre-op-checklist` | `surgery.schedule` | 6 adımlı Pre-Op güvenlik kontrol listesini kaydeder. |
| `POST` | `/api/v1/surgery/bookings/{id}/cancel` | `surgery.schedule` | İptal gerekçesiyle randevuyu iptal eder. |
| `GET` | `/api/v1/surgery/bookings/{id}` | Giriş Yapmış Kullanıcı | Randevu detayını döner. |
| `GET` | `/api/v1/surgery/bookings` | Giriş Yapmış Kullanıcı | Tarih, salon, cerrah ve durum filtreli ameliyat listesi. |

---

## 3. Kullanıcı Arayüzü

- Sayfa: `src/HospitalManagement.Web.Client/Pages/Surgery/SurgeryScheduling.razor` (`/surgery/scheduling`)
- Menü: `MainLayout.razor` içerisinde hekim ve yönetici menülerinde "Ameliyat Planlama".
- Özellikler:
  - KPI Sayaçları (Toplam, Planlandı, Pre-Op Hazır, Devam Ediyor, Tamamlandı, İptal).
  - Salon, durum ve tarih filtreleme barı.
  - Randevu tablosu, aciliyet rozetleri (`Emergency`, `Expedited`, `Elective`) ve 6/6 Pre-Op uygunluk göstergesi.
  - Yeni Ameliyat Planlama, Pre-Op Kontrol Listesi, Yeniden Planlama ve İptal modalleri.

---

## 4. Doğrulama ve Testler

- **Birim Testleri (`SurgeryPlanningDomainTests.cs`):**
  - Randevu oluşturma, protokol üretimi ve aynı cerrah/anestezist doğrulama hatası.
  - Pre-Op kontrol listesinin 6/6 tamamlama durumunda `PreOpCleared` statüsüne geçişi.
  - `SurgeryPlanningService` ameliyathane salon çakışması tespiti ve `409 Conflict`.
  - `SurgeryPlanningService` sorumlu cerrah zaman çakışması tespiti ve `409 Conflict`.
- **Bileşen Testleri (`SurgerySchedulingComponentTests.cs`):**
  - Bunit ile sayfa başlığı, KPI sayaçları, salon ve protokol verilerinin render doğrulaması.
- **Entegrasyon Testleri (`SurgeryPlanningIntegrationTests.cs`):**
  - Gerçek PostgreSQL ve `ApiWebApplicationFactory` üzerinde:
    1. `DEMO-OR-01` için ilk randevunun `201 Created` ile oluşturulması.
    2. Aynı salonda çakışan ikinci randevunun `409 Conflict` ile atomik reddedilmesi.
    3. Farklı salonda ama aynı cerrahla çakışan üçüncü randevunun `409 Conflict` ile atomik reddedilmesi.
    4. 6/6 Pre-Op kontrol listesi kaydı ve durumun `PreOpCleared` oluşunun doğrulanması.
    5. Klinik yetkisi olmayan `SystemAdministrator` kullanıcısının `403 Forbidden` ile reddedilmesi.
