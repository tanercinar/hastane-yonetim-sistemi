# Randevu Check-In ve Günlük Sıra Yönetimi (F03-G06)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Kayıt/Check-in ve Günlük Sıra Yönetimi (Appointment Check-In and Queue Management)** mimarisini, sıra numarası üretim kurallarını, arayüz bileşenlerini ve güvenlik doğrulamalarını açıklar.

## 1. Mimari Prensipler ve Yaşam Döngüsü Kuralları

- **Sıra Numarası (Queue Number) Üretimi:**
  - Check-in yapılan her randevuya, ilgili hekim/poliklinik ve randevu gününe özel ardışık artan pozitif bir tam sayı sıra numarası (`QueueNumber`, örn: `1, 2, 3...`) atanır.
  - Sıra numarası çakışmalarını önlemek ve tutarlılığı korumak amacıyla `appointments` tablosunda `queue_number` alanı saklanır.
- **Durum Geçiş Sınırları ve Korumaları:**
  - `CheckIn`: Yalnızca `Confirmed` (Onaylı) durumundaki randevular için çalışır. `Cancelled`, `Completed` veya `NoShow` durumundaki randevular için check-in çağrıldığında `InvalidOperationException` fırlatılır ve `409 Conflict` dönülür.
  - `NoShow`: Yalnızca `Confirmed` durumundaki randevular "Gelmedi" olarak işaretlenebilir.
  - `Complete`: Yalnızca `CheckedIn` durumundaki muayeneler tamamlanabilir.
- **Yetkilendirme Sınırları:**
  - Günlük randevu kuyruğu (`/api/v1/scheduling/appointments/daily`) ve check-in / no-show işlemleri `HospitalPermissions.Appointment.CheckIn` ve `HospitalPermissions.Appointment.Manage` yetkisine tabi olup yalnızca Kayıt Personeli (`RegistrationStaff` / `REG`) ve yetkili personel tarafından yürütülebilir.
  - Hasta portali kullanıcılarının (`Patient` / `PAT`) günlük kuyruk uç noktasına erişimi `403 Forbidden` ile engellenir.

## 2. Arayüz Bileşenleri (`DailyAppointmentQueue.razor`)

- **Günlük Randevu ve Sıra Ekranı (`/staff/queue`):**
  - Tarih seçici, poliklinik ve hekim filtreleri.
  - Randevu ve sıra tablosu: Saat, Sıra No rozeti (`#1`, `#2`), Hasta ID, Poliklinik, Hekim, Şikayet ve Randevu Durumu.
  - `Confirmed` randevular için "Giriş Yap (Check-In)" ve "Gelmedi (No-Show)" işlem butonları.
  - Check-in tamamlandığında otomatik sıra numarası bildirimi ve durum güncellemesi.

## 3. Test ve Doğrulama

- **Birim Testleri (`AppointmentDomainUnitTests`):**
  - `CheckIn` pozitif sıra numarası ataması ve durum geçişi.
  - Geçersiz sıra numarası (0 veya negatif) ve geçersiz durumlarda `ArgumentOutOfRangeException` / `InvalidOperationException` kontrolleri.
  - `MarkNoShow` durum geçişi ve negatif yollar.
- **Bileşen Testleri (`DailyAppointmentQueueComponentTests`):**
  - Günlük kuyruk tablosunun render edilmesi.
  - "Giriş Yap" butonuna basıldığında check-in API çağrısı ve başarı mesajının gösterilmesi.
  - Yetkisiz erişim durumunda `ForbiddenState` gösterimi.
- **Entegrasyon Testleri (`DailyAppointmentQueueIntegrationTests`):**
  - Canlı PostgreSQL üzerinde kayıt personeli oturumu ile ardışık check-in ve sıra numarası doğrulaması (`#1`, `#2`).
  - Çift check-in ve no-show randevuya check-in denemelerinde `409 Conflict` ret yanıtı.
  - Hasta hesabı ile erişim denemesinde `403 Forbidden` ret yanıtı.
  - `audit_logs` tablosunda `Appointment.CheckIn`, `Appointment.NoShow` ve `Appointment.DailyList` kayıtlarının tespiti.
