# Personel MFA ve Hassas Oturum Yönetimi (F02-G05)

Bu belge, Hastane Yönetim Sistemi projesinde uygulanan TOTP tabanlı çok faktörlü kimlik doğrulama (`MFA`), kurtarma kodları (`Recovery Codes`) ve hassas işlemler için yakın zamanda doğrulanmış oturum (`Recent Authentication / Sensitive Session`) kurallarını açıklar.

## 1. Genel Mimari ve Karar Modeli

Yetkilendirme ve kimlik mimarisi [`docs/adr/ADR-0004-identity-and-native-oidc.md`](../adr/ADR-0004-identity-and-native-oidc.md) ve [`docs/security/authorization-matrix.md`](../security/authorization-matrix.md) belgelerine dayanır:

- **Personel İçin TOTP MFA:** Klinik ve idari personel hesaplarında TOTP tabanlı standart MFA desteği sağlanır.
- **Güvenli Secret Yönetimi:** MFA paylaşımlı anahtarı (`sharedKey`) yalnızca kurulum aşamasında authenticated kullanıcıya gösterilir; düz metin loglara, hata mesajlarına veya yetkisiz yanıtlara asla yazılmaz.
- **Tek Kullanımlık Kurtarma Kodları (Single-Use Recovery Codes):** MFA etkinleştirildiğinde üretilen 8 adet kurtarma kodu tek kullanımlıktır; bir kez kullanıldığında veritabanından güvenli biçimde silinir (`RedeemTwoFactorRecoveryCodeAsync`).
- **Hassas Oturum / Yakın Doğrulama (Step-Up / Recent Authentication):** Kritik yönetimsel işlemler veya hassas kaynak erişimleri için oturumun son 5 dakika içinde doğrulanmış olması (`auth_time` claim'i) zorunlu tutulur.

## 2. İki Faktörlü Giriş Akışı

```text
[İstemci] --(POST /sessions {email, password})--> [Sunucu]
                                                    │
                                           MFA Etkin mi?
                                           ├── Hayır ──> 200 OK (__Host-HospitalManagement.Auth cookie)
                                           └── Evet ───> 200 OK { requiresTwoFactor: true }
                                                         (__Host-HospitalManagement.2FA geçici cookie)
                                                    │
[İstemci] --(POST /two-factor-sessions {code})---> [Sunucu]
                                                    │
                                           TOTP / Kurtarma Kodu Geçerli mi?
                                           ├── Evet ───> 200 OK (__Host-HospitalManagement.Auth cookie)
                                           │             (Geçici 2FA cookie temizlenir)
                                           └── Hayır ──> 401 Unauthorized
```

## 3. Uç Noktalar

| Metot | Yol | İzin / Koşul | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/identity/two-factor-sessions` | `__Host-HospitalManagement.2FA` cookie | 2FA TOTP veya kurtarma kodu ile giriş doğrulama |
| `GET` | `/api/v1/identity/mfa/setup` | `Identity.ProfileEditOwn` | TOTP shared key ve `otpauth://` URI üretimi |
| `POST` | `/api/v1/identity/mfa/enable` | `Identity.ProfileEditOwn` | İlk TOTP kodunu doğrulayarak MFA'yı açma ve kurtarma kodlarını alma |
| `POST` | `/api/v1/identity/mfa/disable` | `Identity.ProfileEditOwn` | Parola teyidi ile MFA'yı devre dışı bırakma |
| `POST` | `/api/v1/identity/mfa/recovery-codes` | `Identity.ProfileEditOwn` | Yeni kurtarma kodları üretme (eski kodları geçersiz kılar) |

## 4. Güvenlik Korumaları

1. **İki Aşamalı Oturum Sınırı:** Parola doğru girilse bile `TwoFactorEnabled == true` olan hesaplar tam oturum çerezi (`__Host-HospitalManagement.Auth`) alamaz; yalnızca geçici `__Host-HospitalManagement.2FA` çerezi oluşturulur.
2. **Kurtarma Kodlarının Tek Kullanımlılığı:** Kullanılan kurtarma kodu tekrar gönderildiğinde sistem `401 Unauthorized` ile reddeder.
3. **Step-up / Yakın Doğrulama:** `RecentAuthenticationRequirement` ve `RecentAuthenticationAuthorizationHandler` kullanıcının `auth_time` claim'ini inceleyerek oturumun geçerlilik süresini denetler.
