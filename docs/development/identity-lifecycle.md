# Kimlik yaşam döngüsü

Bu belge F02-G02 kapsamında kurulan ASP.NET Core Identity tabanını açıklar. Sistem yalnız sentetik `DEMO` hesaplarla çalışır; sertifikalı bir HBYS değildir. Rol/izin politikaları F02-G03, kaynak kapsamı F02-G04, personel MFA F02-G05 ve audit F02-G06 kapsamındadır.

## Sınır ve sahiplik

`IdentityAccess` hesabı, parola hash'ini, oturumu ve kısa ömürlü işlem kodlarını yönetir. Hesap, kişi ve iş profili aynı kayıt değildir:

- `ApplicationUser.PersonId` başka bir modülün tablosuna foreign key kurmadan kanonik kişi kimliğine referans verir.
- Hasta hesabı self-registration sırasında yalnız hesap ve yeni bir `PersonId` oluşturur; demografi/hasta ana kaydı F03-G01'de `Patients` tarafından oluşturulur.
- Personel hesabı yalnız yönetici davet komutuyla, önceden bilinen `PersonId` ile oluşturulur; meslek, bölüm ve atama `Organization` sahibindedir.
- `IdentityAccessDbContext` yalnız `identity_access` şemasına erişir. Başka modülün DbContext veya tablosunu kullanmaz.

ASP.NET Core Identity tabloları ile uygulamaya özel `action_codes` tablosu aynı modül şemasındadır. Rol tabloları Identity'nin yapısal temelidir; bu görev rol seed'i veya yetki atama API'si eklemez.

## Desteklenen akışlar

### Hasta hesabı

1. Hasta `/account/register` üzerinden `.invalid` uzantılı sentetik e-posta ve güçlü parola gönderir.
2. Sunucu hesap varlığından bağımsız genel `202 Accepted` mesajı döndürür.
3. Yeni veya henüz doğrulanmamış hesap için Mailpit'e `MOCK / DEMO` doğrulama kodu gönderilir. Yeni kod istendiğinde önceki etkin kod iptal edilir.
4. Kod `/account/verify-email` formuna yazılır; URL veya query string içinde taşınmaz.
5. Doğrulanmamış, kilitli veya devre dışı hesap giriş yapamaz.
6. Giriş başarılıysa sunucu güvenli oturum cookie'si üretir; tarayıcı storage alanına auth token yazılmaz.

### Parola sıfırlama

1. Bilinen ve bilinmeyen hesaplar aynı `202` durumunu ve aynı genel gövdeyi alır.
2. Uygun hesap için kısa ömürlü, tek kullanımlık kod Mailpit'e gönderilir.
3. Başarılı sıfırlama kodu tüketir, kilidi ve başarısız deneme sayısını temizler, security stamp'i değiştirir ve sıfırlama isteğini yapan tarayıcıyı aynı yanıtta çıkışa zorlar.
4. Eski parola ve tüketilmiş kod tekrar kullanılamaz; yeni parola ile yeniden giriş gerekir.

### Personel daveti

Personel self-registration endpoint'i yoktur. `IIdentityLifecycleService.InviteStaffAsync` yalnız gelecekte permission kontrolü yapan yönetim use-case'i tarafından çağrılacak application sınırıdır. Davet, daveti veren kullanıcı ve mevcut personel `PersonId` değerini ister. Personel `/account/accept-staff-invitation` formunda tek kullanımlık kodla parolasını oluşturur ve hesabını etkinleştirir.

F02-G03/G08 tamamlanana kadar personel daveti oluşturan public API veya yönetim ekranı bilinçli olarak yoktur.

## HTTP sözleşmesi

Tüm yazma endpoint'leri `X-HMS-CSRF` header'ı ve antiforgery cookie'si ister. Hassas grup ayrı rate-limit politikasına bağlıdır.

| Metot ve yol | Anonymous | Başarı | Not |
|---|---:|---:|---|
| `GET /api/v1/identity/antiforgery` | Evet | `200` | Request token döndürür, güvenli cookie üretir |
| `POST /api/v1/identity/patient-registrations` | Evet | `202` | Genel cevap; hesap varlığını açıklamaz |
| `POST /api/v1/identity/patient-email-confirmations` | Evet | `200` | Geçersiz/süresi dolmuş/kullanılmış kod `400` |
| `POST /api/v1/identity/sessions` | Evet | `200` | Hatalı veya kullanılamaz hesap için genel `401` |
| `GET /api/v1/identity/session` | Hayır | `200` | Geçersiz oturum `401`; yalnız minimum hesap özeti |
| `POST /api/v1/identity/sessions/logout` | Hayır | `204` | Cookie'yi sonlandırır |
| `POST /api/v1/identity/password-reset-requests` | Evet | `202` | Bilinen/bilinmeyen hesap için aynı sözleşme |
| `POST /api/v1/identity/password-resets` | Evet | `200` | Tek kullanımlık kod ve yeni parola ister |
| `POST /api/v1/identity/staff-invitation-acceptances` | Evet | `200` | Yalnız önceden davet edilmiş personel |

Parola ve işlem kodu DTO'ları `record` değildir; debugger/log tarafından otomatik `ToString()` ile içeriğe dönüştürülmez. API yanıtları parola, ham kod, hash, security stamp veya kilitleme ayrıntısı taşımaz.

