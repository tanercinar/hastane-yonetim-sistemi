# Faz 11 Raporlama ve Operasyon Panoları Kapısı

## Amaç

Bu kapı, Hastane Yönetim Sistemi projesinde **Faz 11 — Raporlama ve operasyon panoları** kapsamında geliştirilen okuma modelleri, projeksiyon altyapısı, operasyon panoları, güvenli CSV dışa aktarımı ve SignalR gerçek zamanlı iletişim zincirinin güvenlik, dayanıklılık ve performans standartlarını karşıladığını doğrular.

## Kapı sözleşmesi

1. **Okuma Modeli ve Idempotency:** Modül olaylarından (`AppointmentProjectedEvent`, `DiagnosticProjectedEvent`, `BedOccupancyProjectedEvent`, `PharmacyProjectedEvent`) beslenen read model güncellemeleri `ProjectionProcessedEvent` üzerinden `EventId` bazlı tam idempotenttir.
2. **Yeniden Kurma (Rebuild):** `POST /api/v1/reporting/projections/rebuild` komutu veri kaybı veya model değişimi durumunda tüm projeksiyonları sıfırdan tutarlı olarak yeniden inşa eder.
3. **Kapsam ve Gizlilik Sınırı:**
   - Yönetim panolarında hasta adları ve kişisel veriler gereksiz yere listelenmez.
   - Hekimler yalnızca kendi atandıkları bölüm ve hastalarıyla sınırlandırılmış sayaçları görür.
   - Tanısal panoda kritik sonuçlardan klinik drill-down yapılması ayrıca klinik yetki ve bakım ilişkisi kontrolü gerektirir.
4. **Kapsam Dışı Finansal Veri İzolasyonu:** Eczane ve stok panosu yalnız operasyonel miktarları (reçete sayıları, stok adetleri, miat tarihleri) gösterir; hiçbir birim fiyat, satınalma veya fatura verisi içermez.
5. **Dışa Aktarma Güvenliği (CWE-1236 & DoS):**
   - Dışa aktarma işlemi `report.operations.export` iznine bağlıdır (yalnızca ChiefMedicalOfficer ve HospitalManager); hekim veya personel `403 Forbidden` alır.
   - CSV formül enjeksiyonuna karşı `=`, `+`, `-`, `@`, `\t`, `\r` ile başlayan tüm hücreler tek tırnak (`'`) ile nötralize edilir.
   - Tek seferde azami 5000 satır sınırı (`MaxExportRows = 5000`) uygulanır; aşan istekler engellenir.
   - Dışa aktarılan veriler diskte veya statik klasörlerde kalıcı dosya olarak tutulmaz; doğrudan bellek içi akış (`UTF-8 BOM`) ile istemciye iletilir.
   - Tüm dışa aktarmalar `report.operations.export` audit denetim iziyle günlüğe kaydedilir.
6. **SignalR Ölçek ve Dayanıklılık:**
   - İstemciler SignalR hub metotlarına keyfi girdi vererek başka bölümün grubuna (`JoinDepartmentGroup`) katılamaz (TM-13 kontrolü).
   - Olaylar monotonik artan sıra numarası (`SequenceNumber`) ile istemciye iletilir.
   - Bağlantı kopması durumunda Blazor istemcisi otomatik olarak yeniden bağlanır (`.WithAutomaticReconnect()`) ve `Reconnected` olayında sunucudan kanonik durumu yeniden çeker.
   - Projeksiyon gecikmesi (`GET /api/v1/reporting/projections/lag`) üzerinden canlı izlenir.

## Otomatik kanıt

`Phase11ReportingGateIntegrationTests`, `ReportingIntegrationTests`, `ReportingSignalRTests`, `SecureExportServiceTests` ve ilgili birim testleri PostgreSQL Testcontainers üzerinde kaynak işlem → projeksiyon → dashboard → SignalR zincirini, yetki sınırlarını ve export saldırı korumalarını doğrular.

## Manuel kabul

Tekrarlanabilir manuel adımlar ve sonuç matrisi repository kökündeki [`F11_Test.md`](../../F11_Test.md) belgesindedir.
