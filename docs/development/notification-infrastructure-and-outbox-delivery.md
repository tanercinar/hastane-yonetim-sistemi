# Bildirim Altyapısı ve Güvenilir Outbox Teslimatı (F03-G07)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Bildirim Altyapısı (Notification Infrastructure and Outbox Delivery)** mimarisini, outbox/idempotency desenini, gizlilik standartlarını ve doğrulama süreçlerini açıklar.

## 1. Mimari Prensipler ve Outbox Deseni

- **Outbox Olay Modeli (`notifications.notification_outbox_events`):**
  - Randevu oluşturma (`Appointment.Booked`), iptal (`Appointment.Cancelled`) ve check-in (`Appointment.CheckedIn`) olayları doğrudan dış servise bağımlı olmadan outbox tablosuna yazılır.
  - Her outbox kaydı benzersiz bir tekillik anahtarı (`IdempotencyKey`, örn: `appt-booked-{id}`) içerir (`ux_notification_outbox_idempotency_key`).
- **Idempotent İşleme ve Çift Bildirim Engeli:**
  - `ProcessOutboxAsync` işleyicisi bekleyen olayları işlerken, uygulama içi bildirim (`InAppNotification`) ve mock teslimat (`MockDeliveryRecord`) tablolarında `IdempotencyKey` kontrolü yapar.
  - Aynı olayın tekrar işlenmesi durumunda yeni kayıt üretilmez; çift bildirim (duplicate notification) engellenir.
- **Klinik Gizlilik Standardı (Privacy by Design):**
  - Bildirim başlığı ve gövdesi C3/C4 düzeyinde hassas klinik teşhis, ICD-10 tanı kodu veya hekim klinik notu İÇERMEZ.
  - Yalnızca randevu tarihi, referans numarası, poliklinik genel bilgisi ve sıra numarası yer alır.
- **Mock Teslimat Kanalları (`notifications.mock_deliveries`):**
  - E-posta (`Email`) ve SMS (`Sms`) gönderimleri `DEMO-*@hospital.invalid` adreslerine mock teslimat kayıtları olarak loglanır. Dış internete veya gerçek SMTP/SMS sağlayıcılarına çağrı yapılmaz.
  - F10-G08 ile sağlayıcı sınırı, güvenli şablon ve kullanıcı tercihleri için
    [`notification-provider-mock.md`](notification-provider-mock.md) sözleşmesi eklenmiştir.

## 2. Arayüz Bileşenleri (`NotificationList.razor`)

- **Bildirimlerim Ekranı (`/notifications`):**
  - Kullanıcının kendine ait okunmuş ve okunmamış bildirimlerini kronolojik olarak listeler.
  - Okunmamış bildirim sayısı özeti ve belirteci.
  - "Okundu İşaretle" aksiyon butonu (`POST /api/v1/notifications/{id}/read`).
  - IDOR koruması: Kullanıcı yalnızca kendi `PersonId`'sine ait bildirimleri listeleyebilir ve güncelleyebilir.

## 3. Test ve Doğrulama

- **Birim Testleri (`NotificationDomainUnitTests`):**
  - Outbox durum geçişleri (`Pending -> Processed` / `Failed`).
  - In-app bildirim okundu işaretleme (`MarkAsRead`).
  - Mesaj gövdesinde hassas klinik veri bulunmadığının doğrulanması.
- **Bileşen Testleri (`NotificationComponentTests`):**
  - Bildirim listesi, okunmamış rozetleri ve Türkçe metin doğrulaması.
  - Okundu işaretleme buton etkileşimi.
  - Yetkisiz erişimde `ForbiddenState` görünümü.
- **Entegrasyon Testleri (`NotificationInfrastructureIntegrationTests`):**
  - Gerçek PostgreSQL üzerinde randevu oluşturma sonrası outbox olayı ve mock e-posta kaydı üretimi.
  - Outbox tekrar çalıştırıldığında çift bildirim oluşmadığının (idempotency) testi.
  - Hastanın kendi bildirimini listelemesi ve okundu işaretlemesi.
  - IDOR negatif testi: Başka kullanıcının bildirimini okundu yapmaya çalışırken `404 Not Found` yanıtı.
