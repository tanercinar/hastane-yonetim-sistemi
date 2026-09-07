# Web Uygulama Güvenliği ve OWASP ASVS 5.0 L2 Değerlendirmesi (F13-G03)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin web uygulama güvenliği katmanını OWASP ASVS (Application Security Verification Standard) 5.0 Seviye 2 (L2) gereksinimlerine göre değerlendirir, uygulanan sertleştirme mekanizmalarını ve otomatik güvenlik testlerini belgeler.

## 2. OWASP ASVS 5.0 L2 Güvenlik Değerlendirmesi ve Kontrol Matrisi

| ASVS Bölümü | Güvenlik Alanı | Proje Uygulaması ve Doğrulama | Durum |
|---|---|---|:---:|
| **V1. Mimari** | Güven Sınırları & Kapsam | Modüler monolit mimari; her modülün kendi DbContext'i; doğrudan tablo erişimi yasak; `IResourceScoped` ve `ICareRelationshipEvaluator` ile çok katmanlı yetkilendirme. | **UYUMLU** |
| **V2. Kimlik Doğrulama** | Parola & Lockout | En az 12 karakter, 6 benzersiz karakter, büyük/küçük harf, rakam ve alfasayısal olmayan karakter zorunluluğu; 5 hatalı denemede 15 dakika lockout; IP ve kullanıcı bazlı rate limiting (`identity-sensitive`). | **UYUMLU** |
| **V3. Oturum Yönetimi** | Cookie Güvenliği | `__Host-HospitalManagement.Auth` çerezi; `HttpOnly = true`, `SameSite = Strict`, `Secure = Always`, `Path = "/"`; API çağrılarında 302 yerine 401/403 Problem Details yanıtı. | **UYUMLU** |
| **V4. Erişim Denetimi** | Yetkilendirme & IDOR | `Permission + Resource Scope + Care Relationship`; hasta-hasta ve sahipsiz hekim erişim engeli; dikey rol atlama engeli (`AuthorizationAttackSuiteTests`). | **UYUMLU** |
| **V5. Doğrulama & Sanitizasyon** | Path Traversal & Yükleme | `AttachmentSecurityValidator` ile `..`, `/`, `\` ve null byte temizliği; 15 MB dosya boyutu sınırı; PE/MZ çalıştırılabilir ikili dosya engeli; güvenli uzantı allow-list'i (.pdf, .jpg, .png, .webp, .dcm). SVG (XSS riski) ve script uzantıları reddedilir. | **UYUMLU** |
| **V7. Hata & Günlükleme** | Bilgi İfşası Engeli | RFC 9457 Problem Details; stack trace ve canary bilgisi istemciye iletilmez; Exception/URL/Telemetry içinde PHI maskeleme. | **UYUMLU** |
| **V8. Veri Koruma** | Korumalı Sağlık Verisi (PHI) | Kilit ekranı bildirimlerinde tanı/ilaç temizliği; CSV aktarımında formül enjeksiyonu (`=`, `+`, `-`, `@`) koruması; veri tabanında hassas sırların yalnız hash'lenmesi. | **UYUMLU** |
| **V12. İletişim** | TLS & HSTS | Production/Staging ortamında zorunlu HTTPS yönlendirmesi (`UseHttpsRedirection`) ve HTTP Strict Transport Security (`UseHsts`). | **UYUMLU** |
| **V13. Kötü Amaçlı Kod** | Zararlı Dosya Taraması | `IAttachmentMalwareScanner` soyutlaması ve MZ/PE binary imza denetimi; geçersiz MIME türü sahteciliği tespiti. | **UYUMLU** |
| **V14. Yapılandırma & Başlıklar** | Güvenlik Başlıkları | `SecurityHeadersMiddleware` ile tüm yanıtlara zorunlu HTTP güvenlik başlıklarının eklenmesi. | **UYUMLU** |

## 3. Uygulanan Güvenlik Başlıkları (Security Headers)

`SecurityHeadersMiddleware` (`src/HospitalManagement.Host/Api/SecurityHeadersMiddleware.cs`) ve `SecurityHeaderDefaults` (`src/HospitalManagement.BuildingBlocks/Security/SecurityHeaderDefaults.cs`) ile sisteme şu başlıklar kazandırılmıştır:

1. **`X-Content-Type-Options: nosniff`**: MIME-sniffing saldırılarını engeller; tarayıcının içerik türünü tahmin etmesini yasaklar.
2. **`X-Frame-Options: DENY`**: Clickjacking saldırılarına karşı sayfanın herhangi bir iframe içinde yüklenmesini engeller.
3. **`Referrer-Policy: strict-origin-when-cross-origin`**: Dış kaynaklara yapılan yönlendirmelerde hassas yol/parametre bilgilerinin sızmasını önler.
4. **`Permissions-Policy`**: Hassas cihaz API'lerini (kamera, mikrofon, coğrafi konum, ödeme, USB) devre dışı bırakır (`accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()`).
5. **`Content-Security-Policy (CSP)`**:
   ```http
   default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self' wss: ws:; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'
   ```
6. **`X-XSS-Protection: 0`**: Eski ve güvenlik açığı barındıran tarayıcı XSS filtrelerini devre dışı bırakıp modern CSP'yi zorunlu kılar.

## 4. Web Saldırı Vektörleri Karşılaştırması ve Savunma Mekanizmaları

- **XSS (Cross-Site Scripting)**: Blazor WebAssembly HTML otomatik kodlaması; CSP kuralı (`object-src 'none'`); SVG dosyalarının yüklenmesinin engellenmesi; zengin metin kabul edilmemesi.
- **CSRF (Cross-Site Request Forgery)**: Durum değiştiren isteklerde `X-HMS-CSRF` antiforgery belirteci doğrulaması (`IdentityAntiforgeryEndpointFilter`); kimlik doğrulama çerezinde `SameSite=Strict`.
- **SQL Injection**: 100% EF Core LINQ parametreli sorguları; kod tabanında sıfır `FromSqlRaw` veya dize birleştirme.
- **SSRF (Server-Side Request Forgery)**: Sunucu tarafında istemci tarafından belirtilen dış URL'lere giden hiçbir HTTP isteği yürütülmez; tüm dış entegrasyonlar mock portlar üzerinden yönetilir.
- **Path Traversal & Unsafe Upload**: `AttachmentSecurityValidator.SanitizeFileName` ile dizin geçişi karakterlerinin (`..`, `/`, `\`) temizlenmesi; dosya boyutu (15 MB) ve sihirli bayrak (magic byte) doğrulaması.
- **Open Redirect**: Sistem hiçbir zaman kullanıcı kontrollü `returnUrl` parametresi üzerinden kontrolsüz HTTP 302 yönlendirmesi yapmaz; tüm durumlar RESTful Problem Details ile döner.
- **CORS Zafiyetleri**: Varsayılan olarak yabancı kökenlere (cross-origin) açık CORS politikası tanımlanmamıştır; strictly same-origin geçerlidir.

## 5. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Security/WebApplicationSecurityTests.cs`:
  - `SecurityHeadersMeetOwaspAsvsMandatoryRequirements`
  - `PathTraversalSanitizerStripsDirectorySeparatorsAndPreservesBaseName`
  - `UnsafeUploadValidationRejectsExecutableFilesEvenWhenRenamedToPdf`
  - `UnsafeUploadValidationRejectsDangerousExtensions`
  - `UnsafeUploadValidationRejectsFilesExceedingFifteenMegabytes`
  - `CookieConfigurationEnforcesHostPrefixAndStrictFlags`
  - `AntiCsrfHeaderContractMatchesStandardTokenHeader`
- Tüm testler (456 birim, 13 mimari) başarıyla geçmiştir.
