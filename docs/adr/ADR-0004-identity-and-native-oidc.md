# ADR-0004: Web Kimliği ve Native OIDC Geçişi

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **İlgili:** `docs/security/authorization-matrix.md`, `docs/security/threat-model.md`

## Bağlam

İlk web ürünü birinci taraf aynı-origin uygulamadır; sonra Windows ve Android istemcileri eklenecektir. Parola/token akışını elde yazmak yüksek risklidir. Web'de browser storage'da bearer token tutmak XSS etkisini artırır; native istemcide cookie/webview tabanlı giriş ise modern güvenlik beklentilerine uymaz.

## Karar

### Ortak kimlik alanı

- ASP.NET Core Identity kullanıcı/parola, doğrulama, lockout, recovery ve personel MFA yaşam döngüsü için kullanılır.
- Yetkilendirme role ek olarak permission ve resource-based handler ile yapılır.
- Hasta self-registration; personel sistem yöneticisi daveti/aktivasyonu ile oluşturulur.
- Hesap/Person/Patient/StaffProfile ayrı kavramlardır.
- Personel için TOTP MFA zorunlu; yüksek risk işlemler step-up/yakın doğrulama ister.

### Web

- Aynı-origin güvenli `HttpOnly`, `Secure`, uygun `SameSite` cookie kullanılır.
- State-changing istekler antiforgery/CSRF koruması gerektirir.
- Oturum girişte/ayrıcalık değişiminde döndürülür; logout ve hesap kapatma sunucu oturumunu iptal eder.
- Token `localStorage` veya `sessionStorage` içine konmaz.

### Windows ve Android (Faz 12 Kesinleşen Tasarım)

- **Yetkilendirme Protokolü:** Standart OAuth 2.0 / OpenID Connect Authorization Code akışı + PKCE (Proof Key for Code Exchange, RFC 7636).
- **Public Client:** Native istemciler gizli anahtar saklayamaz (`token_endpoint_auth_method: none`); gömülü client_secret kullanılmaz (TM-26 koruması).
- **PKCE S256:** Yalnızca `code_challenge_method=S256` kabul edilir; güvensiz `plain` yöntemi reddedilir. `code_verifier` CSPRNG ile 64 karakter (RFC 7636 43-128 karakter kuralı) üretilir.
- **Sistem Tarayıcısı (RFC 8252):** Embedded webview içinde asla kullanıcı adı/parola toplanmaz.
  - Windows Desktop: Sistem tarayıcısı ve Loopback IP dinleyici (`http://127.0.0.1:{port}/callback` veya `http://localhost:{port}/callback`) ya da özel protokol (`hospitalmanagement://auth-callback`).
  - Android Mobil: Chrome Custom Tabs ve özel şema (`hospitalmanagement://auth-callback`) veya Android App Links (`https://hospital.example.com/auth-callback`).
- **Callback URI Doğrulaması:** Katı `NativeCallbackUriValidator` allow-list ile açık yönlendirme (Open Redirect, TM-28) ve şema sızıntısı engellenir.
- **Token Yaşam Döngüsü & RTR:**
  - Access token: Kısa ömürlü (15 dakika), scoped Bearer token.
  - Refresh token: Süreli (8 saat), her kullanımda yenilenir (**Refresh Token Rotation - RTR**).
  - **Reuse Detection:** Kullanılmış bir refresh token tekrar gönderilirse tüm token ailesi derhal sunucuda iptal edilir (TM-27).
- **Güvenli Depolama (Secure Storage, TM-22):**
  - Windows: Data Protection API (DPAPI - `ProtectedData`, `CurrentUser`).
  - Android: Android Keystore ve `EncryptedSharedPreferences` / MAUI `SecureStorage`.
  - Düz metin dosya, SQLite veya SharedPreferences'a token veya klinik veri yazılmaz.
- **Çevrimdışı Klinik Veri Yasağı:** Cihazda offline klinik kayıt cache edilmez; istemciler salt çevrimiçi API tüketicisidir.
- **Logout & Revocation (RFC 7009):** `POST /api/v1/identity/native/logout` ile sunucu tarafı oturum ve refresh token anında iptal edilir.
- **Homemade JWT Yasağı:** Projede standart dışı veya kriptografik zayıf token üreteci yazılmaz; standart yerleşik kütüphaneler kullanılır.
- **Web İzolasyonu:** Web Blazor WASM uygulamasının `__Host-HospitalManagement.Auth` SameSite=Strict cookie akışı %100 korunur; native Bearer token akışı web oturumunu etkilemez.
- **Ayrıntılı Tasarım:** `docs/development/native-identity-security-design.md` belgesinde belgelenmiştir.

## Alternatifler

### Tüm istemciler için bearer JWT ve localStorage

Reddedildi: web XSS etkisini artırır, rotation/revocation karmaşıklığı getirir.

### Tüm istemciler için cookie

Reddedildi: browser web için uygundur; native uygulamada sistem tarayıcısı/OIDC ve secure token storage daha doğru sınırdır.

### Kendi OAuth/OIDC sunucusunu sıfırdan yazmak

Reddedildi: güvenlik protokolü uygulamak ürün kapsamı değildir. Desteklenen standart bileşen/sağlayıcı seçimi Faz 12'de güncel durumla yapılır.

### Active Directory/Entra

Şimdilik reddedildi: gerçek kurum entegrasyonu yoktur. Gerçek deployment hedefinde yeniden değerlendirilebilir.

## Sonuçlar

### Olumlu

- Web'de daha küçük token saldırı yüzeyi.
- Native'de modern standart akış ve platform secure storage.
- Kimlik yaşam döngüsünde yerleşik .NET bileşenleri.

### Olumsuz

- Web ve native için iki oturum taşıma deseni vardır.
- Native OIDC ek bir sağlayıcı/konfigürasyon kararı gerektirir.
- Permission değişimlerinin aktif session/token'a yansıması ayrıca tasarlanmalıdır.

## Uygulama korumaları

1. Auth response ve loglarda token/MFA secret/recovery code yazılmaz.
2. Account enumeration önlenir; reset/invite linkleri tek kullanımlı ve kısa ömürlüdür.
3. Son aktif ADM'nin devre dışı bırakılması/rol kaybı engellenir.
4. Permission değişimi güvenlik stamp/session invalidation üretir.
5. Open redirect, reset poisoning, session fixation, MFA bypass ve deep-link testleri zorunludur.

## Yeniden değerlendirme koşulları

- Native geliştirme başlarken desteklenen OIDC ürünleri/sürümleri seçileceğinde
- Kurumsal SSO/Entra/Active Directory talebi doğarsa
- Çoklu hastane/tenant veya gerçek kullanıcı federasyonu eklenirse
- Yeni istemci türü veya offline erişim talebi gelirse
