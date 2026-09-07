# Windows Temel Akışları ve Personel Masaüstü İş İstasyonu (F12-G04)

## 1. Amaç ve Kapsam

Bu belge, Faz 12 kapsamında Windows platformunda çalışan klinik ve idari personelin operasyonel ihtiyaçlarını karşılamak üzere tasarlanan personel iş istasyonu (staff desktop workspace) arayüzünü, klavye navigasyonunu ve erişilebilirlik standartlarını belgeler.

## 2. Mimari ve Bileşen Yapısı

Personel iş istasyonu, host-agnostik Razor Class Library (`HospitalManagement.UI`) içerisinde `StaffDesktopWorkspace.razor` bileşeni olarak uygulanmıştır. Bu sayede hem Windows masaüstü (.NET MAUI Blazor Hybrid) hem de Web ortamında tutarlı bir kullanıcı deneyimi sunulur.

### 2.1. Bileşen Bileşenleri ve Veri Modeli

- **Bileşen Dosyası**: `src/HospitalManagement.UI/Components/StaffWorkspace/StaffDesktopWorkspace.razor`
- **Model / DTO'lar**: `src/HospitalManagement.UI/Components/StaffWorkspace/StaffWorkspaceDtos.cs`
  - `StaffAppointmentSummaryDto`: Poliklinik randevu kayıtları (Saat, Hasta Adı, Protokol No, Şikayet, Durum, Öncelik).
  - `StaffPatientSearchResultDto`: Hasta arama ve demografik hızlı erişim sonuçları.
  - `StaffEncounterSummaryDto`: Aktif muayene özeti, şikayet, klinik notlar, laboratuvar/radyoloji tetkikleri, reçete edilen ilaçlar.
  - `StaffWorkspaceNotificationDto`: Acil konsültasyon ve kritik tetkik uyarıları.

## 3. Geniş Ekran ve Master-Detail Düzeni

Windows masaüstü ortamında (1080p ve üzeri çözünürlükler):
1. **Sol Panel (Master)**: Randevu listesi ve durum filtreleri (Tümü, Bekliyor, Muayenede, Tamamlandı).
2. **Sağ Panel (Detail)**: Aktif seçili randevunun hasta bilgileri, klinik karşılaşma notları, tetkik istemleri ve reçete özeti.
3. **Üst Çubuk**: Hızlı hasta arama alanı (ad, soyad veya TCKN) ve acil bildirim paneli.
4. **Hızlı Eylem Çubuğu**: Muayene başlatma, reçete yazma, tetkik isteme ve muayeneyi tamamlama düğmeleri.

## 4. Klavye Navigasyonu ve Kısayollar

Kullanıcıların fareye ihtiyaç duymadan akışları tamamlayabilmesi için erişilebilir klavye desteği sağlanmıştır:

| Kısayol | İşlev | Açıklama |
| :--- | :--- | :--- |
| `Alt + 1` | Hasta Arama Odağı | Arama girdi kutusuna doğrudan odaklanır (`id="staff-patient-search-input"`). |
| `Alt + 2` | Randevu Listesi Odağı | Sol paneldeki randevu tablosuna odaklanır. |
| `Alt + 3` | Karşılaşma Özeti Odağı | Sağ detay panelindeki klinik içerik alanına odaklanır. |
| `Alt + 4` | Bildirimler Odağı | Acil bildirim rozetine ve paneline odaklanır. |
| `Tab / Shift+Tab` | Mantıksal Sıra | Tüm etkileşimli alanlar mantıksal DOM sırasına göre gezilir. |
| `Enter / Space` | Eylem Tetikleme | Seçili randevuyu detay panelinde açar veya aksiyon butonunu çalıştırır. |

## 5. Erişilebilirlik ve Windows High Contrast Desteği

- **Yüksek Kontrast (High Contrast)**: CSS içinde `@media (forced-colors: active)` sorgusu ile Windows Yüksek Kontrast temaları desteklenir. Arka planlar `Canvas`, kenarlıklar `CanvasText` ve `Highlight`, metinler `ButtonText` / `CanvasText` sistem renk değişkenlerine bağlanmıştır.
- **Ekran Okuyucu (Narrator / Screen Reader)**:
  - `role="region"` ve `aria-label` etiketleri ile iş istasyonu bölgeleri tanımlanmıştır.
  - Randevu durum değişiklikleri ve arama sonuçları `aria-live="polite"` alanları üzerinden bildirilir.
  - Etkileşimli öğeler için `aria-keyshortcuts` ve `aria-selected` özellikleri tanımlanmıştır.

## 6. Doğrulama ve Testler

- **Bileşen Testleri**: `tests/HospitalManagement.ComponentTests/Components/StaffDesktopWorkspaceComponentTests.cs`
  - `StaffDesktopWorkspace_RendersAppointmentsAndEncounterSummary_Correctly`: Masaüstü master-detail görünümünün doğru render edildiğini test eder.
  - `StaffDesktopWorkspace_SelectingAppointment_UpdatesEncounterDetails`: Randevu seçimiyle detay panelinin reaktif güncellenmesini doğrular.
  - `StaffDesktopWorkspace_KeyboardShortcutsAndAccessibility_AttributesPresent`: Kısayol (`aria-keyshortcuts`), `role` ve erişilebilirlik niteliklerini doğrular.
- Tüm testler %100 oranında başarılıdır.
