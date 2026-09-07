# Platform Test ve Paketleme (F12-G07)

## 1. Amaç ve Kapsam

Bu belge, .NET MAUI Blazor Hybrid mimarisiyle geliştirilen Windows ve Android yerel istemcilerinin derleme, paketleme, duman (smoke) testi ve güvenlik doğrulama süreçlerini belgeler.

## 2. Derleme ve Paketleme Yönergeleri

Uygulama `src/HospitalManagement.Maui/HospitalManagement.Maui.csproj` projesi altında çoklu hedefli (.NET 10 MAUI) olarak yapılandırılmıştır:
- Windows: `net10.0-windows10.0.19041.0` (x64)
- Android: `net10.0-android` (API 34+)

### 2.1. Windows İstemcisi Derleme ve Paketleme

#### Geliştirici / Debug Derleme:
```powershell
dotnet build src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-windows10.0.19041.0 -c Debug
```

#### Unpackaged (Geliştirme / Test Çalıştırması):
```powershell
dotnet publish src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
```

#### MSIX Paketleme (Paketli Dağıtım):
```powershell
dotnet publish src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-windows10.0.19041.0 -c Release -p:GenerateAppxPackageOnBuild=true
```

### 2.2. Android İstemcisi Derleme ve Paketleme

#### Geliştirici / Debug APK Derleme:
```powershell
dotnet build src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-android -c Debug
```

#### İmzalı / Sürüm APK Dağıtımı:
```powershell
dotnet publish src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

#### Google Play / Dağıtım AAB (Android App Bundle):
```powershell
dotnet publish src/HospitalManagement.Maui/HospitalManagement.Maui.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=aab
```

## 3. Güvenlik Politikası: İmzalama ve Keystore Gizliliği

1. **Repoda Sıfır Sertifika / Keystore İlkesi**:
   - Hiçbir özel anahtar (`.key`, `.snk`), sertifika (`.pfx`, `.p12`, `.cer`) veya Android keystore (`.keystore`, `.jks`) dosyası kaynak kod deposuna taahhüt edilmez (`committed`).
   - `.gitignore` kuralları ile tüm bu uzantılar kesin olarak yok sayılmıştır.
2. **CI / Dağıtım Sırları**:
   - Üretim veya test imzalaması için gereken sertifikalar ve anahtar parolaları yalnızca CI/CD ortamında (GitHub Actions Secrets, Azure Key Vault) Base64 kodlanmış gizli değişkenler olarak tanımlanır ve derleme esnasında geçici disk alanında kullanılır.
3. **Otomatik Güvenlik Doğrulama Betiği**:
   - `tools/verify-platform-packaging.ps1` betiği tüm projeyi tarayarak açıkta sertifika, keystore veya sabit parola olmadığını doğrular.

## 4. Platform Duman (Smoke) Test Senaryoları

| Senaryo | Hedef Platform | Doğrulama Adımı | Beklenen Sonuç |
| :--- | :--- | :--- | :--- |
| **Windows Açılış & Giriş** | Windows 10/11 | `HospitalManagement.Maui.exe` başlatılır, sistem tarayıcısı ile OIDC girişi tetiklenir. | Tarayıcıda oturum açılır, `hospitalapp://oauth/callback` ile uygulamaya dönülür. |
| **Windows Personel İş İstasyonu** | Windows 10/11 | `Alt+1` ile hasta arama, randevu listesi ve detay paneli gezilir. | Klavye ile tam kontrol sağlanır, High Contrast modunda renkler net kalır. |
| **Android Açılış & Portre Ekran** | Android Emülatör / Cihaz | 360dp - 412dp genişlikte başlatılır, sekme geçişleri yapılır. | Hiçbir yatay taşma oluşmaz, tüm butonlar en az 44x44px dokunma alanına sahiptir. |
| **Android Geri Tuşu** | Android Emülatör / Cihaz | Reçete detayı veya tahlil detayı açıkken sistem geri tuşuna basılır. | Uygulama kapanmaz; detay ekranı kapanıp listeye güvenle geri dönülür. |
| **Ağ Kesintisi (Offline Barrier)** | Windows & Android | Cihaz uçak moduna alınır veya Wi-Fi kesilir. | Veri yazma kuyruğu tutulmaz; anında "Bağlantı Gerekli" ekranı gösterilir. |

## 5. Doğrulama ve Test Çıktısı

- `tools/verify-platform-packaging.ps1` betiği çalıştırılmış ve tam başarıyla tamamlanmıştır:
  - `WindowsTarget`: `net10.0-windows10.0.19041.0` (0 hata, 0 uyarı)
  - `AndroidTarget`: `net10.0-android` (0 hata, 0 uyarı)
  - `SecretLeaksDetected`: 0
  - `KeystoresCommitted`: 0
  - `Status`: PASS
