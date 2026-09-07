# Native Kimlik ve Güvenlik Tasarımı (Windows & Android)

Bu belge, **Faz 12 (F12-G01)** kapsamında Windows ve Android native istemcileri için belirlenen OAuth 2.0, OpenID Connect (OIDC) ve PKCE kimlik güvenliği mimarisini ve teknik uygulama detaylarını açıklar. İlgili mimari karar için [ADR-0004](../adr/ADR-0004-identity-and-native-oidc.md) belgesine başvurunuz.

---

## 1. Mimari Prensipler ve Standartlar

1. **RFC 6749 & RFC 6750:** OAuth 2.0 Yetkilendirme Çerçevesi ve Bearer Token Kullanımı.
2. **RFC 7636:** Proof Key for Code Exchange (PKCE) - Public Client saldırılarına karşı yetkilendirme kodu koruması.
3. **RFC 8252:** OAuth 2.0 for Native Apps (BCP 212) - Sistem tarayıcısı zorunluluğu, gömülü webview yasağı, loopback ve özel şema kuralları.
4. **RFC 7009:** OAuth 2.0 Token Revocation - Güvenli oturum kapatma ve token iptali.
5. **Homemade JWT Yasağı:** Projede standart dışı, kendi kendine token üreten veya doğrulayan kriptografik zayıf kodlar yazılmaz; yerleşik ve standart .NET güvenlik kütüphaneleri kullanılır.
6. **Web Cookie Akışı İzolasyonu:** Web istemcisinin (Blazor WASM) kullandığı `__Host-HospitalManagement.Auth` SameSite=Strict cookie ve antiforgery koruması korunur; native istemciler web oturumunu bozmaz veya taklit etmez.

---

## 2. Yetkilendirme Akışı (PKCE + Sistem Tarayıcısı)

Native istemciler istemci gizli anahtarı (`client_secret`) saklayamayacağından (Public Client), yetkilendirme kodu yakalama saldırılarına (Authorization Code Interception Attack) karşı PKCE S256 zorunludur.

```mermaid
sequenceDiagram
    autonumber
    actor U as Kullanıcı (Personel / Hasta)
    participant N as Native İstemci (Windows / Android)
    participant B as Sistem Tarayıcısı (Default Browser / Custom Tabs)
    participant S as API Sunucusu (OAuth / OIDC Endpoint)
    participant DB as Kimlik Veritabanı & Token Store

    Note over N: 1. code_verifier üret (CSPRNG, 64 karakter)<br/>2. code_challenge = Base64Url(SHA256(verifier))<br/>3. Kriptografik rastgele state ve nonce üret
    N->>B: Sistem tarayıcısını aç (/authorize?response_type=code&client_id=...&code_challenge=...&code_challenge_method=S256&redirect_uri=...&state=...)
    U->>B: Kimlik bilgilerini girer (Kullanıcı adı, şifre, gerekirse MFA)
    B->>S: Kimlik doğrulama isteği (POST /sessions veya OIDC login)
    S-->>B: 302 Redirect (redirect_uri?code=AUTH_CODE&state=STATE)
    B->>N: Callback URI tetiklenir (Windows Loopback / Android Custom Scheme)
    Note over N: State doğrulaması yap (CSRF engeli)
    N->>S: POST /token (grant_type=authorization_code, code=AUTH_CODE, redirect_uri=..., code_verifier=VERIFIER, client_id=...)
    Note over S: 1. code_verifier'dan challenge hesapla<br/>2. İstekteki challenge ile eşleştiğini doğrula (S256)<br/>3. Tek kullanımlık code'u yak ve geçersiz kıl
    S->>DB: Yeni token ailesi kaydet (Access + Refresh Token)
    S-->>N: 200 OK (access_token, refresh_token, expires_in=900, token_type=Bearer)
    Note over N: Tokenları OS Secure Storage'a yaz (DPAPI / Android Keystore)
```

---

## 3. Callback URI Kuralları ve Doğrulama (`NativeCallbackUriValidator`)

RFC 8252 gereği yetkilendirme kodunun güvenle native uygulamaya dönebilmesi için aşağıdaki kurallar zorunludur:

| Platform | Desteklenen Callback Deseni | Örnek URI | Güvenlik Koruması |
| :--- | :--- | :--- | :--- |
| **Windows Desktop** | Loopback IP (Tercih edilen) | `http://127.0.0.1:{port}/callback` veya `http://localhost:{port}/callback` | Rastgele boş port dinleme, loopback IP dışı dinleyiciye izin vermeme, host header doğrulama. |
| **Windows Desktop** | Özel URI Şeması (Fallback) | `hospitalmanagement://auth-callback` | Windows Protocol Handler kaydı. |
| **Android Mobil** | Özel URI Şeması | `hospitalmanagement://auth-callback` | Android Intent Filter ve package doğrulaması. |
| **Android Mobil** | Android App Links | `https://hospital.example.com/auth-callback` | Dijital varlık bağlantıları (`assetlinks.json`) ile domain sahiplik doğrulaması. |

