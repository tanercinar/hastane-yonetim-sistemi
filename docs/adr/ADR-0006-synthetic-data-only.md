# ADR-0006: Yalnız Sentetik Veri ve Açık Simülasyon

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **İlgili:** `docs/privacy/data-classification.md`, `docs/privacy/data-inventory.md`

## Bağlam

Proje eğitim, staj ve portföy amacı taşır; gerçek hastane sözleşmesi, güvenli üretim ortamı veya gerçek veri işleme yetkisi yoktur. Açık veri setleri dahi yeniden tanımlama, lisans ve gerçek vaka içeriği riski taşıyabilir. Bununla birlikte veri modeli ve güvenlik kontrollerinin gerçekçi olması istenir.

## Karar

- Development, test, demo, ekran görüntüsü, doküman, log örneği ve CI'da yalnız **gerçek kişiden türetilmemiş sentetik veri** kullanılacaktır.
- Kimlikler `DEMO-*` prefix taşır; T.C. kimlik numarası üretilmez, geçerli telefon/e-posta/adres kullanılmaz.
- Arayüzde kalıcı “Eğitim/Simülasyon — Gerçek veri girmeyin” göstergesi bulunur.
- Uygulama startup'ında demo modu ve veri işareti doğrulanır; açık demo dışı profile geçiş ayrı karar gerektirir.
- Fixture/seed deterministik ve tekrarlanabilirdir; kullanıcı tarafından gerçek veri girişi konusunda uyarı/validation uygulanır.
- Klinik not, tanı sonucu ve görüntüler kurgusal veya lisansı doğrulanmış ve metadata'sı temiz demo asset'leridir.
- Mock entegrasyon payload'ları da yalnız sentetiktir; gerçek kurum endpoint'i/credential'ı yoktur.
- Sentetik veri C2/C3 güvenlik tasarımını gevşetmez: aynı yetki, audit, şifreleme değerlendirmesi ve retention kontrolleri uygulanır.
- Gerçek veriye benzeyen fixture ve secret için otomatik repo taraması Faz 1/13'e eklenir.

## Alternatifler

### Anonimleştirilmiş gerçek sağlık verisi

Reddedildi: anonimleştirmenin geri döndürülemezliği ve hukuki dayanağı uzmanlık gerektirir; portföy için gereksiz risktir.

### Herkese açık sağlık veri seti

Varsayılan olarak reddedildi: lisans, hassasiyet ve yeniden tanımlama koşulları veri setine göre değişir. Yalnız tamamen sentetik/açık lisanslı asset ayrı incelemeyle kullanılabilir.

### Rastgele faker verisi, kontrolsüz

Reddedildi: tesadüfen geçerli telefon/kimlik/adres veya tutarsız klinik senaryo üretebilir. Proje-specific güvenli generator gerekir.

## Sonuçlar

### Olumlu

- Gizlilik, etik ve portföy paylaşım riski büyük ölçüde azalır.
- Testler deterministik ve temizlenebilir olur.
- Ekran görüntüsü/demo güvenle paylaşılabilir.

### Olumsuz

- Gerçek veri dağılımı ve edge case'ler tam temsil edilmez.
- Performans ve arama kalitesi sentetik generator'ın kalitesine bağlıdır.
- Gerçek kullanım uyumluluğu kanıtlanmış olmaz.

## Uygulama korumaları

1. Seed generator reserved domain ve açık demo adlandırma kullanır.
2. Repo taraması geçerli kimlik/telefon/secret pattern'lerini ve büyük bilinmeyen veri dosyalarını inceler.
3. Import endpoint'leri mock ve demo doğrulamasından geçer; gerçek kaynak kabul etmez.
4. README ve tüm demo ekranları sertifikalı HBYS olmadığını belirtir.
5. Hata raporuna database dump veya kullanıcı girilmiş sağlık verisi eklenmez.

## Yeniden değerlendirme koşulları

- Gerçek hastane pilotu veya gerçek veri talebi gelirse
- Açık veri seti/asset eklenmek istenirse (lisans ve veri koruma incelemesi)
- Uygulama production-benzeri ortamda kullanıma sunulacaksa

Bu koşullardan biri oluşursa geliştirme durur; veri koruma etki analizi, hukuk/klinik uzman incelemesi, altyapı ve güvenlik planı olmadan gerçek veri kabul edilmez.
