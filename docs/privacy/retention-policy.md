# Saklama, İmha ve Anonimleştirme Politikası

## Durum ve sınır

Bu politika eğitim ürününün teknik tasarımını yönlendirir; gerçek hastane için kesin hukuki süre veya uyumluluk görüşü değildir. Sağlık kaydı süreleri ve kanuni istisnalar gerçek kullanımdan önce hukuk, sağlık kayıtları ve güvenlik uzmanlarınca doğrulanmalıdır. Bu nedenle klinik kayıt süreleri kod içine sabitlenmez ve `TBD-LEGAL` olarak işaretlenir.

## İlkeler

- Veri yalnızca belirtilen amaç ve gerekli süre için tutulur.
- İşleme şartı/amaç ortadan kalktığında veri silinir, yok edilir veya geri döndürülemeyecek biçimde anonimleştirilir.
- Silme, yok etme ve anonimleştirme farklı işlemlerdir; maskeleme anonimleştirme değildir.
- İmha işlemi kim, ne zaman, hangi politika ve hangi sayıda kayıt üzerinde çalıştı bilgisiyle kanıt üretir; silinen klinik içeriği kanıta kopyalamaz.
- Backup, cache, projection, blob, export ve mock mesajları da retention kapsamındadır.
- Legal hold gerçek ürün senaryosunda saklamayı durdurabilir; bu projede yalnız tasarım hook'u olarak bulunur.
- İmzalı klinik kaydın günlük CRUD silmesi yasaktır; retention işi ayrı, ayrıcalıklı ve test edilen süreçtir.

## Politika tablosu

“Demo varsayılanı” yerel geliştirme/test temizliği içindir ve hukuki süre değildir.

| Anahtar | Kategori | Demo varsayılanı | Tetikleyici | Yöntem | Gerçek kullanım kararı |
|---|---|---:|---|---|---|
| RET-IDENTITY | Kullanıcı hesabı | Hesap + 90 gün | Hesap kapandı ve bağımlılıklar çözüldü | Kimlik alanlarını anonimleştir; güvenlik olaylarını ayır | TBD-LEGAL |
| RET-AUTH-SECRET | Token/reset/recovery metadata | Dakika–30 gün; türe göre | Süre doldu/kullanıldı/iptal | Kriptografik yok etme veya hard delete | Güvenlik politikası |
| RET-STAFF | Personel/atama | Demo proje ömrü | Profil demo reseti | Sentetik reset/anonimleştirme | TBD-LEGAL |
| RET-PATIENT-MASTER | Hasta ana kaydı | Demo proje ömrü | Demo reset veya doğrulanmış imha talebi | Bağımlılığa göre anonimleştirme | TBD-LEGAL |
| RET-CONTACT | İletişim/adres | Hesap + 30 gün | Amaç bitti/güncellendi | Eski değeri sil/anonimleştir | TBD-LEGAL |
| RET-APPOINTMENT | Randevu/check-in | Demo proje ömrü | Demo reset | Anonimleştirme veya ilişkili silme | TBD-LEGAL |
| RET-CLINICAL | Klinik kayıtlar | Demo proje ömrü | Yalnız politika işi | İlişkiyi koruyan anonimleştirme/yok etme | TBD-LEGAL; kodda sabitlenmez |
| RET-CLINICAL-FILE | Klinik dosya/görüntü | Demo proje ömrü | Üst kayıt politikası | Blob + metadata birlikte yok et | TBD-LEGAL |
| RET-DISPENSE | Teslim/izlenebilirlik | Demo proje ömrü | Politika işi | İlişkiyi koruyan anonimleştirme | TBD-LEGAL |
| RET-INVENTORY | Hasta dışı stok | Demo proje ömrü | Ürün/lot yaşamı ve reset | İş kaydını temizle | Kurum politikası |
| RET-SPECIMEN | Numune metadata | Demo proje ömrü | Politika işi | Barkod/kimlik ilişkisini anonimleştir | TBD-LEGAL |
| RET-AUDIT | Audit olayları | En az demo proje ömrü | Ayrı güvenlik politikası | İçerik minimizasyonu; süre sonunda kanıtlı imha | TBD-LEGAL; imha kayıtları için KVKK rehberinde en az 3 yıl bilgisi gerçek kullanımda doğrulanır |
| RET-TECH-LOG | Uygulama log/trace | 30 gün | Yaş | Rolling delete | Operasyon politikası |
| RET-NOTIFICATION | Mesaj ve teslim durumu | 30 gün | Teslim + süre | İçerik ve alıcı ref'i sil | Operasyon/TBD-LEGAL |
| RET-REPORTING | Projection | Kaynakla eşzamanlı | Yeniden oluşturma/reset | Drop/rebuild veya anonimleştir | Kaynak politika |
| RET-EXPORT | Export/geçici dosya | 24 saat | İndirme veya süre | Blob hard delete + link iptali | Güvenlik politikası |
| RET-INTEGRATION | Mock mesaj/dead letter | 30 gün | Tamamlandı + süre | Payload sil; kimliksiz sayaç kalabilir | Gerçek entegrasyonda TBD-LEGAL |
| RET-BACKUP | Demo backup | 30 gün | Backup yaşı | Güvenli yok etme/expire | Kurtarma hedefiyle doğrula |

