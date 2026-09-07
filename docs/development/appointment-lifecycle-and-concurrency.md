# Randevu Yaşam Döngüsü ve Eşzamanlılık Yönetimi (F03-G04)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Randevu Yaşam Döngüsü (Appointment Lifecycle)**, durum geçiş kurallarını, atomik slot rezervasyonu ve eşzamanlı çift alma (double booking) koruma mekanizmalarını açıklar.

## 1. Mimari Prensipler ve Durum Modeli

- **Randevu Durumları (`AppointmentStatus`):**
  - `Draft`: Randevu taslağı.
  - `Reserved`: Slotun geçici olarak tutulması (Hold).
  - `Confirmed`: Kesinleşmiş, aktif randevu.
  - `CheckedIn`: Hastanın polikliniğe vardığı ve giriş yaptığı durum.
  - `Completed`: Hekim muayenesinin/işleminin tamamlandığı durum.
  - `Cancelled`: Randevunun iptal edildiği durum (iptal nedeni kaydedilir). Slot tekrar `Available` haline getirilir.
  - `NoShow`: Hastanın randevuya gelmediği durum.
- **Geçiş Kuralları (`Domain Invariants`):**
  - Yalnızca `Confirmed` randevular için `CheckedIn` veya `NoShow` yapılabilir.
  - Yalnızca `CheckedIn` durumundaki randevular `Completed` yapılabilir.
  - `Completed` durumundaki bir muayene/randevu iptal edilemez (`Cancel`).
  - İptal edilen randevu için check-in yapılamaz.
- **Atomik Slot Alma ve Yarış Koşulu Koruması (`Optimistic Concurrency`):**
  - İki eşzamanlı istemci aynı slot için `BookAppointment` çağrısı yaptığında veritabanı düzeyindeki `AppointmentSlot.Version` ve durum kontrolü ile tam olarak biri `201 Created` alırken diğeri `409 Conflict` alır.
  - İptal edilen randevunun slotu tekrar `Available` hale gelerek yeni rezervasyonlara açılır; tarihsel iptal kayıtları denetim amacıyla `appointments` tablosunda saklanır.
- **Denetim İzi:** Randevu oluşturma (`Appointment.Book`), iptal etme (`Appointment.Cancel`), check-in (`Appointment.CheckIn`), tamamlama (`Appointment.Complete`) ve no-show (`Appointment.NoShow`) olayları kriptografik hash'li log tablosuna kaydedilir.

## 2. Veritabanı Tablosu (`scheduling.appointments`)

| Alan | Tip | Açıklama |
|---|---|---|
| `id` | `uuid` | Benzersiz randevu kimliği (PK) |
| `slot_id` | `uuid` | Bağlı randevu slotu |
| `patient_id` | `uuid` | Randevu sahibi hasta |
| `doctor_id` | `uuid` | İlgili hekim |
| `department_id` | `uuid` | Poliklinik/bölüm |
| `appointment_time_utc` | `timestamptz` | Randevu tarihi ve saati |
| `status` | `varchar(16)` | Randevu durumu (`Confirmed`, `CheckedIn`, `Completed`, `Cancelled`, `NoShow`) |
| `reason_for_visit` | `varchar(512)` | Geliş şikayeti / nedeni |
| `cancellation_reason` | `varchar(512)` | İptal gerekçesi |
| `version` | `bigint` | Concurrency kontrol versiyonu |

## 3. Test ve Doğrulama

- **Birim Testleri (`AppointmentDomainUnitTests`):**
  - Başarılı yaşam döngüsü (`Confirmed -> CheckedIn -> Completed`) ve versiyon artışları.
  - Geçersiz geçişlerin reddedilmesi (`Cancelled -> CheckedIn` ret, `Confirmed -> Completed` ret).
  - Tamamlanmış randevunun iptal edilememesi kuralı.
- **Entegrasyon Testleri (`AppointmentLifecycleIntegrationTests`):**
  - İki paralel istemcinin aynı slotu aynı anda almayı denemesi (`Task.WhenAll`): 1 Created, 1 Conflict doğrulaması.
  - Randevu alma, iptal etme, açılan slotun yeniden alınabilmesi, check-in ve tamamlama uçtan uca akışı.
