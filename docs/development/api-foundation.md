# API temeli

Bu belge `F01-G07` ile kurulan HTTP API sözleşmesini ve yerel doğrulama adımlarını açıklar.
API yalnız sentetik `DEMO` veriyle çalışır; gerçek bir HBYS veya klinik karar desteği değildir.

## Sürümleme ve uç noktalar

Uygulama uç noktaları `/api/v1` öneki altında yayınlanır. İlk güvenli durum uç noktası:

```text
GET /api/v1/platform/status
```

Yanıt; servis adını, `v1` API sürümünü, `DEMO` veri modunu ve isteğin correlation ID'sini içerir.
Yeni API uç noktaları aynı sürümlü route grubuna eklenmelidir.

Altyapı uç noktaları sürümlü iş API'sinden ayrıdır:

| Uç nokta | Amaç | Başarı ölçütü |
|---|---|---|
| `GET /health/live` | İşlemin HTTP isteği kabul edebildiğini gösterir. | `200 Healthy`; bağımlılık kontrolü yoktur. |
| `GET /health/ready` | İşlemin trafik almaya hazır olduğunu gösterir. | PostgreSQL `SELECT 1` kontrolü başarılıysa `200`; değilse `503`. |
| `GET /openapi/v1.json` | `v1` OpenAPI belgesini yayınlar. | Yalnız `Development` ve `Testing` ortamlarında bulunur. |

Health yanıtları bağımlılık adı ve durumuyla sınırlıdır. Bağlantı metni, exception, açıklama ve süre
istemciye verilmez. Uç noktalar cache'lenmez ve iş API'sinin rate-limit kotasını tüketmez.

## Problem Details sözleşmesi

API hata yanıtları `application/problem+json` içerik türünde RFC 9457 Problem Details kullanır.
Standart alanlara iki kararlı extension eklenir:

- `code`: istemcinin karar vermesi için güvenli, makinece okunabilir hata kodu.
- `correlationId`: sunucu loglarıyla eşleştirme anahtarı; klinik içerik taşımaz.

Örnek bir `404` yanıtı:

```json
{
  "type": "https://hospital-management.invalid/problems/not_found",
  "title": "Kaynak bulunamadı",
  "status": 404,
  "detail": "İstenen kaynak bulunamadı.",
  "instance": "urn:hospital-management:request:DEMO-api-check-001",
  "code": "not_found",
  "correlationId": "DEMO-api-check-001"
}
```

Doğrulama hatalarında `code` değeri `validation_failed` olur. `errors` nesnesindeki alan adları
camelCase, mesajlar ise güvenli ve kararlı kodlardır (`required`, `length_out_of_range` gibi).
Beklenmeyen exception metni, türü, stack trace'i ve yerel dosya yolu yanıta kopyalanmaz.

## Correlation ID

İstemci isteğe tek bir `X-Correlation-ID` başlığı ekleyebilir. Kabul edilen değer:

- 1-64 karakterdir;
- yalnız ASCII harf/rakam ile `-`, `_` ve `.` içerir;
- hasta, klinik kayıt, token veya secret içermez.

Eksik, birden fazla veya geçersiz değer güvenli bir sunucu GUID'i ile değiştirilir. Aynı değer başarı
ve hata yanıt başlığında ve JSON sözleşmesinde döner.

## Rate limit

Tüm `/api/v1` uç noktaları ortak sabit pencere politikasına bağlıdır. Varsayılan değerler:

```json
{
  "HospitalManagement": {
    "Api": {
      "RateLimit": {
        "PermitLimit": 120,
        "WindowSeconds": 60
      }
    }
  }
}
```

Kuyruk kapalıdır. Kota aşıldığında API `429 rate_limit_exceeded` Problem Details ve mümkünse
`Retry-After` başlığı döndürür. Dağıtık/çok instance'lı üretim kotası bu yerel limiter'ın kapsamı
dışındadır ve deployment tasarımında merkezi bir çözümle ele alınmalıdır.

## Yerel çalıştırma ve manuel kontrol

Önce `docs/development/configuration-and-secrets.md` ve
`docs/development/local-infrastructure.md` adımlarıyla Development secret'larını ve PostgreSQL'i
hazırla. Sonra proje kökünde:

```powershell
dotnet run --project src/HospitalManagement.Host/HospitalManagement.Host.csproj
```

Terminalde yazılan yerel HTTP adresini aşağıdaki `$baseUrl` değerine ata:

```powershell
$baseUrl = "http://localhost:5111"
$headers = @{ "X-Correlation-ID" = "DEMO-manual-api-001" }

Invoke-RestMethod "$baseUrl/api/v1/platform/status" -Headers $headers
Invoke-RestMethod "$baseUrl/health/live"
Invoke-RestMethod "$baseUrl/health/ready"
Invoke-RestMethod "$baseUrl/openapi/v1.json"
```

Olumsuz sözleşmeyi görmek için:

```powershell
try {
    Invoke-WebRequest "$baseUrl/api/v1/olmayan-kaynak" -Headers $headers
} catch {
    $_.Exception.Response.StatusCode
    $_.ErrorDetails.Message
}
```

Beklenen sonuç `404`, `application/problem+json`, `not_found` kodu ve aynı correlation ID'dir.

## Otomatik doğrulama

Docker Desktop çalışırken yalnız bu görevin gerçek host + PostgreSQL sözleşme testlerini çalıştır:

```powershell
dotnet test tests/HospitalManagement.IntegrationTests/HospitalManagement.IntegrationTests.csproj `
  -c Release `
  --filter "Roadmap=F01-G07"
```

Testler başarı, geçersiz correlation ID, OpenAPI görünürlüğü, liveness/readiness, `404`, alan bazlı
doğrulama, güvenli `500` ve rate-limit `429` yollarını kapsar.

## Resmî teknik kaynaklar

- [ASP.NET Core OpenAPI](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [ASP.NET Core API error handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api)
- [ASP.NET Core rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
- [ASP.NET Core health checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
