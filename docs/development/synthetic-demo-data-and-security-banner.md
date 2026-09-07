# Sentetik Demo Verisi ve Güvenlik Bildirimi (F02-G07)

Bu belge, Hastane Yönetim Sistemi projesinde uygulanan sentetik kullanıcı/rol tohumlama (`IdentityDataSeeder`), `DEMO-*` adlandırma kuralı ve her ekranda zorunlu olarak gösterilen güvenlik banner'ı (`DemoSecurityBanner`) standartlarını açıklar.

## 1. Mimari Kararlar ve Simülasyon İlkeleri

Proje ilkeleri [`docs/adr/ADR-0006-synthetic-data-only.md`](../adr/ADR-0006-synthetic-data-only.md) ve [`docs/privacy/data-classification.md`](../privacy/data-classification.md) belgelerine dayanır:

- **Yalnız Sentetik Veri (`DEMO_SYNTHETIC`):** Sistemde hiçbir gerçek hasta, personel veya kurum verisi bulunmaz. Tüm e-postalar açık `DEMO-` prefixi taşır ve `.invalid` / `.test` alan adlarını kullanır.
- **Deterministik ve Tekrarlanabilir Tohumlama (`Idempotent Seed`):** `IdentityDataSeeder` servisi 12 kanonik rolü ve 10 temsilî sentetik kullanıcıyı (yönetici, doktor, hemşire, eczacı, laborant, radyoloji teknikeri, danışma, başhekim, hasta ve denetçi) deterministik GUID kimlikleriyle veritabanına ekler. Tekrar tekrar çalıştırıldığında hata vermez veya mükerrer kayıt üretmez.
- **Sertifikasız HBYS Uyarısı (Security Banner):** Blazor arayüzünün her sayfasında (`MainLayout`) kalıcı olarak yer alan `DemoSecurityBanner` bileşeni, sistemin klinik karar desteği veya sertifikalı bir sağlık bilgi sistemi olmadığını; eğitim ve portföy amacıyla simüle edildiğini kullanıcılara açıkça duyurur.

## 2. Tohumlanan Standart Demo Hesapları

| Rol Kodu | E-posta | Görev Tanımı | Hesap Türü |
|---|---|---|---|
| `ADM` | `DEMO-admin@hospital.invalid` | Sistem Yöneticisi | Personel |
| `DOC` | `DEMO-doctor@hospital.invalid` | Doktor | Personel |
| `NUR` | `DEMO-nurse@hospital.invalid` | Hemşire | Personel |
| `PHA` | `DEMO-pharmacist@hospital.invalid` | Eczacı | Personel |
| `LAB` | `DEMO-labtech@hospital.invalid` | Laboratuvar Personeli | Personel |
| `RAD` | `DEMO-radtech@hospital.invalid` | Radyoloji Personeli | Personel |
| `REG` | `DEMO-receptionist@hospital.invalid` | Kayıt/Danışma | Personel |
| `CHM` | `DEMO-chief@hospital.invalid` | Başhekim | Personel |
| `PAT` | `DEMO-patient@hospital.invalid` | Hasta | Hasta |
| `MGR` | `DEMO-manager@hospital.invalid` | Hastane Yöneticisi | Personel |

## 3. Güvenlik Banner Bileşeni (`DemoSecurityBanner.razor`)

Arayüzde erişilebilirlik standartlarına uygun `role="region" aria-label="EĞİTİM VE SİMÜLASYON ORTAMI"` niteliği ile render edilir ve şu uyarı metinlerini içerir:

1. **Uyarı:** `Gerçek hasta verisi, gerçek T.C. kimlik numarası veya gerçek sağlık kaydı girmeyiniz. Yalnız sentetik DEMO veriler kullanılır.`
2. **Sorumluluk Reddi:** `Bu sistem sertifikalı bir HBYS veya klinik karar destek aracı değildir; eğitim ve portföy amacıyla geliştirilmektedir.`

## 4. Doğrulama ve Test Kapsamı

- **Birim Testleri (`DemoDataAndSecurityUnitTests`):** Tohumlama işleminin deterministikliği, tekrarlanabilirliği ve tüm demo kullanıcılarının `DEMO-*` ile `.invalid` kurallarına uyumu test edilir.
- **Bileşen Testleri (`DemoSecurityBannerComponentTests`):** Banner'ın bUnit ile DOM'a doğru ARIA etiketleri ve uyarı metinleriyle render edildiği kanıtlanır.
- **Entegrasyon Testleri (`DemoDataAndSecurityIntegrationTests`):** PostgreSQL üzerinde seed işleminin çift çalıştırmada hatasız tamamlandığı ve tohumlanan kullanıcıların (`ADM`, `DOC`, `PAT`) yetki sınırlarıyla API oturumu açabildiği doğrulanır.
