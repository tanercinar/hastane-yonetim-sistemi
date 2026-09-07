# Faz 10 Birlikte Çalışabilirlik Kapısı

## Amaç

Bu kapı FHIR R4, HL7 v2, DICOM/PACS, MHRS, e-Nabız, MEDULA/İTS/ÜTS ve SMS/e-posta
sınırlarının yalnız sentetik `DEMO` veri ve yerel `MOCK` uygulamalar olduğunu; gerçek kurum
endpoint'i, credential'ı veya ağ çağrısı içermediğini doğrular.

## Kapı sözleşmesi

- Mock motor timeout'u her denemede uygulanır; latency süre bütçesine dahildir.
- `TransientError` retry bütçesi içinde toparlanır ve tek teknik log üretir.
- `CorruptPayload` callback'i çalıştırmadan güvenli kodla başarısız olur.
- MHRS idempotency anahtarı aynı randevunun ikinci kez üretilmesini engeller.
- FHIR hasta export'u anonim için `401`, hasta için `403`, yetkili başhekim için `200` döner.
- FHIR MOCK çevrimdışıyken export `503 Problem Details` döner; exception ayrıntısı sızmaz.
- Mock yönetimi yalnız `interoperability.mock.manage` iznine sahip sistem yöneticisindedir.
- Sağlayıcı teslimatı tamamen başarısız olsa da ana randevu kaydı korunur; outbox güvenli
  `Failed` durumuna geçer.
- Entegrasyon logu serbest payload, klinik içerik, token, secret veya exception mesajı tutmaz.

## Otomatik kanıt

`MockServerIntegrationTests`, `FhirDemoIntegrationTests`, `MhrsIntegrationTests` ve
`NotificationInfrastructureIntegrationTests` gerçek PostgreSQL Testcontainers üzerinde
timeout, retry, bozuk cevap, duplicate/idempotency, yetkisiz export ve ana kayıt dayanıklılığını
doğrular. Tüm Faz 10 testleri `Roadmap~F10` filtresiyle çalıştırılır.

## Manuel kabul

Tekrarlanabilir manuel adımlar ve sonuç matrisi repository kökündeki
[`F10_Test.md`](../../F10_Test.md) belgesindedir. Uygulamada ayrı entegrasyon yönetim UI'ı
bulunmadığı için manuel kabul, tarayıcı Network paneli veya aynı HTTP sözleşmesini kullanan
PowerShell çağrılarıyla yapılır.