### Açık Yönlendirme (Open Redirect) Engelleme
- Geçersiz host veya yabancı IP içeren yönlendirmeler atomik olarak reddedilir.
- Göreli şemalar (`//attacker.com`), `javascript:`, `data:` ve kullanıcı kimlik bilgisi (`user:pass@`) içeren URI'lar `NativeCallbackUriValidator` tarafından engellenir.

---

## 4. Token Yaşam Döngüsü ve Refresh Token Rotasyonu (RTR)

```mermaid
stateDiagram-v2
    [*] --> Active: Token Alındı (AT 15dk, RT 8saat)
    Active --> Active: Token Yenileme (RTR: Eski RT İptal -> Yeni RT + Yeni AT Verildi)
    Active --> Expired: RT Süresi Doldu
    Active --> Revoked: Kullanıcı Çıkışı (Logout)
    Active --> Compromised: RT Yeniden Kullanımı Tespit Edildi (Reuse Detected)
    Compromised --> Revoked: Tüm Token Ailesi Derhal İptal Edildi
    Expired --> [*]
    Revoked --> [*]
```

1. **Kısa Ömürlü Access Token:** Access token'lar 15 dakika geçerlidir. Hırsızlık durumunda etki alanı daraltılır.
2. **Refresh Token Rotasyonu:** Her `POST /token?grant_type=refresh_token` çağrısında mevcut refresh token derhal geçersiz kılınır ve yeni bir refresh token üretilir.
3. **Yeniden Kullanım Tespiti (Reuse Detection):** Eğer bir refresh token ikinci kez kullanılmaya çalışılırsa (saldırganın ve meşru kullanıcının aynı token'ı kullanması senaryosu), sunucu bunu bir sızıntı göstergesi olarak kabul eder. İlgili kullanıcı oturumuna ait **tüm token ailesi (token family) iptal edilir** ve meşru kullanıcı derhal yeniden giriş yapmaya yönlendirilir.
4. **Token Revocation (RFC 7009):** `POST /api/v1/identity/native/logout` veya token revocation uç noktası çağrıldığında sunucu tarafında aktif oturum sonlandırılır.

---

## 5. Platform Güvenli Depolama (Secure Storage) Karşılaştırması

| Özellik | Windows Masaüstü | Android Mobil |
| :--- | :--- | :--- |
| **Mekanizma** | Windows Data Protection API (DPAPI) / Windows Credential Manager | Android Keystore + `EncryptedSharedPreferences` / MAUI `ISecureStorage` |
| **Şifreleme Türü** | Kullanıcı oturum anahtarına bağlı simetrik şifreleme (`DataProtectionScope.CurrentUser`) | Donanım destekli (TEE/StrongBox) AES-256 GCM anahtarı |
| **Düz Metin Riski** | Yok (Registry veya dosyaya asla düz metin yazılmaz) | Yok (Uygulama sandbox'ında şifrelenmiş saklanır) |
| **Cihaz Yedekleme Riski** | DPAPI anahtarı başka makineye aktarılamaz | `android:allowBackup="false"` ve Keystore anahtarları yedeklenemez |
| **Çevrimdışı Klinik Veri** | **Kesinlikle saklanmaz** (yalnızca auth token tutulur) | **Kesinlikle saklanmaz** (yalnızca auth token tutulur) |

---

## 6. Tehdit Modeli ve Kontrol Eşlemesi

| Tehdit ID | Tehdit Tanımı | Kontrol Mekanizması | Doğrulama Yöntemi |
| :--- | :--- | :--- | :--- |
| **TM-02** | Session fixation / Çalıntı token | Kısa ömürlü token (15 dk), Refresh Token Rotation, güvenli storage | Oturum rotasyon ve iptal testleri |
| **TM-22** | Cihazda güvensiz token saklama | Windows DPAPI, Android Keystore, klinik veri offline cache yasağı | Unit testler ve platform smoke kontrolleri |
| **TM-26** | Yetkilendirme kodu yakalama (Code Interception) | PKCE S256 zorunluluğu (`code_challenge_method=S256`), loopback IP | PKCE doğrulama ve sahte verifier testleri |
| **TM-27** | Çalınan refresh token istismarı | RTR ve Reuse Detection ile tüm aileyi iptal etme | Mükerrer refresh token deneme testleri |
| **TM-28** | Open Redirect / Callback hijacking | `NativeCallbackUriValidator` allow-list ve şema denetimi | Open redirect ve bozuk URI negatif testleri |

---

## 7. Web Cookie ve Native İstemci Bir Arada Yaşama Stratejisi

ASP.NET Core Host API, iki farklı kimlik doğrulama şemasını uyum içinde destekler:
1. **Web İstekleri:** `CookieAuthenticationDefaults.AuthenticationScheme` (`__Host-HospitalManagement.Auth`), Antiforgery Header (`X-XSRF-TOKEN`).
2. **Native İstekleri:** `JwtBearerDefaults.AuthenticationScheme` veya Bearer token middleware (`Authorization: Bearer <access_token>`).
3. **Prensip:** Web istekleri asla native token sızdırmaz; native istemciler asla cookie gerektirmez. API yetkilendirme politikaları (`Permission`, `ResourceScope`) istemci türünden bağımsız olarak aynı claims matrisini çalıştırır.