## Güvenlik kontrolleri

- Oturum cookie'si `__Host-HospitalManagement.Auth`, `HttpOnly`, `Secure=Always`, `SameSite=Strict`, `Path=/`, session-only ve sliding expiration kapalıdır.
- Antiforgery cookie'si `__Host-HospitalManagement.Antiforgery`, `HttpOnly`, `Secure=Always`, `SameSite=Strict` ayarındadır.
- HTTPS zorunludur. HTTP üzerinde antiforgery token üretimi tasarım gereği reddedilir.
- Parola en az 12 karakter; büyük/küçük harf, rakam, sembol ve en az 6 benzersiz karakter ister.
- Varsayılan kilit 5 başarısız deneme ve 15 dakikadır; değerler doğrulanan ayarlardan gelir.
- Bilinmeyen kullanıcı girişinde de dummy parola hash'i doğrulanarak bariz hızlı dönüş azaltılır.
- Ham işlem kodu 24 kriptografik rastgele byte'tan üretilir, yalnız teslim mesajında bulunur. Veritabanına sadece SHA-256 hex hash, amaç, süre ve tüketim/iptal metadata'sı yazılır.
- Aynı kullanıcı ve amaç için yalnız bir etkin kod partial unique index ile korunur. Kod tüketimi optimistic concurrency sürümüyle tekrar kullanıma karşı korunur.
- Kod, parola, recipient adresi ve klinik içerik log/telemetry/exception/URL içine yazılmaz. MOCK SMTP teslim hatası yalnız içeriksiz sabit olay üretir.
- Yalnız `.invalid` e-posta alan adı kabul edilir; gerçek kişiye veya kuruma mesaj gönderilemez.

## Yapılandırma

`HospitalManagement:Identity` bölümü:

| Anahtar | Varsayılan | Doğrulanan aralık |
|---|---:|---:|
| `SessionMinutes` | 60 | 5–480 |
| `ActionCodeMinutes` | 30 | 5–60 |
| `LockoutMinutes` | 15 | 5–1440 |
| `MaxFailedAccessAttempts` | 5 | 3–10 |
| `SensitivePermitLimit` | 10 | 5–100 |

Production-benzeri ortamda TLS sonlandırma/proxy ayarı ayrıca dağıtım güvenlik incelemesine tabidir. Cookie güvenlik seçenekleri environment'a göre gevşetilmez.

## Yerelde çalıştırma ve elle doğrulama

Önce geliştirme HTTPS sertifikasını ve yerel bağımlılıkları hazırlayın:

```powershell
dotnet dev-certs https --trust
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

Tarayıcıda `https://localhost:7111/account/register`, Mailpit için `http://127.0.0.1:8025` açın.

1. `DEMO-manual-patient@hospital.invalid` ve politika uyumlu sentetik parola ile kayıt olun. Genel kabul mesajını ve parola alanının temizlendiğini doğrulayın.
2. Mailpit'teki MOCK kodu URL'ye eklemeden doğrulama formuna girin. Aynı kodun ikinci kullanımının reddedildiğini kontrol edin.
3. Giriş yapıp `/account` sayfasında e-posta ve `Patient` türünü görün. DevTools → Application altında auth/token değerinin Local/Session Storage'a yazılmadığını; cookie'nin `Secure`, `HttpOnly`, `SameSite=Strict` olduğunu kontrol edin.
4. Çıkıştan sonra `/account` sayfasının oturumsuz durumu gösterdiğini doğrulayın.
5. Bilinen ve bilinmeyen iki `.invalid` adres için parola sıfırlama isteyin; UI mesajının aynı olduğunu kontrol edin. Bilinen hesap koduyla parolayı değiştirin; kodun tekrarı ve eski parola reddedilmeli, yeni parola çalışmalıdır.
6. Aynı hesaba art arda beş hatalı parola gönderin; doğru parola kilit süresinde reddedilmeli. Parola sıfırlama sonrası yeni parola ile giriş tekrar mümkün olmalıdır.
7. `http://localhost:5111` üzerinden form işlemi denemeyin; güvenli cookie/antiforgery için HTTPS kullanın.

## Otomatik doğrulama

```powershell
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj `
  -c Release --filter "Roadmap=F02-G02"

dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
  -c Release --filter "Roadmap=F02-G02"

dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj `
  -c Release --filter "Roadmap=F02-G02"
```

Integration ve E2E testleri Docker üzerinden geçici PostgreSQL 18.6 kullanır. E2E Kestrel'i kısa ömürlü sentetik test sertifikasıyla HTTPS başlatır ve Chromium'da tam hasta yaşam döngüsünü yürütür.

## Migration ve retention

`InitialIdentityAccess` migration'ı ileri yönde uygulanır. Production-benzeri geri dönüş, kullanıcı/rol/kod tablolarını `Down` ile düşürmek yerine düzeltici migration ile yapılır. Yerelde boş DEMO veritabanı dışında veri kaybettiren geri alma kullanılmaz.

Hesap metadata'sı `RET-IDENTITY`, parola hash'i ve işlem kodu metadata'sı `RET-AUTH-SECRET` kapsamındadır. Süresi dolmuş/tüketilmiş/iptal edilmiş kod metadata'sının temizliği sonraki retention uygulama görevine aittir; süreler gerçek kullanım için `TBD-LEGAL`/güvenlik politikasıdır.
