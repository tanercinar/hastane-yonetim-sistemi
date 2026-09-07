# Hasta Randevu Arama ve Alma Ekranları (F03-G05)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Hasta Randevu Arama ve Alma (Patient Appointment Search and Booking UI)** bileşenlerini, istemci API katmanını, erişim denetimi kurallarını ve arayüz durumlarını açıklar.

## 1. Mimari Prensipler ve Güvenlik Sınırları

- **Yetki ve Erişim Denetimi:**
  - `/patient/appointments/book`: Hasta veya randevu alma yetkisine sahip personeller tarafından kullanılır.
  - `/patient/appointments`: Hastanın kendi geçmiş ve gelecek randevularını listeler.
  - Hasta başka hastaların randevularını göremez, değiştiremez veya iptal edemez (`IDOR Koruması`).
- **İstemci Mimarisi (`SchedulingApiClient`):**
  - `ISchedulingApiClient` arayüzü ile gevşek bağlılık (loosely-coupled).
  - CSRF güvenliği için her POST işleminde `X-HMS-CSRF` başlığı otomatik olarak alınır ve iliştirilir.
- **Arayüz Durumları:**
  - `LoadingState`: Slotlar ve randevular yüklenirken.
  - `ForbiddenState`: Yetkisiz erişim denemelerinde.
  - `EmptyState`: Uygun slot veya kayıtlı randevu bulunmadığında.
  - `ErrorState` ve başarı geri bildirimleri.

## 2. Arayüz Bileşenleri ve Sayfalar

- **Randevu Alma Ekranı (`BookAppointmentPage.razor` - `/patient/appointments/book`):**
  - Poliklinik ve doktor seçimi.
  - Takvim tarih seçici (bugünden itibaren 14 gün ileriye kadar).
  - Seçilen güne ait müsait saat slotlarının grid halinde buton olarak listelenmesi.
  - Slot seçimi, geliş nedeni (opsiyonel şikayet) alanı ve "Randevuyu Onayla" butonu.
  - Başarılı işlem sonrası randevu özet kartı ve yönlendirme linkleri.
- **Randevularım Ekranı (`MyAppointments.razor` - `/patient/appointments`):**
  - Tarih, poliklinik, hekim, şikayet, durum rozeti ve işlem butonlarını içeren tablo.
  - Durum rozetleri: `Confirmed` (Onaylandı), `CheckedIn` (Giriş Yapıldı), `Completed` (Tamamlandı), `Cancelled` (İptal Edildi), `NoShow` (Gelmedi).
  - Gelecek onaylı randevular için iptal gerekçesi modalı ve "Randevuyu İptal Et" akışı.

## 3. Test ve Doğrulama

- **Bileşen Testleri (`AppointmentComponentTests`):**
  - Hekim, poliklinik ve slot butonlarının render edilmesi.
  - Randevularım tablosunun ve iptal modalının açılıp kapanması.
  - Yetkisiz durumda `ForbiddenState` gösterimi.
- **Entegrasyon Testleri (`PatientAppointmentSearchAndBookingIntegrationTests`):**
  - Canlı PostgreSQL üzerinde hasta oturumuyla uygun slotların aranması.
  - `BookAppointment` çağrısı ile randevu kaydı oluşturulması (`201 Created`).
  - `GetPatientAppointments` ile yalnızca hastanın kendi randevularının listelenmesi.
  - `CancelAppointment` çağrısı ile randevunun iptal edilmesi ve `audit_logs` tablosunda `Appointment.Book` ile `Appointment.Cancel` kayıtlarının tespiti.
