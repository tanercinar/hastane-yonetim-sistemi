# Gözlemlenebilirlik Temeli

## Amaç ve sınır

F01-G09; teknik arıza ve gecikmeleri ilişkilendirmek için yapılandırılmış uygulama logu,
OpenTelemetry trace/metric ve istek süresi ölçümü sağlar. Bu katman bir audit trail değildir ve klinik
içeriğin kopyasını tutmaz. Teknik log/trace verisi `RET-TECH-LOG` kapsamında değerlendirilir; başlangıç
saklama hedefi 30 gündür.

## Güvenli istek olayı

Her HTTP isteğinin sonunda `RequestTelemetryMiddleware` tek bir source-generated olay üretir:

| Alan | İçerik | Gizlilik kuralı |
|---|---|---|
| `RequestMethod` | Allow-list içindeki HTTP metodu veya `OTHER` | İstemciden gelen serbest metin yazılmaz |
| `RouteTemplate` | `/api/v1/.../{resourceId}` biçiminde endpoint şablonu veya `unmatched` | Ham path ve route değeri yazılmaz |
| `StatusCode` | Sayısal HTTP durumu | Güvenli, düşük cardinality |
| `ElapsedMilliseconds` | Uygulama sınırında ölçülen süre | İçerik taşımaz |
| `CorrelationId` | Doğrulanmış `X-Correlation-ID` veya sunucunun ürettiği değer | Klinik içerik olarak kullanılmamalıdır |
| `TraceId` | 32 karakterlik W3C trace kimliği | Log ile span eşleştirmesi |

`2xx/3xx` olayları Information, `4xx` Warning, `5xx` Error seviyesindedir. Event ID `2100`'dür.
Request/response body, query string, header, cookie/token, kullanıcı adı, IP, user-agent, exception
mesajı ve stack trace bu olaya eklenmez.

JSON console formatter scope yayımlamaz. ASP.NET'in dahili request scope'u ham `RequestPath`
taşıyabildiği için `IncludeScopes=false` bir gizlilik kontrolüdür; bunu değiştirmek güvenlik incelemesi ve
canary regresyon testi gerektirir.

## Sınıflandırma ve redaction

`Microsoft.Extensions.Telemetry` log redaction hattı ve
`Microsoft.Extensions.Compliance.Redaction` varsayılan silici redactor ile etkinleştirilmiştir. Klinik
bir değerin teknik log API'sine verilmesi zorunlu hâle gelirse parametre `[ClinicalData]` ile
sınıflandırılır; varsayılan çıktı boşaltılır. Tercih edilen yaklaşım yine de bu değeri log çağrısına hiç
vermemektir.

Testing ortamındaki redaction probe yalnız integration testi içindir. Test; aynı canary değerini DTO
body, path parametresi ve query içine koyar, sınıflandırılmış log değerinin silindiğini ve log/span/metric
artefact'larında canary bulunmadığını doğrular. Bu endpoint OpenAPI'de yayınlanmaz ve Testing dışında
map edilmez.

## OpenTelemetry sinyalleri

| Sinyal | Ad | Güvenli alanlar |
|---|---|---|
| Activity source | `HospitalManagement.Host.Requests` | — |
| Span | `hospital.http.request` | method, route template, status, correlation ID |
| Meter | `HospitalManagement.Host.Requests` | — |
| Histogram | `hospital.http.server.request.duration` (`ms`) | method, route template, status |

Metric etiketlerinde correlation/trace/user/resource ID bulunmaz; bu hem veri minimizasyonu hem de
yüksek cardinality kontrolüdür. Otomatik ASP.NET Core instrumentation bilinçli olarak açılmamıştır;
varsayılan ham URL, ağ ve istemci alanlarının politika dışı taşınmasını önlemek için allow-list tabanlı
uygulama span'i kullanılır.

Development profilinde öğrenme ve yerel hata ayıklama amacıyla OpenTelemetry console exporter açıktır;
metric'ler yaklaşık beş saniyede bir yazılır. Console exporter Production'da doğrulama hatasıyla
reddedilir. Gelecekte OTLP collector eklenecekse endpoint/credential secret store'dan gelmeli, TLS ve
egress allow-list uygulanmalı ve yeni exporter canary testiyle doğrulanmalıdır.

Bu düşük hacimli DEMO temelinde yalnız allow-list alanlı uygulama span'leri `AlwaysOnSampler` ile
kaydedilir. Gerçek yük veya Production hedefi oluşursa örnekleme oranı ölçümle belirlenmeli; başarısız
span'lerin korunması ve sampling kararının loglarla uyumu ayrıca tasarlanmalıdır.

## Yerel doğrulama

Önce yerel altyapı ve user-secrets kurulumunu tamamla, ardından:

```powershell
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj
```

Ayrı terminalde güvenli bir correlation ID ile istek gönder:

```powershell
Invoke-WebRequest `
  -Uri http://localhost:5111/api/v1/platform/status `
  -Headers @{ 'X-Correlation-ID' = 'DEMO-observability-manual-001' }
```

Gerçek port için uygulamanın başlangıç çıktısını esas al. JSON logda event `2100`, correlation ID,
32 karakterlik trace ID, route şablonu, durum ve süre görünmelidir. Ham query/body/path değeri
görünmemelidir. Development console çıktısında aynı trace ID'li `hospital.http.request` span'i ve en geç
birkaç saniye içinde süre histogramı görülür.

Otomatik regresyon testi:

```powershell
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
  --filter "Roadmap=F01-G09"
```

## Resmî teknik kaynaklar

- [OpenTelemetry .NET tracing](https://opentelemetry.io/docs/languages/dotnet/traces/)
- [OpenTelemetry .NET metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/)
- [.NET source-generated logging ve classified redaction](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation)
- [.NET compliance/redaction kitaplıkları](https://learn.microsoft.com/en-us/dotnet/core/extensions/compliance)
