# Yapılandırma ve secret yönetimi

Host yalnız `Development`, `Testing` ve `Production` environment adlarını kabul eder. Ortak güvenlik değişmezleri `appsettings.json`, ortamın secretsız ayarları `appsettings.{Environment}.json`, hassas değerler ise repo dışındaki provider'lar üzerinden yüklenir. ASP.NET Core'un varsayılan önceliğinde environment dosyası ortak dosyayı; Development user-secrets bunu; environment değişkenleri de önceki kaynakları geçersiz kılar.

## Yapılandırma sözleşmesi

| Anahtar | Development | Testing | Production-benzeri | Hassas |
|---|---|---|---|---:|
| `HospitalManagement:Runtime:DataMode` | `DEMO` | `DEMO` | `DEMO` | Hayır |
| `HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled` | `false` | `false` | `false` | Hayır |
| `ConnectionStrings:HospitalDatabase` | user-secrets | Test fixture/override | Deployment secret provider | Evet |
| `HospitalManagement:ObjectStorage:Endpoint` | appsettings.Development | appsettings.Testing | Deployment config; HTTPS zorunlu | Hayır |
| `HospitalManagement:ObjectStorage:AccessKey` | user-secrets | Test fixture/override | Deployment secret provider | Evet |
| `HospitalManagement:ObjectStorage:SecretKey` | user-secrets | Test fixture/override | Deployment secret provider | Evet |
| `HospitalManagement:Email:*` | Yerel Mailpit `MOCK` | İzole `MOCK` | Yalnız `MOCK` | Hayır |

Her environment uygulama başlangıcında options doğrulamasından geçer. Eksik bağlantı dizesi/nesne depolama kimliği, bilinmeyen environment, `DEMO` dışı veri modu, gömülü AI, gerçek e-posta modu, geçersiz `.invalid` göndereni veya Production'da TLS'siz nesne depolama uygulamayı anahtar adını veren fakat değeri göstermeyen hata ile durdurur.

## İlk yerel kurulum

Önce yerel altyapıyı ve ignore edilen `.env` dosyasını hazırla:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
```

Ardından `.env` içindeki rastgele yerel PostgreSQL/MinIO bilgilerini Host'un Development user-secrets deposuna aktar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
```

Script Host projesindeki `UserSecretsId` değerini okuyarak işletim sisteminin standart Development user-secrets deposunu bulur. Mevcut JSON nesnesini koruyup yalnız üç anahtarı bellekte birleştirir; UTF-8 BOM içermeyen geçici dosyayı aynı dizinde oluşturur ve hedef dosyayla atomik olarak değiştirir. Bu yöntem Windows PowerShell 5.1'in pipe çıktısına BOM ekleyebilmesi nedeniyle seçilmiştir. Secret değerleri komut satırına, terminal çıktısına veya repo içindeki geçici dosyaya yazılmaz; başka user-secret anahtarları temizlenmez. İşlem yarıda kalırsa aynı dizindeki açıkça adlandırılmış geçici/yedek dosyalar `finally` bloğunda kaldırılır.

Development scripti G04'teki yalnız yerel MinIO yönetici kimliğini mock nesne depolama bağlantısı için yeniden kullanır. Bu kolaylık Production-benzeri ortam için geçerli değildir; gerçek deployment kararı verilirse uygulamaya ayrı ve en az yetkili nesne depolama kimliği sağlanmalıdır.

Sonra uygulamayı çalıştır:

```powershell
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj
```

User-secrets yalnız geliştirme kolaylığıdır; şifreli veya üretim için güvenilir bir kasa değildir. Production-benzeri deployment hedefi seçildiğinde yönetilen secret provider kullanılmalıdır. Environment değişkenleriyle hiyerarşik anahtar vermek gerekirse platformlar arası ayraç çift alt çizgidir; örneğin `ConnectionStrings__HospitalDatabase`. Secret değerlerini shell geçmişine, `launchSettings.json` dosyasına veya CI loguna yazma.

## Güvenli hata ve log davranışı

- Başlangıç doğrulama mesajları yalnız eksik/geçersiz anahtar adlarını belirtir; değerleri eklemez.
- Başarılı başlangıç logu yalnız doğrulanan environment adını yazar.
- Options sınıfları secret taşısa da record değildir ve otomatik değer dökümü yapılmaz.
- `dotnet user-secrets list` değerleri düz metin gösterdiği için doğrulama komutu olarak kullanılmaz veya CI loguna alınmaz.
- Testlerde yalnız `DEMO-SENSITIVE-CANARY-*` değerleri kullanılır ve yakalanan loglarda bulunmadıkları doğrulanır.

## Manuel negatif kontrol

Mevcut Development secret'larını silmek gerçek bir yerel state değişikliğidir. Bu nedenle normal test scripti secret deposunu temizlemez. İzole bir terminalde kritik anahtarı geçici olarak kaldırıp Host'u başlattığında uygulama `ConnectionStrings:HospitalDatabase` anahtarını belirten güvenli hata ile durmalıdır; kontrolden sonra yukarıdaki configure scriptini yeniden çalıştır.

Resmî kaynaklar: [ASP.NET Core yapılandırma önceliği](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0), [Options başlangıç doğrulaması](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0), [Development user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).
