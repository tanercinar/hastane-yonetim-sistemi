# Native Bildirim ve Deep-Link Sınırı (F12-G06)

## 1. Amaç ve Kapsam

Bu belge, Faz 12 kapsamında Windows ve Android platformlarında çalışan yerel istemciler için uygulama içi bildirim merkezini, korumalı sağlık verisi (PHI) sızıntısını engelleyen bildirim temizleme kurallarını ve güvenli deep-link (`hospitalapp://...`) yönlendirme mimarisini belgeler.

## 2. Sağlık Mahremiyeti ve Kilit Ekranı PHI Filtresi

Kişisel Sağlık Verileri mevzuatı ve HIPAA/GDPR standartları uyarınca, cihaz kilit ekranında veya işletim sistemi bildirim banner'larında hastaya ait korunan sağlık bilgileri (teşhis/tanı, ilaç adları, dozajlar, kritik laboratuvar sayısal değerleri) açıkça gösterilemez.

### 2.1. Güvenli Bildirim Modeli ve Biçimlendirici

- **Model**: `SafeNotificationMessage` (`src/HospitalManagement.UI/Notifications/SafeNotificationModels.cs`)
  - `PublicLockScreenTitle`: İşletim sistemi ve kilit ekranında gösterilen genel başlık (Örn: "Randevu Güncellemesi", "Yeni E-Reçete Düzenlendi").
  - `PublicLockScreenPreview`: PHI içermeyen, tamamen genel bilgilendirme metni (Örn: "Adınıza yeni bir reçete kaydı oluşturuldu. İlaç detayları için uygulamayı açınız.").
  - `AuthenticatedDetail`: Yalnızca kullanıcı uygulamayı açıp kimlik doğrulamasını tamamladıktan sonra uygulama içinde görüntülenen klinik içerik.
  - `DeepLinkUrl`: İlgili kayda doğrudan yönlendiren güvenli URL şeması.
- **Biçimlendirici**: `SafeNativeNotificationFormatter` (`src/HospitalManagement.UI/Notifications/SafeNativeNotificationFormatter.cs`)
  - Tıbbi anahtar kelimeleri (kanser, diyabet, hipertansiyon, hiv, biyopsi, ilaç isimleri, dozlar) ve sayısal klinik birimleri (`mg`, `g/dL`, `mmol`, `mmHg`) denetler.
  - Kilit ekranı önizlemesine hiçbir hassas klinik terimin sızmamasını garanti eder.

## 3. Güvenli Deep-Link Yönlendirmesi (NativeDeepLinkRouter)

- **Dosya**: `src/HospitalManagement.UI/Notifications/NativeDeepLinkRouter.cs`
- **Özel Şema**: `hospitalapp://`
- **İzin Verilen Hedefler (Allow-list)**:
  - `hospitalapp://appointments?id={id}` → `/patient/appointments/{id}`
  - `hospitalapp://prescriptions?id={id}` → `/patient/prescriptions/{id}`
  - `hospitalapp://results?id={id}` → `/patient/diagnostic-results/{id}`
  - `hospitalapp://staff-workspace` → `/staff/workspace` (Yalnızca personel rolleri için)
  - `hospitalapp://home` → `/`

### 3.1. Güvenlik ve Doğrulama Kontrolleri

1. **Şema Kısıtlaması**: `javascript:`, `data:`, `file:`, `http:` ve `https:` gibi harici şemalar reddedilir; açık yönlendirme (open redirect) ve XSS engellenir.
2. **Dizin Geçişi (Path Traversal)**: `..`, `\` veya `//` içeren manipüle edilmiş yollar engellenir.
3. **Parametre Doğrulaması**: Kaynak kimlikleri (`id`) katı alfasayısal desene (`SafeResourceIdRegex`) göre doğrulanır; SQL veya script enjeksiyonları reddedilir.
4. **Rol ve Yetki Sınırı**: Hasta rolündeki bir kullanıcı `hospitalapp://staff-workspace` linkine tıkladığında yönlendirme reddedilir (`IsAuthorized = false`) ve güvenli `/forbidden` rotasına aktarılır.
5. **Dayanıklılık**: Geçersiz veya bozuk linkler uygulamanın çökmesine yol açmaz; anlaşılır bir hata ile ana sayfaya yönlendirir.

## 4. Uygulama İçi Bildirim Merkezi (InAppNotificationCenter)

- **Bileşen**: `src/HospitalManagement.UI/Components/Notifications/InAppNotificationCenter.razor`
- **Özellikler**:
  - Okunmamış bildirim rozeti ve sayaç.
  - Kategoriye göre etiketleme (Randevu, E-Reçete, Tahlil/Tanı, Acil Görev, Genel).
  - Tıklama ile ilgili kayda deep-link üzerinden gitme ve otomatik okundu işaretleme.
  - "Tümünü Okundu İşaretle" aksiyonu.
  - WCAG erişilebilir semantik etiketler (`role="region"`, `aria-label`, `<time>`).
  - Windows High Contrast teması ile tam uyum.

## 5. Doğrulama ve Testler

- **Bileşen ve Güvenlik Testleri**: `tests/HospitalManagement.ComponentTests/Security/NativeDeepLinkAndNotificationSecurityTests.cs`
  - `SafeNativeNotificationFormatterStripsProtectedHealthInformationFromLockScreenPreview`: Tanı ve ilaçların kilit ekranı önizlemesinden temizlendiğini test eder.
  - `SafeNativeNotificationFormatterStripsLaboratoryResultsFromPreview`: Sayısal tahlil değerlerinin kilit ekranına sızmadığını test eder.
  - `NativeDeepLinkRouterRejectsNonAllowedSchemes`: Yetkisiz URI şemalarını test eder.
  - `NativeDeepLinkRouterRejectsPathTraversalAttempts`: Dizin geçişi saldırılarının reddedildiğini test eder.
  - `NativeDeepLinkRouterRejectsMaliciousQueryParameters`: Zararlı parametrelerin reddedildiğini test eder.
  - `NativeDeepLinkRouterRejectsPatientAccessToStaffWorkspace`: Hasta rolünün personel alanına erişiminin engellendiğini test eder.
  - `NativeDeepLinkRouterAllowsDoctorAccessToStaffWorkspace`: Hekim rolünün personel alanına güvenle yönlendirildiğini test eder.
  - `NativeDeepLinkRouterParsesValidPatientRoutesSuccessfully`: Geçerli hasta rotalarının doğru çözümlendiğini test eder.
- **UI Testleri**: `tests/HospitalManagement.ComponentTests/Components/InAppNotificationCenterComponentTests.cs`
  - `InAppNotificationCenterRendersEmptyStateWhenNoNotifications`
  - `InAppNotificationCenterRendersNotificationsAndUnreadBadge`
- Tüm testler %100 oranında başarılıdır (142/142 test).
