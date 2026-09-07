# Gizlilik İncelemesi ve Veri Minimizasyonu Sertleştirmesi (F13-G10)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin KVKK ve GDPR veri koruma ilkelerine tam uyumunu; uygulama logları, telemetri, izleme (tracing), hata mesajları, URL sorgu parametreleri, SignalR canlı yayınları, mobil kilit ekranı bildirimleri ve veri dışa aktarma (export) süreçlerinde korumalı sağlık verisi (PHI) ve doğrudan kişisel verilerin sızmasını önleyen mimari güvenlik kontrollerini belgeler.

## 2. Gizlilik ve Veri Minimizasyonu Kontrolleri

### 2.1. Sentetik Veri ve Canary İşaretleyicisiyle Sızıntı Testi
- **Yalnızca Sentetik Veri**: Sistemde hiçbir gerçek hasta, hekim, kimlik numarası veya gerçek e-posta verisi bulunmaz; tüm veriler belirgin `DEMO-*` ön eki ve RFC 2606 `.invalid` alan adı kullanır.
- **Canary Test Güvencesi**: `PrivacyReviewAndDataMinimizationTests` içerisinde tanımlanan sentetik canary kimlik belirteçleri (`DEMO-CANARY-TR-*`, `CanaryFirst`, `canary.patient@demo.invalid`) kullanılarak anonimleştirme işlemi test edilmiştir. Anonimleştirme sonrasında hiçbir canary belirtecinin saklanmadığı kanıtlanmıştır.

### 2.2. KVKK / GDPR Unutulma Hakkı ve Anonimleştirme (`Patient.Anonymize`)
- **Geri Döndürülemez Temizleme**: Hasta verileri anonimleştirildiğinde isim `ANONİM`, soyisim `HASTA` olarak güncellenir; TCKN, telefon, e-posta, açık adres ve acil durum irtibatı `null` yapılarak kalıcı olarak imha edilir.
- **İlişkisel Bütünlüğün Korunması**: Hastanın birincil anahtarı (`Id`) ve protokol numarası (`Mrn`) korunarak geçmiş karşılaşmalar, imzalı klinik notlar, reçeteler ve adli denetim izlerinin ilişkisel bütünlüğü bozulmaz.

### 2.3. Loglama ve Telemetri Minimizasyonu
- **Sıfır Klinik Veri Yasağı**: Uygulama loglarında ve OpenTelemetry izlerinde tanı metinleri, reçete detayları, hasta adları veya kimlik numaraları kesinlikle loglanmaz.
- **Hata Mesajı Ayrıştırması**: `ApiExceptionHandler` loglarında yalnızca `ExceptionType` ve `CorrelationId` saklanır; iç istisna mesajları ve SQL parametreleri loglanmaz.

### 2.4. URL ve Rota Parametresi Güvenliği
- **Klinik Verisiz Rota Parametreleri**: URL yol ve sorgu parametrelerinde (`GET /api/v1/...`) yalnızca anlamsız teknik GUID'ler veya güvenli sentetik protokol numaraları (`DEMO-APT-*`, `DEMO-ENC-*`) kullanılır; tanı adı, hasta adı veya ilaç adı asla URL'de taşınmaz.

### 2.5. SignalR ve Canlı Yayın Veri Minimizasyonu
- **Tek Başına Bilgi Vermeyen Payload**: `EmergencyRealtimeNotifier` ve `InpatientRealtimeNotifier` gibi SignalR yayınlarında hasta adı veya klinik içerik yayınlanmaz. Yalnızca `ChangedAtUtc` zaman damgası yayınlanarak istemcilerin yetkili API üzerinden kendi izinleri dahilinde veri tazelemesi sağlanır.

### 2.6. Kilit Ekranı Bildirimlerinde PHI Temizliği (`SafeNativeNotificationFormatter`)
- **Genel Kilit Ekranı Önizlemesi**: Mobil işletim sistemi bildirimlerinde tanı, ilaç etken maddesi, dozaj ve sayısal laboratuvar değerleri temizlenir; yalnızca genel bildirim başlıkları ("Yeni E-Reçete Düzenlendi", "Sonuç Bildirimi") gösterilir.

### 2.7. Güvenli CSV Dışa Aktarma ve Formül Enjeksiyonu Koruması
- **Maksimum 5000 Satır Koruması**: `SecureExportService.MaxExportRows` ile bellek tüketimi sınırlandırılır.
- **Formül Nötralizasyonu**: `=cmd|...`, `@SUM...`, `-...`, `+...` ile başlayan tüm hücreler tek tırnak (`'`) ile nötralize edilerek RFC 4180 çift tırnak koruması uygulanır.
- **Diskte Geçici Dosya Bırakmama**: CSV dışa aktarımı doğrudan UTF-8 BOM bellek akışı olarak istemciye iletilir.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Privacy/PrivacyReviewAndDataMinimizationTests.cs`:
  - `SyntheticCanaryMarkersAreIrreversiblyErasedUponPatientAnonymization`
  - `SecureCsvExportSanitizesFormulaInjectionAndEscapesSpecialCharacters`
  - `SecureCsvExportEnforcesStrictRowCapToPreventMemoryExhaustion`
- `tests/HospitalManagement.IntegrationTests/EmergencyRealtimePrivacyTests.cs`:
  - `EmergencyRealtimeEventsContainOnlyRefreshTimestamp`
- `tests/HospitalManagement.ComponentTests/Security/NativeDeepLinkAndNotificationSecurityTests.cs`:
  - `SafeNativeNotificationFormatterStripsProtectedHealthInformationFromLockScreenPreview`
  - `SafeNativeNotificationFormatterStripsLaboratoryResultsFromPreview`
