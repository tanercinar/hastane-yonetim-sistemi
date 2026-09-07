# Hastane Yönetim Sistemi (Hospital Management System)

[![CI](https://github.com/tanercinar/hastane-yonetim-sistemi/actions/workflows/ci.yml/badge.svg)](https://github.com/tanercinar/hastane-yonetim-sistemi/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor%20Web%20%26%20MAUI-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL%2018-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Infrastructure-Docker%20Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-660%2B%20Passing-brightgreen)](tests/)
[![Security](https://img.shields.io/badge/OWASP-ASVS%205.0%20L2-orange)](docs/security/)
[![Accessibility](https://img.shields.io/badge/WCAG-2.1%20AA-blue)](docs/development/accessibility-and-usability.md)

Eğitim, staj ve mühendislik portföyü amacıyla sıfırdan geliştirilmiş, sağlık bilişimi standartlarına uygun, yüksek güvenlikli ve **Modüler Monolit (Modular Monolith)** mimarili kapsamlı bir hastane bilgi ve klinik yönetim sistemi simülasyonudur.

> ⚠️ **ÖNEMLİ YASAL FERAGAT VE GÜVENLİK UYARISI:**  
> Bu sistem **yalnızca sentetik `DEMO` verileriyle** çalışır. Sertifikalı bir T.C. Sağlık Bakanlığı HBYS'si, tıbbi cihaz veya klinik karar destek yazılımı **değildir**. Uygulamaya gömülü yapay zekâ veya otomatik teşhis mekanizması bulunmaz; Antigravity, Codex ve Cursor gibi araçlar yalnızca geliştirme ve test otomasyonunda eş-programcı olarak kullanılmıştır. **Sisteme hiçbir koşulda gerçek hasta verisi, T.C. Kimlik Numarası veya kurumsal kimlik bilgisi girilemez.**

---

## 🏛️ Mimari ve Teknoloji Yığını

Sistem, mikroservis karmaşıklığına girmeden güçlü modüler sınırlar sağlayan **Modular Monolith** ve **Clean Architecture / Domain-Driven Design (DDD)** prensiplerine dayanır:

* **Çekirdek Çerçeve:** .NET 10 (C# 13 / 14)
* **Web İstemcisi:** ASP.NET Core Blazor Web App (Interactive WebAssembly & SSR, Responsive Türkçe Arayüz)
* **Mobil & Masaüstü İstemcisi:** .NET MAUI Blazor Hybrid (Windows ve Android platformları için paylaşılan `HospitalManagement.UI` Razor Class Library)
* **İlişkisel Veritabanı:** PostgreSQL 18.6 & Entity Framework Core (Her Bounded Context için izole şemalar ve bağımsız migration'lar)
* **Nesne Depolama:** MinIO (S3 Uyumlu Klinik Ekler, Raporlar ve Sentetik DICOM Simülasyonu)
* **E-posta Altyapısı:** Mailpit (MOCK yerel SMTP ve Web Arayüzü)
* **Gerçek Zamanlı İletişim:** ASP.NET Core SignalR (Poliklinik sıraları, acil triyaj panoları, kritik sonuç uyarıları)
* **Gözlemlenebilirlik:** OpenTelemetry, Structured Logging, Health Checks (`/health/live`, `/health/ready`), Correlation ID
* **Güvenlik Mimarisi:** OWASP ASVS 5.0 L2, Katı HTTP Başlıkları (CSP, HSTS, `X-Frame-Options: DENY`, `nosniff`), RFC 7636 PKCE S256, RBAC + ABAC + Care Relationship Yetkilendirmesi, Kriptografik SHA-256 Audit Blok Zinciri

---

## 📦 Modül Kataloğu ve Kapsam

Sistem `src/Modules/` altında 14 bağımsız alt alana ayrılmıştır:

1. **Organization:** Hastane, şube, bina, kat, klinik departmanlar ve personel rol/uzmanlık atamaları.
2. **IdentityAccess:** ASP.NET Core Identity, parola politikası, lockout (5 hatalı deneme / 15 dk), MFA TOTP, RFC 2606 `.invalid` e-posta kısıtı, timing attack koruması.
3. **Patients:** Hasta ana indeksi (MPI), demografik kayıt, güvenli arama, KVKK/GDPR unutulma hakkı ile geri döndürülemez anonimleştirme (`Patient.Anonymize`).
4. **Scheduling:** Hekim poliklinik slot motoru, randevu oluşturma, çakışma engelleme, check-in, no-show ve gerçek zamanlı poliklinik çağrı kuyruğu.
5. **ClinicalRecords:** SOAP klinik notlar, ICD-10 tanı kodlama kataloğu, branş konsültasyonları, güvenli dosya ekleri (magic byte & path traversal denetimi), hastanın longitudinal zaman çizelgesi.
6. **Pharmacy:** İlaç kataloğu, e-reçete yaşam döngüsü, kural tabanlı ilaç etkileşim ve alerji kontrolü, FEFO (First-Expired, First-Out) lot stok yönetimi ve eczane teslimi.
7. **Diagnostics:** Laboratuvar ve radyoloji istemleri, numune barkodlama ve zincirleme gözetim (custody chain), panik kritik değer algılama ve eskalasyon bildirimleri, sentetik PACS/DICOM önizleme simülatörü, patoloji ve kan bankası uygunluk matrisi.
8. **Inpatient:** Servis, oda ve yatak aggregate root modeli, yatış/transfer/taburculuk akışları, eMAR (Elektronik İlaç Uygulama - 5 Doğru Kuralı), hemşire vital gözlem ve bakım planları.
9. **Emergency:** Acil kabul, renk kodlu triyaj sınıflandırması (Kırmızı/Sarı/Yeşil), canlı acil takip panosu, hızlı STAT istemler ve disposition yönetimi.
10. **SurgeryCriticalCare:** Ameliyathane salon planlama, Pre-Op kontrol listesi, perioperatif anestezi ve ameliyat notu (immutability), Yoğun Bakım (ICU) flowsheet izlem ve saatlik sıvı dengesi (I&O), ISBAR klinik devir-teslim panosu.
11. **SpecialtyCare:** Kadın doğum ve gebelik takibi (Naegele kuralı), doğum ve yenidoğan kayıtları (Apgar skoru), Diş hekimliği (FDI iki basamaklı odontogram), Evde sağlık hizmetleri planlaması.
12. **Interoperability (Sağlık Bilişimi Entegrasyonları):** HL7 v2 (ADT/ORM/ORU) ER7 simülasyonu, FHIR R4 kaynak dönüşümleri, DICOM Modality Worklist (MWL), MHRS randevu mock motoru, e-Nabız paket gönderim simülatörü (101-106 paketleri), MEDULA sınır kontratları.
13. **Reporting:** Olay güdümlü operasyonel projeksiyonlar (`IReportingProjectionEngine`), poliklinik, yatak doluluk, eczane ve acil panoları, formül enjeksiyonuna karşı korumalı güvenli CSV dışa aktarma (`ISecureExportService`).
14. **Notifications & AuditPrivacy:** Kanonik şablon kataloğu, KVKK/GDPR uyumlu PHI içermeyen kilit ekranı bildirimleri, SHA-256 kriptografik kurcalama dirençli denetim günlüğü (`AuditLogEntry`).

### Kapsam Dışı (Out of Scope)
* Finans, muhasebe, faturalandırma, SGK/MEDULA gerçek tahsilat ve bordro süreçleri (finansal model içermez).
* Gerçek dış hastane veya e-Devlet sistemlerine canlı ağ çağrıları (tüm dış protokoller izole mock/simülatördür).

---

## 🔑 Demo Hesapları ve Test Rolleri

Veritabanı hazırlandığında aşağıdaki roller kullanıma hazır sentetik kimliklerle tohumlanır:

| Rol | Demo E-Posta | Parola | Temel Yetki ve Kapsam |
| :--- | :--- | :--- | :--- |
| **Sistem Yöneticisi** | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` | Sistem ayarları, kullanıcı yetkilendirme, denetim izleme (Klinik verilere erişemez) |
| **Başhekim (CMO)** | `DEMO-cmo@hospital.invalid` | `DEMO-Cmo-Pass!1` | Operasyonel raporlar, CSV export (`report.operations.export`), muayene yeniden açma |
| **Hekim** | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` | Muayene, klinik not, e-reçete, tetkik istemi, hasta paneli |
| **Hemşire** | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` | Vital bulgu, hasta gözlem, bakım görevleri, eMAR ilaç uygulama (Reçete yazamaz) |
| **Kayıt Görevlisi** | `DEMO-registration@hospital.invalid` | `DEMO-Reg-Pass!1` | Hasta kabul, randevu kaydı, kimlik bilgisi güncelleme |
| **Eczacı** | `DEMO-pharmacist@hospital.invalid` | `DEMO-Pharm-Pass!1` | Reçete karşılama, FEFO stok çıkışı (Hekim SOAP notları izoledir) |
| **Laboratuvar Teknisyeni** | `DEMO-lab@hospital.invalid` | `DEMO-Lab-Pass!1` | Numune kabul, barkod tarama, teknik sonuç onayı |
| **Hasta Portalı** | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` | Randevu alma, kendi reçetelerini ve onaylanmış laboratuvar sonuçlarını inceleme |

> 💡 **İpucu:** Web giriş sayfasında (`/account/login`) tek tıkla form dolduran **"Hızlı Demo Girişi"** butonları yer almaktadır.

---

## 🚀 Hızlı Kurulum ve Terminalden Çalıştırma

### Ön Koşullar
* **Windows 10/11**
* **.NET 10 SDK** (`10.0.201` veya üzeri)
* **Docker Desktop** (Çalışır durumda olmalı)
* **PowerShell 5.1** veya **PowerShell 7+**

### 1. Repoyu Klonlayın
```powershell
git clone https://github.com/tanercinar/hastane-yonetim-sistemi.git
cd hastane-yonetim-sistemi
```

### 2. Bağımlılıkları Yükleyin ve Derleyin
```powershell
dotnet restore .\HospitalManagement.slnx --locked-mode --configfile .\NuGet.Config
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
```
*(Build çıktısında `0 Warning` ve `0 Error` hedeflenir).*

### 3. Altyapı Servislerini Başlatın (PostgreSQL, MinIO, Mailpit)
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
```
*Bu betik Docker konteynerlerini ayağa kaldırır, portları `127.0.0.1` üzerine bağlar ve `.env` dosyasını otomatik oluşturur.*

### 4. Development Gizli Anahtarlarını Yapılandırın
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
```

### 5. Veritabanı Şemalarını ve Demo Verileri Uygulayın
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
```

### 6. Uygulamayı Başlatın
```powershell
dotnet dev-certs https --trust
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

Aşağıdaki bağlantılardan sisteme erişebilirsiniz:
* **Web Arayüzü:** [https://localhost:7111](https://localhost:7111)
* **Mailpit (Mock E-Postalar):** [http://localhost:8025](http://localhost:8025)
* **MinIO Konsolu:** [http://localhost:9001](http://localhost:9001)
* **OpenAPI (Swagger) Şeması:** [https://localhost:7111/openapi/v1.json](https://localhost:7111/openapi/v1.json)
* **Sağlık Kontrolleri:** [https://localhost:7111/health/ready](https://localhost:7111/health/ready)

---

## 🧪 Test Otomasyonu ve Kalite Kapıları

Proje, yazılım mühendisliği disiplininin gereği olarak kapsamlı otomatik testlerle donatılmıştır:

```powershell
# Tüm test paketini çalıştırın (Unit, Component, Architecture, Integration, E2E)
dotnet test .\HospitalManagement.slnx -c Release --no-build --no-restore

# 1. Güvenlik ve Bağımlılık Zafiyet Taraması (0 bilinen CVE)
powershell -ExecutionPolicy Bypass -File .\tools\test-dependency-vulnerabilities.ps1

# 2. Mobil/Masaüstü Paketleme ve Sıfır Keystore/Secret Kontrolü
powershell -ExecutionPolicy Bypass -File .\tools\verify-platform-packaging.ps1

# 3. Kod Biçimlendirme Doğrulaması
dotnet format .\HospitalManagement.slnx --no-restore --verify-no-changes

# 4. Dokümantasyon Bağlantı Doğrulayıcı
powershell -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

### Test Dağılımı:
* **499+ Unit Testi:** Domain kuralları, durum makineleri, kriptografik hash zinciri, concurrency, yetkilendirme saldırı paketi.
* **153+ Component Testi:** Bunit tabanlı Blazor UI durumları (Loading/Empty/Error/Forbidden), WCAG 2.1 AA klavye ve odak testleri.
* **15+ Architecture Testi:** ArchUnitNET ile katman kuralları, sızıntısız CI/CD kontrolleri, modüller arası izolasyon garantisi.
* **160+ Integration Testi:** Gerçek PostgreSQL Testcontainers üzerinde çoklu rol ve uçtan uca senaryolar.

---

## 📚 Dokümantasyon Dizini

* Yol Haritası ve Faz Durumları: [`ROADMAP.md`](ROADMAP.md)
* Faz 13 Yayın Adayı Raporu: [`docs/development/phase-13-release-candidate-gate.md`](docs/development/phase-13-release-candidate-gate.md)
* Test ve Doğrulama Kılavuzu: [`F13_Test.md`](F13_Test.md)
* Mimari Karar Kayıtları (ADR): [`docs/adr/README.md`](docs/adr/README.md)
* Yetkilendirme Matrisi: [`docs/security/authorization-matrix.md`](docs/security/authorization-matrix.md)
* Veri Sınıflandırma ve KVKK Standartları: [`docs/privacy/data-classification.md`](docs/privacy/data-classification.md)
* Test Stratejisi: [`docs/testing/test-strategy.md`](docs/testing/test-strategy.md)

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında geliştirilmiş açık kaynaklı bir eğitim ve portföy çalışmasıdır.
