# Faz 13 Yayın Adayı ve Güvenlik Sertleştirmesi Test Rehberi (F13_Test.md)

Bu rehber yalnızca sentetik `DEMO` verilerle çalıştırılmalıdır. Gerçek hasta, personel, kimlik numarası veya gerçek kurum credential'ı içermez.

> **Kapı durumu:** Otomatik ve manuel yürütme sonucu `ROADMAP.md` ilerleme günlüğünde tutulur.

## 1. Ön Koşullar ve Otomatik Doğrulama

Repository kökünde:

```powershell
# 1. Tüm birim, mimari ve bileşen testleri
dotnet test tests/HospitalManagement.UnitTests/HospitalManagement.UnitTests.csproj -c Release --no-restore
dotnet test tests/HospitalManagement.ComponentTests/HospitalManagement.ComponentTests.csproj -c Release --no-restore
dotnet test tests/HospitalManagement.ArchitectureTests/HospitalManagement.ArchitectureTests.csproj -c Release --no-restore

# 2. Güvenlik, bağımlılık ve tedarik zinciri zafiyet taramaları
powershell -NoProfile -ExecutionPolicy Bypass -File tools/test-dependency-vulnerabilities.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify-platform-packaging.ps1

# 3. Kod biçimlendirme ve dokümantasyon bağlantı doğrulaması
dotnet format --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate-phase0.ps1
```

**Beklenen Çıktı**:
- Build 0 hata / 0 uyarı (`TreatWarningsAsErrors=true`).
- 493/493 Unit testi PASS (0 failed, 0 skipped).
- 153/153 Component testi PASS (0 failed, 0 skipped).
- 15/15 Architecture testi PASS (0 failed, 0 skipped).
- 0 bilinen doğrudan/dolaylı NuGet zafiyeti (CVE).
- 0 keystore/sertifika/secret sızıntısı.
- 0 biçimlendirme sapması (`dotnet format`).
- 0 kırık yerel markdown bağlantısı (`validate-phase0.ps1`).

---

## 2. Kanonik Test Rolleri ve Demo Kimlikleri

| Rol | E-posta | Parola | Kapsam / Yetki Sınırı |
| :--- | :--- | :--- | :--- |
| **Sistem Yöneticisi** | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` | Sistem ayarları, audit inceleme, kullanıcı yönetimi (Klinik erişim YASAK) |
| **Başhekim (CMO)** | `DEMO-cmo@hospital.invalid` | `DEMO-Cmo-Pass!1` | Raporlama dışa aktarma (`report.operations.export`), kilitli muayene yeniden açma |
| **Hekim** | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` | Klinik muayene, reçete yazma, tetkik isteme, kendi hastalarının paneli |
| **Hemşire** | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` | Vital bulgu, bakım planı, eMAR uygulama (Reçete imzalama YASAK) |
| **Kayıt Personeli** | `DEMO-registration@hospital.invalid` | `DEMO-Reg-Pass!1` | Hasta kabul, randevu kaydı, kimlik doğrulama |
| **Eczacı** | `DEMO-pharmacist@hospital.invalid` | `DEMO-Pharm-Pass!1` | İlaç teslimi, FEFO stok yönetimi (Klinik SOAP notları İZOLE) |
| **Laboratuvar Teknisyeni**| `DEMO-lab@hospital.invalid` | `DEMO-Lab-Pass!1` | Numune kabul, teknik onay (Klinik teşhis/onay YASAK) |
| **Hasta** | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` | Kendi randevuları, onaylı reçeteleri ve laboratuvar sonuçları (IDOR YASAK) |

---

## 3. Faz 13 Güvenlik ve Sertleştirme Sonuç Matrisi

```text
Test Eden: Antigravity Automated Release Candidate Engine
Tarih/Saat: 2026-09-04
.NET SDK: 10.0.201
Hedef Ortamlar: Web (Blazor), Windows (net10.0-windows10.0.19041.0), Android (net10.0-android)

[x] F13-G01 Tam Regresyon & Başarı Ölçütleri İzlenebilirlik Matrisi     : PASS
[x] F13-G02 Yetkilendirme Saldırı Paketi (IDOR, Eskalasyon, Scope)       : PASS
[x] F13-G03 Web Uygulama Güvenliği (OWASP ASVS L2, Security Headers)     : PASS
[x] F13-G04 Kimlik ve Secret Güvenliği (Lockout, Timing, MFA, Pkce)      : PASS
[x] F13-G05 Veri ve Audit Bütünlüğü (Hash Chaining, Concurrency, KVKK)   : PASS
[x] F13-G06 Bağımlılık ve Tedarik Zinciri (CPM, Zero CVE, compose.yaml)  : PASS
[x] F13-G07 Performans ve Eşzamanlılık (Slot, Bed, Stock Invariants)     : PASS
[x] F13-G08 Erişilebilirlik ve Kullanılabilirlik (WCAG 2.1 AA, Bunit)    : PASS
[x] F13-G09 Hata ve Dayanıklılık (SafeRetry, RFC 9457 Problem Details)   : PASS
[x] F13-G10 Gizlilik İncelemesi (Canary Sızıntı Testi, Data Minimization): PASS
[x] F13-KAPI Faz 13 Yayın Adayı Kapısı Doğrulaması                      : PASS

Açık Kritik / Yüksek Güvenlik Zafiyeti: 0
Kalan Düşük Riskler: Sentetik demo disclaimer ve mock sağlayıcı etiketleri belgelendi.
Genel Karar: PASS — Yayın Adayı (Release Candidate) Onaylandı.
```

---

## 4. Manuel Keşif ve Rol İzolasyonu Test Adımları

1. **Hasta-Hasta IDOR Testi**:
   - `DEMO-patient@hospital.invalid` olarak giriş yap.
   - Başka bir hastanın tanısal sonucunu veya reçete ID'sini URL üzerinden çağır (`/results/my` veya `/api/v1/diagnostics/results/{baska-hasta-id}`).
   - **Beklenen**: `403 Forbidden` veya `404 Not Found` dönmeli; audit kaydına hastanın sorguladığı klinik içerik yazılmamalıdır.
2. **Klinik Menü İzolasyonu (Admin)**:
   - `DEMO-admin@hospital.invalid` olarak giriş yap.
   - Sol menüde Poliklinik, eMAR, Ameliyathane gibi klinik çalışma alanlarının gizlendiğini doğrula.
   - Doğrudan `/api/v1/clinical-records/notes` çağırmayı dene.
   - **Beklenen**: `403 Forbidden`.
3. **WCAG 2.1 AA Yalnızca Klavye Gezintisi**:
   - Mouse kullanmaksızın `Tab`, `Shift+Tab`, `Alt+1..4` tuşlarıyla Personel Masaüstü Çalışma Alanında sekmeler arasında gezin.
   - **Beklenen**: Odak halkası net görünür, hiçbir odak tuzağı oluşmaz, ekran okuyucu canlı bölge uyarılarını seslendirir.
