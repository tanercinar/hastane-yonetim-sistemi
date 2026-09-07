# Erişilebilirlik ve Kullanılabilirlik Sertleştirmesi (F13-G08)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin Web ve .NET MAUI istemcilerinde WCAG 2.1 AA (Web Content Accessibility Guidelines) standartlarına tam uyumunu, klavye gezintisini, odak (focus) yönetimini, ekran okuyucu (screen reader) desteğini ve erişilebilir hata/durum bildirimlerini belgeler.

## 2. WCAG 2.1 AA Uyum Mekanizmaları

### 2.1. Klavye Gezintisi ve Odak Yönetimi (Keyboard Navigation & Focus Management)
- **Tüm İş Akışlarının Klavyeyle Tamamlanması**: Mouse veya dokunmatik ekran olmadan tüm kritik akışlar (randevu alma, muayene panelleri arasında geçiş, bildirim görüntüleme) `Tab`, `Shift+Tab`, `Enter`, `Space` ve kısayol tuşlarıyla işletilebilir.
- **Kısayol Tuşları**: Personel çalışma alanında (`StaffDesktopWorkspace`) `Alt+1` (Randevular), `Alt+2` (Hasta Arama), `Alt+3` (Muayene) ve `Alt+4` (Bildirimler) kısayolları tanımlanmış ve ekran okuyucu ipuçlarıyla desteklenmiştir.
- **Odak Tuzağı Engeli (Focus Trap Prevention)**: Modallar ve açılır pencereler `Escape` ile kapatılabilir; döngüsel odak tuzağına düşülmez.

### 2.2. Ekran Okuyucu ve ARIA Semantiği (Screen Readers & ARIA Landmarks)
- **Anlamsal Bölümler (Landmarks)**: `role="main"`, `role="region"`, `role="tablist"`, `role="tab"`, `role="alert"`, `role="status"` semantik etiketleri eksiksiz kullanılmıştır.
- **Canlı Bölgeler (Live Regions)**: Kritik sistem ve ağ uyarıları (`ConnectionRequiredState`) için `role="alert"` ve `aria-live="assertive"`; durum ve arama sonuçları için (`UiStatePanel`) `role="status"` ve `aria-live="polite"` dinamik anonsları sağlanmıştır.
- **Dekoratif Görseller**: Tüm SVG ve ikonlar `aria-hidden="true"` ile işaretlenerek ekran okuyucuların gereksiz anons yapması engellenmiştir.

### 2.3. Dokunma Hedefleri ve Mobil Kullanılabilirlik (Touch Targets & Mobile Layout)
- **En Az 44x44px Dokunma Alanı**: Mobil portaldaki (`PatientMobileWorkspace`) tüm butonlar `.mobile-touch-btn` sınıfıyla asgari 44x44px fiziksel dokunma alanına sahiptir.
- **Sıfır Yatay Taşma**: 360dp - 412dp genişliğindeki mobil ekranlarda yatay kaydırma engellenmiş; dikey portre düzeni optimize edilmiştir.
- **Donanım/Yazılım Geri Tuşu**: Detay modalı veya sekmesi açıkken donanım geri tuşuna basıldığında uygulama kapanmak yerine bir önceki görünüme kademeli döner (`HandleHardwareBack`).

### 2.4. Renk Kontrastı ve Yüksek Kontrast Desteği (Contrast & High Contrast)
- **Kontrast Oranı**: Metin ve arka plan renk kombinasyonları WCAG AA gereği en az 4.5:1 (büyük metinler için 3:1) kontrast oranını karşılar.
- **Windows High Contrast / Dark Mode**: `@media (forced-colors: active)` medya sorguları ile Windows High Contrast modu ve koyu tema otomatik olarak desteklenir.
- **Yalnızca Renkle Bilgi Vermeme**: Durum göstergeleri (örneğin triyaj seviyeleri, randevu durumları) rengin yanı sıra açık metin etiketleri ve ikonlarla ifade edilir.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.ComponentTests/Accessibility/AccessibilityAndUsabilityTests.cs`:
  - `StaffDesktopWorkspaceProvidesWcagCompliantLandmarksAndKeyboardShortcuts`
  - `PatientMobileWorkspaceEnsuresMainLandmarkAndTouchTargetRequirements`
  - `ConnectionRequiredStateProvidesAccessibleAlertAndAriaLiveAnnouncements`
  - `UiStatePanelProvidesAriaLiveAndRoleConfiguration`
  - `DemoSecurityBannerRendersAccessibleDismissibleNote`
  - `InAppNotificationCenterRendersAccessibleStatusAndCounter`
