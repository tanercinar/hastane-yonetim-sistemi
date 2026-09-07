# ADR-0002: API-Öncelikli Blazor ve Çoklu İstemci Modeli

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **İlgili:** ADR-0001, Faz 1 ve Faz 12

## Bağlam

İlk teslim web uygulaması; sonraki istemciler Windows ve Android'dir. Web geliştirme hızı korunurken iş kurallarının Blazor bileşenlerine veya server-rendered UI oturumuna bağlanması, native istemciler geldiğinde yeniden yazım doğurur. Tamamen ayrı JavaScript frontend ise tek geliştirici için ikinci dil/ekosistem ve UI tekrarını artırır.

## Karar

- Sunucu ASP.NET Core REST/JSON API'yi `/api/v1` sınırında sunar ve OpenAPI üretir.
- İlk web istemcisi .NET 10 **Blazor Web App, Interactive WebAssembly** modelini kullanır.
- Web istemcisi de domain/veritabanına doğrudan erişmez; typed API client ve açık DTO/contract kullanır.
- Paylaşılabilir, host-agnostic Razor bileşenleri `HospitalManagement.UI` Razor Class Library'de tutulur.
- Platform özelliği gerektiren UI, interface üzerinden çağrılır ve Web/MAUI host'ları ayrı implementation verir.
- Windows ve Android, .NET MAUI Blazor Hybrid host'ları olarak aynı API'yi ve uygun RCL bileşenlerini kullanır.
- API hataları RFC 9457 Problem Details benzeri kararlı sözleşmeyle; doğrulama hataları alan bazlı, güvenli kodlarla döner.
- API versioning tasarıma hazır olur; v1 erken geliştirmede gereksiz çok sürüm taşınmaz.
- Gerçek zamanlı olaylar SignalR ile taşınır; istemci reconnect sonrası sunucudan canonical durumu yeniden çeker.
- Çevrimdışı veri yazma ve local klinik cache yoktur.

## Alternatifler

### Blazor Interactive Server'ı tek uygulama katmanı yapmak

Reddedildi: web için hızlı olsa da UI'yi sunucu devresine bağlar ve native istemci/API sınırını erteleyebilir. Interactive Server belirli server-only yönetim sayfalarında ancak ayrı kararla kullanılabilir.

### React/Angular/Vue SPA

Reddedildi: güçlü seçeneklerdir ancak bu C# odaklı, tek geliştiricili eğitim ürününde ikinci ana teknoloji yığınının maliyeti tercih edilmedi.

### Ayrı WPF + native Android UI

Reddedildi: maksimum platform özelliği sağlarken UI tekrarını büyütür. MAUI Blazor Hybrid portföy hedefi için yeterlidir.

### GraphQL veya gRPC

Şimdilik reddedildi: REST/OpenAPI rol/kaynak güvenliği ve portföy anlaşılabilirliği için yeterlidir. İç performans ihtiyacı kanıtlanırsa yeniden değerlendirilir.

## Sonuçlar

### Olumlu

- Tek C#/.NET yetkinlik hattı ve yüksek UI paylaşımı.
- API contract testleriyle istemci bağımsızlığı.
- Web tamamlanırken native hedeflerin mimari zemini hazır olur.
- Güvenlik kararları her istemcide aynı sunucu sınırında uygulanır.

### Olumsuz

- Interactive WebAssembly ilk yük boyutu ve API çağrı karmaşıklığı getirir.
- Tüm bileşenler %100 paylaşılmaz; mobil/masaüstü gezinme farklı olabilir.
- Cookie web kimliği ile native OIDC iki istemci auth desenini gerektirir.

## Uygulama korumaları

1. UI projesi EF Core/modül Infrastructure'a referans veremez.
2. DTO'lar entity değildir ve hassas alanları varsayılan taşımamalıdır.
3. RCL platform API'sine doğrudan bağımlı olamaz.
4. API authorization her istemci için aynıdır; UI gizleme güvenlik sayılmaz.
5. Contract/OpenAPI değişikliği integration ve istemci testini tetikler.

## Yeniden değerlendirme koşulları

- Blazor/MAUI destek veya erişilebilirlik engeli kanıtlanırsa
- Cihaz özelliği ortak UI'dan daha ağır basarsa
- Ölçülmüş performans/indirme bütçesi Interactive WASM'ı karşılamazsa
- Yeni istemci C# contract tüketemiyorsa (API yine kararlı kalır)
