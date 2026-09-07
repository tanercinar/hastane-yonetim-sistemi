# SMS/E-posta Sağlayıcı Mock'u (F10-G08)

Bu belge, bildirim modülündeki dış kanal sınırını ve güvenli yerel yakalayıcıyı tanımlar.
Uygulama gerçek SMTP, SMS ağ geçidi veya internet uç noktasına bağlanmaz.

## Mimari sınır

- `INotificationProviderPort`, uygulamanın e-posta/SMS teslim sözleşmesidir.
- `RetryingNotificationProviderPort`, geçici hataları en çok üç kez dener. Son hata yalnız
  `MOCK_NOTIFICATION_PROVIDER_FAILED` güvenli koduyla döner; exception ayrıntısı saklanmaz.
- `INotificationTransport` dış sistem sınırıdır. Yerel geliştirmede
  `LocalNotificationCaptureTransport` kullanılır.
- Yakalanan iletiler `notifications.mock_deliveries` tablosunda
  `MOCK-LOCAL-CAPTURE` sağlayıcı etiketi, kanal, locale, şablon, deneme sayısı ve
  idempotency anahtarıyla tutulur. Benzersiz `(idempotency_key, channel)` indeksi çift
  teslimatı engeller.

Bu yakalayıcı yalnız sentetik `DEMO` alıcılarla kullanılmalıdır; gerçek iletişim bilgisi veya
kurum credential'ı eklenmez.

## Şablon ve hassas içerik filtresi

Dış kanal başlık ve gövdesi outbox içindeki serbest metinden üretilmez. Yalnız
`NotificationTemplateCatalog` allowlist'indeki şablon ve token'lar render edilir:

| Şablon | İzinli token'lar |
|---|---|
| `Appointment.Booked` | `appointmentDate`, `reference` |
| `Appointment.Cancelled` | `appointmentDate` |
| `Appointment.CheckedIn` | `queueNumber` |

Tanı, test sonucu, ICD kodu, klinik not veya allowlist dışındaki diğer token'lar dış kanala
taşınmaz. Bilinmeyen olaylar, klinik ayrıntı içermeyen `Notification.Generic` portal mesajına
dönüştürülür. Serbest metin yalnız alıcının kendi uygulama içi bildiriminde kalır.

Desteklenen locale değerleri `tr-TR` ve `en-US`'tir. Desteklenmeyen locale tercihi API'de
`400 Bad Request` ile reddedilir.

## Kullanıcı tercihleri ve yetki

- Varsayılan: e-posta açık, SMS kapalı, locale `tr-TR`.
- `GET /api/v1/notifications/preferences` için `identity.profile.view-own` gerekir.
- `PUT /api/v1/notifications/preferences` için CSRF koruması ve
  `identity.profile.edit-own` gerekir.
- API bir `PersonId` kabul etmez. Kayıt kapsamı her zaman kimliği doğrulanmış kullanıcının
  `PersonId` claim'inden türetilir; başka kişinin tercihi okunamaz veya değiştirilemez.

## Doğrulama kapsamı

- Birim testleri şablon token filtresini, iki locale'i, kanal tercihini, başarılı retry'ı ve
  retry bütçesi sonunda hata ayrıntısının sızmamasını doğrular.
- PostgreSQL Testcontainers testleri anonim erişimin `401` olduğunu, tercihlerin yalnız oturum
  sahibine yazıldığını, e-posta/SMS kanal seçimini, locale'i, yerel yakalamayı ve tanı/test
  sonucu canary değerlerinin dış iletide bulunmadığını doğrular.
- Randevu olay üreticileri zorunlu allowlist token'larını outbox'a ekler. Outbox yeniden
  işlendiğinde kanal başına aynı idempotency anahtarıyla ikinci teslimat oluşmaz.
- Sağlayıcı üç denemede de başarısız olduğunda outbox güvenli `Failed` durumuna geçer;
  ana Scheduling randevusu geri alınmaz. Sağlayıcı exception metni randevu yanıtına veya
  outbox hata alanına kopyalanmaz.
