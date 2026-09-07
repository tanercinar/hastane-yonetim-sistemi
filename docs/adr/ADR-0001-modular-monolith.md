# ADR-0001: Modüler Monolit ve Modül Sınırları

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **Karar sahipleri:** Proje sahibi
- **İlgili:** `ROADMAP.md`, `docs/product/scope.md`

## Bağlam

Ürün çok sayıda klinik alt alan içeriyor, fakat tek geliştirici ve eğitim/portföy amacıyla geliştirilecek. Mikroservisler ayrı deployment, ağ güvenliği, mesajlaşma, gözlemlenebilirlik, veri tutarlılığı ve yerel geliştirme yükünü erken aşamada gereksiz artırır. Katmansız tek monolit ise modüllerin veritabanı ve iş kurallarına birbirinden kontrolsüz erişmesine yol açar.

## Karar

ASP.NET Core içinde tek dağıtım birimine sahip **modüler monolit** kullanılacaktır.

- Modüller: IdentityAccess, Organization, Patients, Scheduling, ClinicalRecords, Pharmacy, Diagnostics, Inpatient, Emergency, SurgeryCriticalCare, SpecialtyCare, Inventory, Notifications, Reporting, Interoperability ve AuditPrivacy.
- Her modül kendi `Domain`, `Application`, `Infrastructure`, `Endpoints` ve `Contracts` alanlarını kapsüller.
- İlk aşamada her katman için ayrı proje çoğaltılmaz; modül assembly'si ve namespace/folder sınırları kullanılır.
- Bir modül başka modülün tablolarına, `DbContext` ayrıntılarına, domain entity'sine veya Infrastructure alanına doğrudan erişmez.
- Modüller açık uygulama servisi/contract veya uygulama içi olay üzerinden haberleşir.
- Ortak `BuildingBlocks` yalnız az, kararlı ve gerçek anlamda ortak primitive'leri içerir; “misc/common” çöplüğü olamaz.
- İşlem tutarlılığı aynı süreçte yerel transaction ile sağlanabilir. Asenkron yan etkiler için güvenilir outbox benzeri desen daha sonra kullanılır.
- Mimari bağımlılık testleri sınırları CI'da uygular.

## Alternatifler

### Mikroservis

Reddedildi: bağımsız ölçek/deployment ihtiyacı ölçülmedi; tek geliştirici için dağıtık transaction, ağ hata modu ve operasyon yükü faydasından büyüktür.

### Katmanlı tek monolit

Reddedildi: tüm domain entity ve repository'lerin ortak katmanlarda toplanması klinik alan sınırlarını zamanla siler.

### Her modül ve katman için ayrı proje

Şimdilik reddedildi: onlarca proje navigation/build karmaşıklığı yaratır. Assembly sınırı yetersiz kalırsa ölçülü biçimde ayrılabilir.

## Sonuçlar

### Olumlu

- Tek komutla geliştirme, test ve deployment.
- Modül içi transaction ve hata ayıklama basitliği.
- Alan sınırları korunurken tek geliştirici hızının kaybolmaması.
- Gerekirse belirli modülü ileride ayırmak için açık sözleşme zemini.

### Olumsuz

- Tüm uygulama birlikte deploy edilir ve aynı süreç arızasından etkilenir.
- Sınırlar yalnız disiplin ve testlerle korunmazsa erozyona uğrayabilir.
- Modüller aynı veritabanı sunucusunu paylaşır; yine de ayrı şema/sahiplik uygulanmalıdır.

## Uygulama korumaları

1. Yasak project/namespace bağımlılıkları architecture test ile kırılır.
2. Cross-module database join/application query yalnız açık Reporting projection'ında yapılır.
3. Genel repository ve tüm entity'leri bilen `ApplicationDbContext` API'si yayınlanmaz.
4. Yeni modül önce glossary, ownership, contract ve veri envanteriyle tanımlanır.
5. Redis, broker veya servis ayrımı yeni ADR ve ölçüm ister.

## Yeniden değerlendirme koşulları

- Bir modülün bağımsız ölçek/deployment gereksinimi ölçülürse
- Takımlar modül başına bağımsız sahipliğe ayrılırsa
- Yasal/veri yerleşimi sınırı ayrı deployment gerektirirse
- Tek süreçte kabul edilemez izolasyon veya yayın riski kanıtlanırsa
