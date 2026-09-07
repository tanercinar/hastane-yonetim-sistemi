# ADR-0005: Klinik Kayıt Değişmezliği ve Düzeltme Modeli

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **İlgili:** `docs/product/state-machines.md`, `docs/security/threat-model.md`

## Bağlam

Klinik not, tanı, reçete, sonuç ve ilaç uygulaması sıradan CRUD verisi değildir. İmzalı/final kaydın sessizce üzerine yazılması, kimin ne zaman ne bildiğini ve hangi kararla işlem yaptığını kaybettirir. Her değişimi tam event sourcing ile kurmak ise bu eğitim ürünü için ağırdır.

## Karar

- Klinik içerik önce `Draft`, sonra yetkili kullanıcı sign/finalize ettiğinde değişmez sürüm olur.
- İmzalı/final sürüm doğrudan `UPDATE` ile değiştirilmez.
- Sonradan bilgi eklemek için **Addendum**, yanlış değeri düzeltmek için eski sürüme referans veren **Correction**, hiç gerçekleşmemiş/yanlış kişiye kayıt için **EnteredInError** kullanılır.
- Düzeltme yeni sürüm/record üretir; eski sürüm yetkili geçmiş ve audit için korunur.
- İptal ile hatalı giriş ayrıdır: iptal planlanan işlemin yapılmadığını, hatalı giriş kaydın gerçekliği yanlış temsil ettiğini belirtir.
- Her imza/final/düzeltme/iptal; aktör, UTC, gerekçe, önceki kayıt/sürüm, encounter ve optimistic concurrency değeri taşır.
- Audit olayı klinik içeriği kopyalamaz; hedef kimlik/sürüm ve eylemi kaydeder.
- Liste/zaman çizelgesi normalde güncel geçerli sürümü gösterir; yetkili kullanıcı geçmiş sürümleri görebilir.
- Veritabanı constraint ve uygulama kuralları final kaydın illegal geçişini engeller.
- Tam event sourcing kullanılmaz; kritik aggregate'lar açık revision tabloları/immutable child records ile modellenir.

## Kayıt türüne göre davranış

| Kayıt | Draft | Final/İmza | Sonraki değişiklik |
|---|---|---|---|
| Klinik not | Yazar düzenler | İmzalı sürüm | Addendum veya Correction |
| Tanı | Geçici/ön tanı değişebilir | Encounter tamamlanınca sürüm | Durum değişimi/düzeltme kaydı |
| Reçete | Doktor taslağı | Signed | Gerekçeli cancel; yeni reçete/düzeltme |
| Laboratuvar/radyoloji sonucu | Preliminary | Final | Corrected Final veya EnteredInError |
| İlaç teslim/uygulama | İşlem öncesi plan | Gerçekleşen olay | Ters hareket/düzeltme; hard delete yok |
| Yatak hareketi | Plan/istek | Aktifleşen zaman aralığı | Yeni transfer/kapanış kaydı |

## Alternatifler

### CRUD + `UpdatedAt`

Reddedildi: eski klinik anlamı, gerekçeyi ve sorumluluğu korumaz.

### Soft delete her şeyde

Reddedildi: silme bayrağı düzeltme, iptal ve hatalı giriş arasındaki domain farkını ifade etmez.

### Tam event sourcing

Reddedildi: güçlü geçmiş sağlar ancak projection, event versioning ve geliştirici yükü ürün amacına göre fazladır.

### Sadece generic audit trail

Reddedildi: teknik değişiklik logu kullanıcıya/klinik sürece doğru düzeltme semantiği vermez ve içerik sızıntısı riski doğurur.

## Sonuçlar

### Olumlu

- Klinik geçmiş, aktör ve gerekçe korunur.
- Eşzamanlı eski güncelleme tespit edilir.
- Kullanıcıya düzeltme ve final durumları anlamlı sunulur.

### Olumsuz

- Sorgular güncel sürüm/geçmiş ayrımını yönetir.
- Depolama ve mapping daha karmaşıktır.
- Retention/anonimleştirme sürüm zincirini bütün olarak ele almalıdır.

## Uygulama korumaları

1. Her kritik aggregate için izin verilen geçiş unit testleri ve yasak geçiş negatif testleri yazılır.
2. Final kayda yönelik EF değişikliği guard/interceptor veya mapping ile engellenir; veritabanı constraint değerlendirilir.
3. API `rowVersion`/ETag benzeri concurrency kanıtı ister.
4. Correction nedeni boş olamaz; permission ve gerekiyorsa step-up doğrulama aranır.
5. Audit içeriği değil kayıt/sürüm referansını taşır.

## Yeniden değerlendirme koşulları

- Harici mevzuat/standart belirli imza veya kayıt formatı isterse
- Tam olay yeniden oynatma ihtiyacı doğarsa
- Elektronik imza/sertifika veya gerçek tıbbi cihaz kapsamı eklenirse
- Klinik kaydın yasal saklama/anonimleştirme kararı netleşirse
