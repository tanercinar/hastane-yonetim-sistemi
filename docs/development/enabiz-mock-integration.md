# e-Nabız Mock Entegrasyon Rehberi

> **MOCK** — Bu modül gerçek e-Nabız (Sağlık Bakanlığı Sağlık.NET) sunucusuna bağlanmaz.
> Tüm işlemler yerel sentetik veri ile simüle edilir ve `IntegrationMockEngine`
> üzerinden geçer.

## 1. Genel Bakış

e-Nabız mock entegrasyonu, hastanenin sağlık verisi paketlerini e-Nabız sistemine
iletme sürecini simüle eder. Altı farklı veri paketi türü desteklenir:

| Paket | Kod | Açıklama |
|-------|-----|----------|
| Package101HastaKayit | 101 | Hasta kayıt bildirimi |
| Package102HizmetIstem | 102 | Hizmet istem bildirimi |
| Package103LaboratuvarSonuc | 103 | Laboratuvar sonuç bildirimi |
| Package104RadyolojiSonuc | 104 | Radyoloji sonuç bildirimi |
| Package105Recete | 105 | Reçete bildirimi |
| Package106Epikriz | 106 | Epikriz (taburcu özeti) bildirimi |

## 2. Hasta Rıza Yönetimi

e-Nabız veri gönderimi hasta rızasına bağlıdır:

- `HasPatientConsent = true` → Paket **Queued** durumuna alınır ve gönderilebilir.
- `HasPatientConsent = false` → Paket **ConsentDenied** olarak işaretlenir, gönderim engellenir.

Rıza olmadan gönderim denendiğinde `InvalidOperationException` fırlatılır.

## 3. Gönderim Yaşam Döngüsü

```
[Enqueue] → Queued → [Send] → Transmitting → Successful / Failed
                                    ↑
                              [Retry] (RetryCount++)
```

Her gönderim kaydı otomatik bir `SysTakipNo` (sistem takip numarası) ile oluşturulur.

## 4. REST API Uç Noktaları

Temel URL: `/api/v1/interoperability/enabiz`

| Metot | Yol | Açıklama |
|-------|-----|----------|
| `GET` | `/queue` | Gönderim kuyruğunu sorgula (`?status=Queued&patientId=...`) |
| `POST` | `/queue` | Yeni paket kuyruğa ekle |
| `POST` | `/transmissions/{id}/send` | Kuyruktaki paketi gönder |
| `POST` | `/transmissions/{id}/retry` | Başarısız paketi yeniden dene |
| `GET` | `/transmissions/{id}` | Tekil gönderim kaydını getir |

### 4.1 Kuyruğa Ekleme İsteği

```json
{
  "packageType": 101,
  "patientId": "d3b07384-d9a0-4e9c-8f3a-1a2b3c4d5e6f",
  "patientNationalId": "11111111110",
  "hasPatientConsent": true,
  "payloadSummary": "DEMO hasta kayıt paketi"
}
```

### 4.2 Yanıt Örneği

```json
{
  "id": "a1b2c3d4-...",
  "sysTakipNo": "SYS-AB12CD34EF56",
  "packageTypeCode": 101,
  "packageTypeName": "Package101HastaKayit",
  "patientId": "d3b07384-...",
  "patientNationalId": "11111111110",
  "hasPatientConsent": true,
  "status": "Queued",
  "payloadSummary": "DEMO hasta kayıt paketi",
  "responseCode": "QUEUED",
  "responseMessage": "Paket gönderim kuyruğuna eklendi.",
  "retryCount": 0,
  "queuedAtUtc": "2026-09-01T08:00:00Z",
  "sentAtUtc": null,
  "lastAttemptAtUtc": null
}
```

## 5. Veritabanı

Tablo: `interoperability.enabiz_transmissions`

| Sütun | Tip | Açıklama |
|-------|-----|----------|
| Id | uuid (PK) | Kayıt kimliği |
| SysTakipNo | varchar(50) UNIQUE | Sistem takip numarası |
| PackageType | int | Paket türü (101-106) |
| PatientId | uuid | Hasta kimliği |
| PatientNationalId | varchar(20) | TC kimlik numarası |
| HasPatientConsent | boolean | Hasta rızası |
| Status | varchar(30) | Durum (Queued, Transmitting, Successful, Failed, ConsentDenied) |
| PayloadSummary | varchar(4000) | Sanitize edilmiş yük özeti |
| ResponseCode | varchar(50) | Yanıt kodu |
| ResponseMessage | varchar(500) | Yanıt mesajı |
| RetryCount | int | Deneme sayacı |
| QueuedAtUtc | timestamp with tz | Kuyruğa alınma zamanı |
| SentAtUtc | timestamp with tz | Gönderilme zamanı (nullable) |
| LastAttemptAtUtc | timestamp with tz | Son deneme zamanı (nullable) |

İndeksler: `PatientId`, `Status`, `SysTakipNo` (unique).

## 6. Mock Engine Entegrasyonu

Gönderim işlemi `IntegrationMockEngine.ExecuteAsync` üzerinden geçer:
- Hata enjeksiyonu, gecikme ve devre kesici davranışları konfigüre edilebilir.
- `ExternalSystemType.ENabiz` sistem türü kullanılır.
- Korelasyon kimliği: `CORR-ENABIZ-{SysTakipNo}`.

## 7. Güvenlik ve Gizlilik

- Gerçek hasta verisi kullanılmaz; yalnız DEMO/sentetik veri.
- `PayloadSummary` alanında klinik içerik yerine özet bilgi tutulur.
- PHI (Protected Health Information) loglara, telemetriye veya URL'ye yazılmaz.

## 8. Test Kapsamı

| Test Dosyası | Test Sayısı | Açıklama |
|-------------|-------------|----------|
| `ENabizDomainTests.cs` | 5 | Domain model davranışları |
| `ENabizIntegrationTests.cs` | 3 | PostgreSQL + API uç nokta testleri |
