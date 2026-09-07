# Faz 12 Manuel ve Çoklu İstemci Test Rehberi (F12_Test.md)

Bu rehber yalnızca sentetik `DEMO` verilerle çalıştırılmalıdır. Gerçek hasta, personel veya kurum bilgisi içermez.

> **Kapı durumu:** Otomatik ve manuel yürütme sonucu `ROADMAP.md` ilerleme günlüğünde tutulur.

## 1. Ön Koşullar ve Otomatik Doğrulama

Repository kökünde:

```powershell
# 1. Tüm birim, mimari ve bileşen testleri
dotnet test tests/HospitalManagement.UnitTests/HospitalManagement.UnitTests.csproj -c Release --no-restore
dotnet test tests/HospitalManagement.ComponentTests/HospitalManagement.ComponentTests.csproj -c Release --no-restore
dotnet test tests/HospitalManagement.ArchitectureTests/HospitalManagement.ArchitectureTests.csproj -c Release --no-restore

# 2. Windows ve Android MAUI platform derleme ve gizli anahtar/keystore taraması
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify-platform-packaging.ps1

# 3. Biçimlendirme ve kök belge doğrulaması
dotnet format --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate-phase0.ps1
```

Beklenen: Build 0 uyarı / 0 hata; 147 component/gate testi PASS; 423 unit testi PASS; 13 mimari testi PASS; 0 sertifika/keystore sızıntısı; 0 bozuk bağlantı.

### Test Kullanıcıları ve Kimlik Bilgileri

| Rol | E-posta | Parola | Platform / Arayüz |
| :--- | :--- | :--- | :--- |
| **Sistem Yöneticisi** | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` | Web / Yönetim |
| **Hekim** | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` | Windows (.NET MAUI) / Web |
| **Hemşire** | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` | Windows (.NET MAUI) / Web |
| **Kayıt Personeli** | `DEMO-registration@hospital.invalid` | `DEMO-Reg-Pass!1` | Windows (.NET MAUI) / Web |
| **Hasta** | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` | Android (.NET MAUI) / Web |

---

## 2. Sonuç Matrisi

```text
Test eden:
Tarih/saat:
.NET SDK: 10.0.201
Windows Target: net10.0-windows10.0.19041.0
Android Target: net10.0-android

F12-M01 Native OIDC PKCE S256 ve Sistem Tarayıcısı Akışı   : PASS / FAIL
F12-M02 .NET MAUI Blazor Hybrid Çoklu Platform Derlemesi  : PASS / FAIL
F12-M03 API İstemcisi, Problem Details ve Offline Bariyeri : PASS / FAIL
F12-M04 Windows Personel Masaüstü Akışları & Kısayollar   : PASS / FAIL
F12-M05 Android Hasta Akışları, Geri Tuşu & Touch Targets  : PASS / FAIL
F12-M06 PHI-Safe Bildirimler ve hospitalapp:// Deep-Link   : PASS / FAIL
F12-M07 Platform Paketleme ve Sıfır Keystore Sızıntısı     : PASS / FAIL
F12-KAPI Çoklu İstemci Sunucu Tek Doğruluk Kaynağı         : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

---

## 3. Ayrıntılı Manuel Senaryolar

### Senaryo 1: Windows Personel İş İstasyonu ve Klavye Navigasyonu (F12-M04)
1. Windows istemcisini başlatın (`HospitalManagement.Maui.exe`).
2. Hekim rolüyle oturum açın (`DEMO-doctor@hospital.invalid`).
3. Fareye dokunmadan `Alt + 1` tuşuna basın; hasta arama kutusuna odaklanıldığını doğrulayın.
4. `Alt + 2` tuşuna basın; randevu tablosuna geçildiğini, `Tab` ve `Enter` ile randevu seçilebildiğini gözlemleyin.
5. `Alt + 3` ile seçili randevunun klinik özet panelini (vital bulgular, tetkikler, reçeteler) inceleyin.
6. Windows Yüksek Kontrast (High Contrast) modunu açın; renklerin ve metinlerin bozulmadığını doğrulayın.

### Senaryo 2: Android Hasta Portalı ve Touch Target Doğrulaması (F12-M05)
1. Android emülatöründe (veya fiziksel cihazda) hasta rolüyle portalı açın (`DEMO-patient@hospital.invalid`).
2. Ekran genişliğini 360dp - 412dp arasında test edin; yatay kaydırma çubuğunun oluşmadığını teyit edin.
3. Alt menüdeki 4 sekmeyi (`Randevular`, `Randevu Al`, `Reçeteler`, `Sonuçlar`) test edin; tüm düğmelerin en az `44x44px` dokunma alanına sahip olduğunu kontrol edin.
4. "Reçeteler" sekmesine geçin, bir reçete kartına dokunun. Reçete detay paneli açıldığında Android sistem geri tuşuna basın.
5. **Beklenen**: Uygulama kapanmamalı; yalnızca detay paneli kapanıp reçeteler listesine geri dönmelidir.

### Senaryo 3: Ağ Kesintisi ve Sıfır Çevrimdışı Kuyruk (F12-M03 / F12-KAPI)
1. Hasta veya personel ekranı açıkken cihazı uçak moduna alın (veya internet bağlantısını kesin).
2. Sayfada işlem yapmayı (randevu alma, iptal etme vb.) deneyin.
3. **Beklenen**: Hiçbir veri arka planda yerel kuyruğa (`offline queue`) yazılmaz. Ekranda anında "Bağlantı Gerekli" bariyeri görüntülenir.
4. Bağlantıyı tekrar açın ve "Yeniden Dene" butonuna basın; güncel verinin doğrudan sunucudan çekildiğini doğrulayın.

### Senaryo 4: Kilit Ekranı PHI Filtresi ve Güvenli Deep-Link (F12-M06)
1. Tanısal bir test sonucu veya yeni reçete bildirimi tetikleyin.
2. Cihaz kilit ekranında veya bildirim banner'ında metni gözlemleyin.
3. **Beklenen**: Bildirimde kesinlikle ilaç adı, tanı/teşhis, kanser/hiv/diyabet veya sayısal değer yer almaz; yalnızca "Sonuç Bildirimi: Tanısal tetkik sonucunuz onaylandı" şeklinde genel başlık yer alır.
4. Tarayıcıdan veya komut satırından manipüle edilmiş `hospitalapp://staff-workspace` linki açmayı deneyin (Hasta rolüyle).
5. **Beklenen**: Yetkisiz yönlendirme engellenir ve `/forbidden` ekranı gösterilir; uygulama asla çökmez.