## Teknik model

Retention değerleri tek bir politika kaynağında tutulur:

```text
PolicyKey, Version, DataCategory, Trigger, Duration,
Action, LegalBasisReference, EffectiveFrom, ApprovedBy
```

- Production-benzeri profil, `TBD-LEGAL` bir politika ile başlamayı reddeder veya açık demo modu dışında imha çalıştırmaz.
- Periyodik iş küçük, idempotent batch'lerle çalışır; checkpoint ve dry-run raporu üretir.
- Her kayıt için `RetentionPolicyKey`, gerekiyorsa `RetentionStartAtUtc` ve `LegalHold` değerlendirilir.
- Projection/cache kaynak politika sonucunu takip eder; yeniden üretilemeyen kopya kalmaz.
- Blob silme başarısızsa veritabanı kaydı “tamamlandı” gösterilmez; yeniden deneme gerekir.

## İmha kanıtı

Kanıt şunları içerir:

- İşlem kimliği, politika anahtarı/sürümü, UTC başlangıç/bitiş
- Taranan, atlanan, anonimleştirilen, silinen ve hatalı kayıt sayıları
- Çalıştıran servis/kullanıcı ve onay referansı
- İçerik taşımayan hedef kategori/batch hash'i
- Hata ve yeniden deneme durumu

Kanıt, silinen kişisel/klinik veriyi veya düz değer kimliği içermez.

## Anonimleştirme kuralları

- Demo hasta/personel doğrudan tanımlayıcıları rastgele geri döndürülemez değerlerle değiştirilir.
- Serbest metin klinik notu otomatik güvenilir anonim sayılmaz; eğitim ürününde bütün not silinir veya kayıt önceden sentetik tutulur.
- Nadir tarih/bölüm kombinasyonları genellenir; küçük grup raporları bastırılır.
- Aynı kişiyi yeniden bağlamak gerekmiyorsa deterministik hash kullanılmaz.
- Şifreleme veya takma adlandırma anonimleştirme olarak raporlanmaz.

## Yedek ve kurtarma

- Backup, kaynak verinin en yüksek sınıfını taşır ve erişimi daha gevşek olamaz.
- Silinen kayıt backup içinde restore edilirse retention işi tekrar uygulanır.
- Restore provası yalnız sentetik veriyle yapılır ve auditlenir.
- Anahtar yok etme kullanılıyorsa ilgili backup'ların etkisi belgelenir.

## Kaynaklar

- [KVKK — Kişisel Verilerin Silinmesi, Yok Edilmesi veya Anonim Hale Getirilmesi Hakkında Yönetmelik](https://www.kvkk.gov.tr/Icerik/5441/KISISEL-VERILERIN-SILINMESI-YOK-EDILMESI-VEYA-ANONIM-HALE-GETIRILMESI-HAKKINDA-YONETMELIK)
- [KVKK — Silme, Yok Etme veya Anonim Hale Getirme açıklaması](https://www.kvkk.gov.tr/Icerik/2038/kisisel-verilerin-silinmesi-yok-edilmesi-veya-anonim-hale-getirilmesi)
- [T.C. Sağlık Bakanlığı — Kişisel Sağlık Verileri Hakkında Yönetmelik](https://erisilebilir.saglik.gov.tr/TR-28791/kisisel-saglik-verileri-hakkinda-yonetmelik.html)

Kaynaklar ve kesin süreler gerçek kullanımdan önce güncel halleriyle yeniden doğrulanır.
