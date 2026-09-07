# Gerçek Zamanlı Randevu, Sıra ve Bildirim Yönetimi (SignalR) (F03-G08)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Gerçek Zamanlı Randevu Ekranı ve SignalR Entegrasyonu (Realtime Appointment and Queue Management)** mimarisini, yetkilendirme modelini, grup aboneliklerini ve yeniden bağlanma (reconnection) stratejilerini açıklar.

## 1. Mimari ve Yetkilendirme Prensipleri

- **SignalR Hub Uç Noktası (`/hubs/hospital`):**
  - Minimal API / SignalR eşlemesi `app.MapHub<HospitalHub>("/hubs/hospital")` ile sağlanır.
  - Hub `[Authorize]` özniteliğiyle korunur; anonim veya yetkisiz istekler reddedilir (`401/403`).
- **Grup Ayrımı ve İzolasyon:**
  - Kullanıcı bağlandığında Claims (`PersonId`, `Role`) incelenir:
    - Kişisel Grup: `person-{PersonId}` (Hastaya veya kullanıcıya özel anlık bildirimler).
    - Doktor Grubu: `doctor-{PersonId}` (İlgili poliklinik hekiminin hasta sırası güncellemeleri).
    - Personel Grubu: `staff-queue` (Kayıt, danışma ve sistem yöneticisi ekranları için gün boyu check-in ve slot değişiklikleri).
- **Yayınlanan Olay Modelleri (`HospitalManagement.Contracts.Realtime`):**
  - `SlotRealtimeUpdate(SlotId, DoctorId, Status)`: Slot doluluk/açılma durumlarında yayınlanır.
  - `QueueRealtimeUpdate(AppointmentId, DoctorId, QueueNumber, Status)`: Check-in veya no-show işlemlerinde sıra durumu yayınlanır.
  - `NotificationRealtimeUpdate(NotificationId, RecipientPersonId, Title, Message)`: Yeni bildirim oluştuğunda doğrudan hedefe iletilir.

## 2. İstemci Entegrasyonu ve Yeniden Bağlanma (`DailyAppointmentQueue.razor`)

- **SignalR İstemcisi (`Microsoft.AspNetCore.SignalR.Client`):**
  - `.WithUrl(Navigation.ToAbsoluteUri("/hubs/hospital"))` ve `.WithAutomaticReconnect()` ile yapılandırılır.
  - `IAsyncDisposable` uygulanarak bileşen ömrü bittiğinde bağlantı temizlenir (`DisposeAsync`).
- **Sunucudan Yeniden Senkronizasyon (Reconnection Handling):**
  - Ağ kopması veya yeniden bağlanma sonrasında `_hubConnection.Reconnected += async _ => await LoadDailyQueueAsync();` tetiklenerek en güncel durum sunucudan tekrar çekilir.
  - SignalR bağlantısı kurulamasa dahi UI çökmez, zarifçe REST ve manuel yenileme modunda çalışmayı sürdürür.

## 3. Test ve Doğrulama

- **Bileşen Testleri (`DailyAppointmentQueueComponentTests`):**
  - SignalR bağlantısının asenkron başlatılması ve UI render döngüsünü tıkamaması.
  - Check-in ve sıra durumu görselleştirme testleri.
- **Entegrasyon Testleri (`RealtimeAppointmentHubIntegrationTests`):**
  - Anonim negotiate isteğinde `401/403` ret testi.
  - Kimliği doğrulanmış personel ve hekim bağlantısı üzerinden randevu oluşturma ve check-in işlemlerinde `SlotStatusChanged` ve `QueueUpdated` SignalR olaylarının başarılı teslimatı.
