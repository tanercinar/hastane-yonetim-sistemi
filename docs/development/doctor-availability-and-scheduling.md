# Doktor Uygunluk Takvimi ve Randevu Slot Motoru (F03-G03)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Doktor Uygunluk Takvimi (Doctor Availability Schedule)** ve **Randevu Slot Üretim Motoru (Slot Generation Engine)** mimarisini, veri modellerini, mola/izin engelleme kurallarını ve saat dilimi/yaz saati (DST) yönetimini açıklar.

## 1. Mimari Prensipler ve Tasarım Kararları

- **Modül İzolasyonu (ADR-0001):** Takvim ve randevu slotu yönetimi `src/Modules/Scheduling/` altındadır. Diğer modüllerin tablolarına veya context'lerine doğrudan erişmez; `HospitalManagement.BuildingBlocks` üzerinden yetki ve denetim entegrasyonu sağlar.
- **Haftalık Çalışma Pencereleri (`doctor_schedules`):**
  - Doktor ve bölüm bazında haftanın günü (`DayOfWeek`), çalışma başlangıç ve bitiş saatleri (`StartTime`, `EndTime`) ve randevu süresi (`SlotDurationMinutes`: 5-120 dk).
  - Her takvime bağlı çalışma molaları (`schedule_breaks`: örn. 12:30 - 13:30 Öğle Molası).
  - Aynı gün ve doktor için birden fazla aktif takvim açılması çakışma kontrolüyle (`409 Conflict`) engellenir.
- **Doktor İzin ve Blokajları (`doctor_leave_blocks`):**
  - Doktorun izinli veya görevli olduğu UTC zaman aralıkları (`StartUtc`, `EndUtc`).
  - İzin kaydı oluşturulduğunda o aralıktaki tüm `Available` randevu slotları otomatik olarak `Blocked` durumuna geçirilir.
- **Deterministik ve Atomik Slot Üretim Motoru (`SlotGenerationEngine`):**
  - Belirtilen tarih aralığında çalışma penceresini slot süresine göre parçalara ayırır.
  - Molalar (`ScheduleBreak`) ile çakışan slotları filtreler.
  - Doktor izinleri (`DoctorLeaveBlock`) ile çakışan slotları filtreler.
  - Yerel saat dilimi (`Europe/Istanbul` / `UTC`) dönüşümlerini yaz saati (DST) geçişlerini gözeterek doğru UTC `DateTime` damgalarına çevirir.
  - Veritabanında zaten var olan slotları atlayarak mükerrerlik ve yarış koşullarını (`Idempotent`) engeller (`ux_appointment_slots_doctor_start`).
- **Eşzamanlılık Koruması (`IHasConcurrencyVersion`):** `AppointmentSlot` üzerindeki `Version` alanı optimistik kilitleme ile eşzamanlı rezervasyon (`Hold`) ve kesin kayıt (`Book`) yarışlarını yönetir.
- **Denetim İzi:** Takvim oluşturma (`Scheduling.ScheduleCreate`), izin blokajı (`Scheduling.LeaveBlockCreate`) ve slot üretimi (`Scheduling.SlotsGenerate`) olayları kriptografik hash'li log tablosuna kaydedilir.

## 2. Veri Modeli ve Tablo Şeması (`scheduling`)

| Tablo | Açıklama | Anahtar Alanlar ve İndeksler |
|---|---|---|
| `scheduling.doctor_schedules` | Haftalık doktor çalışma pencereleri | `id`, `doctor_id`, `department_id`, `day_of_week`, `start_time`, `end_time`, `slot_duration_minutes`, `is_active` |
| `scheduling.schedule_breaks` | Çalışma penceresi içindeki molalar | `id`, `schedule_id`, `start_time`, `end_time`, `reason` |
| `scheduling.doctor_leave_blocks` | Doktor izin ve blokaj aralıkları | `id`, `doctor_id`, `start_utc`, `end_utc`, `reason`, `is_active` |
| `scheduling.appointment_slots` | Üretilen ve rezerve edilebilir randevu slotları | `id`, `doctor_id`, `department_id`, `start_utc`, `end_utc`, `status`, `version` (Unique: `doctor_id + start_utc`) |

## 3. Test ve Doğrulama

- **Birim Testleri (`ScheduleDomainUnitTests`):**
  - Takvim oluşturma, pencere içi mola geçerliliği, pencere dışı/çakışan mola retleri.
  - Slot rezerve etme (`Hold`), süre aşımı, kesin rezervasyon (`Book`) ve concurrency versiyon artışları.
  - `SlotGenerationEngine` mola ve izin filtreleme, idempotency ve saat dilimi testleri.
- **Entegrasyon Testleri (`DoctorAvailabilityScheduleIntegrationTests`):**
  - Canlı PostgreSQL üzerinde takvim oluşturma (`201 Created`).
  - Aynı gün için mükerrer takvim çakışma tespiti (`409 Conflict`).
  - Gelecek tarihler için atomik slot üretimi (`GenerateDoctorSlots`).
  - Doktor izin kaydı sonrası çakışan slotların `Blocked` durumuna geçmesi ve uygunluk sorgusu (`GetDoctorAvailability`).
