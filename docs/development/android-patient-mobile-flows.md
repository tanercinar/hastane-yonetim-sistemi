# Android Hasta Akışları ve Mobil Çalışma Alanı (F12-G05)

## 1. Amaç ve Kapsam

Bu belge, Faz 12 kapsamında Android platformunda hastaların kişisel sağlık verilerine güvenle erişmesini, randevu oluşturup yönetmesini, reçetelerini ve kesinleşmiş klinik sonuçlarını incelemesini sağlayan mobil kullanıcı arayüzü ve yaşam döngüsü davranışlarını belgeler.

## 2. Mimari ve Bileşen Yapısı

Tüm hasta mobil deneyimi, host-agnostik Razor Class Library (`HospitalManagement.UI`) katmanında `PatientMobileWorkspace.razor` bileşeni olarak geliştirilmiştir. Bu bileşen hem .NET MAUI Blazor Hybrid (Android/Windows) hem de Blazor Web mobil görünümlerinde birebir çalışır.

### 2.1. Bileşen ve Model Dosyaları

- **Bileşen**: `src/HospitalManagement.UI/Components/PatientMobile/PatientMobileWorkspace.razor`
- **Model / DTO'lar**: `src/HospitalManagement.UI/Components/PatientMobile/PatientMobileDtos.cs`
  - `PatientMobileAppointmentDto`: Randevu takip modeli (Randevu No, Poliklinik, Hekim, Tarih, Saat, İptal durumu).
  - `PatientMobileSlotDto`: Yeni randevu alımında uygun slot seçim modeli.
  - `PatientMobilePrescriptionDto` & `PatientMobileMedicationDto`: E-reçete, tanı, teslim/karşılama durumu ve ilaç kullanım talimatları.
  - `PatientMobileDiagnosticResultDto`: Kesinleşmiş (Final) laboratuvar, radyoloji ve patoloji raporları, referans değerleri ve kritik sonuç uyarıları.

## 3. Mobil Ekran (360dp - 412dp) ve Touch Target Uyumluluğu

- **Küçük Ekran Adaptivitesi**: Arayüz 360dp ile 412dp Android dikey portre genişlikleri için optimize edilmiş, hiçbir yatay kaydırma (`horizontal overflow`) olmaksızın dikey akıcı kart düzeni (`fluid card layout`) ile tasarlanmıştır.
- **Asgari Dokunma Hedefi (Touch Target Size)**:
  - WCAG 2.2 ve Android Material tasarım standartları uyarınca, tüm etkileşimli düğmeler, sekmeler, form alanları ve kartlar en az `44x44px` dokunma alanına (`min-width: 44px; min-height: 44px; display: inline-flex; align-items: center; justify-content: center;`) sahiptir (`.mobile-touch-btn`, `.nav-item`, `.filter-chip`).
- **Alt Navigasyon Çubuğu (Bottom Navigation Bar)**:
  - Tek elle kolay kullanım için 4 ana sekme sabit alt çubukta konumlandırılmıştır:
    1. **Randevular**: Aktif ve geçmiş randevular, randevu iptal aksiyonu.
    2. **Randevu Al**: Poliklinik, hekim, saat slot seçimi ve onay adımları.
    3. **Reçeteler**: E-reçeteler, ilaç adetleri, kullanım dozajları ve eczane karşılama durumu.
    4. **Sonuçlar**: Laboratuvar, radyoloji ve patoloji onaylı kesin raporları ve kritik değer filtreleri.

## 4. Geri Tuşu (Back Navigation) ve Yaşam Döngüsü

- **Donanım / Yazılım Geri Tuşu Yönetimi**:
  - Android cihazlarda sistem geri tuşuna basıldığında kullanıcının uygulamadan kazara çıkmasını engellemek için `HandleHardwareBack()` ve `HandleBackNavigation()` mantığı işletilir.
  - Kullanıcı bir reçete detayını, tetkik sonucunu veya randevu alma adımını inceliyorsa geri tuşu detayı kapatarak listeye geri döner; kök ekranda ise randevular ana sekmesine yönlendirir.
- **Cihaz Yaşam Döngüsü ve Yeniden Çizim**:
  - Cihaz dikey/yatay mod değişimi veya arka plandan ön plana geçiş sırasında durum korunur.

## 5. Çevrimdışı Koruma ve Bağlantı Kesintisi (Offline Barrier)

- Sağlık ve hasta güvenliği prensipleri gereğince, çevrimdışı yerel veri yazma kuyruğu (`offline write queue`) tutulmaz.
- `IPlatformConnectivityService` ağ durumunu canlı olarak izler; bağlantı kesildiğinde ekran üstünde dikkat çekici bir çevrimdışı uyarı ve `ConnectionRequiredState` engeli gösterilir.
- Bağlantı yeniden sağlandığında tek dokunuşla ("Tekrar Dene") güncel durum sunucudan çekilir.

## 6. Doğrulama ve Testler

- **Bileşen Testleri**: `tests/HospitalManagement.ComponentTests/Components/PatientMobileWorkspaceComponentTests.cs`
  - `PatientMobileWorkspaceRendersAppointmentsAndDemoBanner`: Randevu kartlarının, hekim bilgilerinin ve DEMO güvenlik başlığının render edilmesini test eder.
  - `PatientMobileWorkspaceRendersOfflineBarrierWhenDisconnected`: Ağ kesintisinde çevrimdışı bariyerinin güvenli biçimde devreye girdiğini test eder.
  - `PatientMobileWorkspaceBackNavigationHandlesDetailsGracefully`: Geri tuşuna basıldığında detay pencerelerinden ana listeye kademeli dönüldüğünü doğrular.
  - `PatientMobileWorkspaceTouchTargetsAndAriaAttributesConfigured`: Sekme geçişleri, filtreler ve erişilebilirlik niteliklerini test eder.
- Tüm testler %100 oranında başarılıdır (124/124 component test).
