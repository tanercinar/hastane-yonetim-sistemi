# Kimlik ve Gizli Bilgi Güvenliği (F13-G04)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin kimlik doğrulama, oturum yönetimi, parola sıfırlama, çok faktörlü doğrulama (MFA), belirteç iptali (token revocation) ve gizli anahtar (secret) güvenliğini belgeler.

## 2. Güvenlik Savunma Mekanizmaları

### 2.1. Kaba Kuvvet (Brute Force) ve Sözlük Saldırılarına Karşı Koruma
- **Hesap Kilitleme (Lockout)**: 5 ardışık başarısız oturum açma girişiminde hesap otomatik olarak 15 dakika kilitlenir (`MaxFailedAccessAttempts = 5`, `LockoutMinutes = 15`).
- **Hız Sınırlama (Rate Limiting)**: Hassas kimlik uç noktaları (`/api/v1/identity/login`, parola sıfırlama, davet) `identity-sensitive` politikası ile IP ve kullanıcı bazında dakikada en fazla 10 istekle sınırlandırılmıştır (`SensitivePermitLimit = 10`).
- **Zamanlama Analizi Koruması (Timing Attack Defense)**: Kullanıcı veri tabanında bulunmasa dahi, sahte kullanıcı modeli (`TimingUser`) üzerinden `TimingPasswordHash` doğrulaması yürütülür; böylece yanıt sürelerinden kullanıcı varlığı çıkarımı (user enumeration) engellenir.

### 2.2. Çok Faktörlü Kimlik Doğrulama (MFA) Güvenliği
- **TOTP Standart Uyumu**: RFC 6238 uyumlu zaman tabanlı tek kullanımlık şifreler (TOTP, `otpauth://totp/...`).
- **Doğrulama Zorunluluğu**: MFA etkinleştirilmeden önce geçerli bir TOTP kodu doğrulanması zorunludur; doğrulanmamış anahtarlar devreye alınmaz.
- **Tek Kullanımlık Kurtarma Kodları**: Kurtarma kodları tek kullanımlıktır (`RedeemTwoFactorRecoveryCodeAsync`) ve kullanıldığında tüketilir.
- **MFA Değişikliğinde Oturum İptali**: MFA devre dışı bırakıldığında veya anahtar sıfırlandığında `UpdateSecurityStampAsync` tetiklenerek mevcut tüm aktif oturumlar geçersiz kılınır.

### 2.3. Parola Sıfırlama ve Host Poisoning Koruması
- **Kriptografik Token Üretimi**: `RandomNumberGenerator.GetBytes(24)` ile 24 baytlık kriptografik rastgele URL-safe belirteç üretilir.
- **Veri Tabanında Yalnızca Hash Saklama**: Veri tabanında (`identity_action_codes` tablosu) açık token tutulmaz; yalnızca SHA-256 özeti saklanır (`CodeHash`). Veri tabanı ele geçirilse dahi sıfırlama kodları elde edilemez.
- **Tek Kullanımlık ve Süreli Kodlar**: Kodlar 30 dakika geçerlidir (`ActionCodeMinutes = 30`); kullanıldığında anında tüketilir (`Consume`) ve mükerrer kullanım engellenir.
- **Eski Kodların Otomatik İptali**: Yeni bir sıfırlama kodu talep edildiğinde kullanıcının önceki tüm geçerli kodları anında iptal edilir (`Revoke`).
- **Host Header Poisoning Engeli**: E-posta içeriklerinde istemcinin gönderdiği kontrolsüz `Host` başlığına güvenilmez; yapılandırılmış güvenli temel URL kullanılır.

### 2.4. Oturum Sabitleme (Session Fixation) ve Belirteç İptali
- **Güvenlik Damgası (SecurityStamp)**: Parola değiştirildiğinde, sıfırlandığında veya hesap durumu güncellendiğinde güvenlik damgası yenilenir. ASP.NET Core cookie doğrulayıcısı değişen damgayı algılayarak eski oturumları anında sonlandırır.
- **Native OIDC Token İptali**: RFC 7009 uyumlu `/api/v1/identity/revoke` uç noktası; Refresh Token Rotation (RTR) ile eski token kullanıldığında tüm token ailesi iptal edilir (`reuse detection` / TM-27).

### 2.5. Gizli Bilgi (Secret) ve Depo Hijyeni
- **Sentetik Veri İzolasyonu**: Sistemdeki tüm demo e-posta adresleri RFC 2606 uyarınca ayrılmış `.invalid` TLD'si ile sınırlandırılmıştır (`NormalizeDemoEmail`). Gerçek bir e-posta adresine yanlışlıkla bildirim gitmesi engellenir.
- **Gizli Dosya Taraması**: Depoda hiçbir gerçek JWT anahtarı, TLS özel anahtarı (`*.key`, `*.pfx`, `*.p12`) veya mobil imzalama keystore'u (`*.keystore`, `*.jks`) bulunmaz; `.gitignore` ve `tools/verify-platform-packaging.ps1` ile taranır.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Security/IdentityAndSecretSecurityTests.cs`:
  - `IdentityOptionsEnforceBruteForceDefenses`
  - `ActionCodeOnlyStoresSha256HashAndEnforcesExpiration`
  - `ActionCodeRevocationPreventsSubsequentUsage`
  - `EmailDomainStrictlyRestrictedToReservedInvalidTld`
  - `PasswordHashingIsNonReversible`
- Toplam 467 birim testi ve 13 mimari testi hatasız geçmiştir.
