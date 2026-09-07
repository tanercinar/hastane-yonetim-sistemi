# Perioperatif Kayıt ve Değiştirilemezlik (Immutability) Geliştirme Notları

Bu belge, **F08-G05 — Perioperatif kayıt** görevinin mimari tasarımını, ameliyathane zaman milestonelarını, anestezi simülasyonunu, cerrahi güvenlik kontrollerini, imzalanmış kayıtların değiştirilemezliğini (immutability) ve düzeltme (correction/addendum) mekanizmasını açıklar.

---

## 1. Mimari ve Kapsam

Perioperatif Ameliyat ve Anestezi Süreci (`HospitalManagement.Modules.SurgeryCriticalCare`), cerrahi müdahale esnasında ve sonrasında gerçekleşen klinik verilerin kayıt altına alınmasını yönetir:
- **Zaman Milestoneları (Surgical Time Milestones):**
  1. Salona Giriş (`RoomEntryTimeUtc`)
  2. Anestezi Başlangıç (`AnesthesiaStartTimeUtc`)
  3. Cerrahi Kesi / İnsizyon (`IncisionTimeUtc`)
  4. Kapatma / Sütür Sonu (`ClosureTimeUtc`)
  5. Anestezi Bitiş / Uyandırma (`AnesthesiaEndTimeUtc`)
  6. Salondan Çıkış / PACU Transfer (`RoomExitTimeUtc`)
  *Doğrulama Kuralı:* Kronolojik sıralama zorunludur (`RoomEntry <= AnesthesiaStart <= Incision <= Closure <= AnesthesiaEnd <= RoomExit`).
- **Anestezi Simülasyonu & Havayolu:**
  - Anestezi Türü (`AnesthesiaType`: `General`, `RegionalSpinal`, `RegionalEpidural`, `LocalSedation`, `MAC`).
  - Anestezi İlaçları, İndüksiyon ve Seyir Notları.
- **İntraoperatif Cerrahi Bulgular & Güvenlik:**
  - Cerrahi operasyon notu ve bulgular (`IntraoperativeFindings`).
  - Gelişen komplikasyonlar (`IntraoperativeComplications`).
  - Tahmini kan kaybı mL (`EstimatedBloodLossMl`).
  - Patoloji ve biyopsi materyalleri (`SpecimensCollected`).
  - Gazlı bez, iğne ve cerrahi alet sayımı teyidi (`CountsConfirmed`).
- **Post-Op Sevk & Talimatlar:**
  - Post-Op Sevk Alanı (`PostOpDisposition`: `PACU`, `SurgicalWard`, `ICU`, `DirectDischarge`).
  - Post-Op bakım ve hemşirelik talimatları (`PostOpInstructions`).
- **Pre-Op Kontrol Listesi Eksiklik Uyarısı:**
  - Pre-Op kontrol listesi tamamlanmamışsa arayüzde belirgin uyarı gösterilir.
- **Klinik Kayıt Değiştirilemezliği ve Düzeltme Geçmişi (Immutability & Correction History):**
  - **Kabul Kriteri:** *İmzalı perioperatif kayıt düzeltme geçmişi tutar; eksik check-list uyarılır.*
  - Bir hekim kaydı imzaladığında (`IsSigned = true`), kayıt kilitlenir.
  - İmzalı kayda doğrudan `PUT` veya güncelleme yapılması `409 Conflict` ile engellenir.
  - Değişiklik ihtiyacı doğduğunda `POST /api/v1/surgery/perioperative-records/{id}/corrections` ile hekim ID, gerekçe (`ReasonForCorrection`) ve düzeltme notu (`CorrectionNote`) içeren yeni bir `PerioperativeCorrection` kaydı eklenir.

---

## 2. API Uç Noktaları

| Metot | Yol | Yetki | Açıklama |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/surgery/perioperative-records` | `surgery.schedule` | Taslak perioperatif kaydı oluşturur/günceller (İmzalanmışsa `409 Conflict`). |
| `GET` | `/api/v1/surgery/perioperative-records/by-booking/{bookingId}` | Giriş Yapmış Kullanıcı | Randevu ID'sine göre perioperatif kaydı ve düzeltme geçmişini döner. |
| `GET` | `/api/v1/surgery/perioperative-records/{id}` | Giriş Yapmış Kullanıcı | ID ile perioperatif kaydı ve düzeltme geçmişini döner. |
| `POST` | `/api/v1/surgery/perioperative-records/{id}/sign` | `surgery.schedule` | Perioperatif kaydı resmî olarak imzalar ve kilitler. |
| `POST` | `/api/v1/surgery/perioperative-records/{id}/corrections` | `surgery.schedule` | İmzalı kayda gerekçeli düzeltme / ek not ekler. |

---

## 3. Kullanıcı Arayüzü

- Sayfa: `src/HospitalManagement.Web.Client/Pages/Surgery/PerioperativeRecordFlow.razor` (`/surgery/perioperative/{BookingId:guid}`)
- Özellikler:
  - Protokol, aciliyet, cerrah, anestezist ve Pre-Op checklist durumu banner'ı.
  - 6 adet kronolojik zaman girişi.
  - Anestezi ve cerrahi bulgular alanları.
  - Alet sayımı teyit anahtarı.
  - Post-Op sevk seçimi.
  - Taslak Kaydet, İmzala & Kilitle butonları.
  - İmzalı kayıtlar için "Düzeltme / Ek Not Ekle" modalı ve geçmiş denetim izi paneli.

---

## 4. Doğrulama ve Testler

- **Birim Testleri (`PerioperativeRecordDomainTests.cs`):**
  - Kronolojik zaman sıralaması doğrulama ve hatalı sıralamada `ArgumentException`.
  - İmzalama sonrası doğrudan taslak düzenlemede `InvalidOperationException`.
  - İmzalı kayda `AddCorrection` ile düzeltme gerekçesi ve notunun kaydedilmesi.
  - Çok adımlı taslak -> imzalama -> engelleme -> düzeltme servis akışı.
- **Bileşen Testleri (`PerioperativeRecordComponentTests.cs`):**
  - Bunit ile başlık, 6 zaman milestone'u, anestezi formu ve sayım anahtarı render doğrulaması.
- **Entegrasyon Testleri (`PerioperativeRecordIntegrationTests.cs`):**
  - Gerçek PostgreSQL üzerinde:
    1. Ameliyat randevusu oluşturma.
    2. Perioperatif taslak kaydetme (`200 OK`).
    3. Hekim imzası (`200 OK`, `IsSigned = true`).
    4. İmzalı kaydı doğrudan güncelleme denemesinde `409 Conflict`.
    5. Gerekçeli düzeltme notu ekleme (`200 OK`).
    6. Randevu bazlı sorguda düzeltme geçmişinin eksiksiz gelmesinin doğrulanması.
