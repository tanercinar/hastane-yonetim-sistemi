# Ürün Kapsamı

## Belgenin amacı

Bu belge, Hastane Yönetim Sistemi'nin ürün sınırını ve teslim seviyelerini tanımlar. `ROADMAP.md` yürütme sırasının, bu belge ise ürün kapsamının tek doğruluk kaynağıdır. Ürün eğitim, staj ve portföy amacı taşır; gerçek hastane kullanımı, mevzuat sertifikasyonu veya klinik karar verme iddiası yoktur.

## Problem ve hedef

Hastane süreçleri çok sayıda rolün aynı hasta yolculuğu üzerinde kontrollü biçimde çalışmasını gerektirir. Proje, bu karmaşıklığı uçtan uca dikey dilimlerle gösterecek; yalnızca sentetik veri ve mock entegrasyon kullanarak güvenli yazılım mimarisi, yetkilendirme, klinik kayıt bütünlüğü ve test disiplinini sergileyecektir.

Başlıca hedefler:

- Hastanın randevu almasından muayene, tanı, reçete ve eczane teslimine kadar çalışan ilk ürün oluşturmak.
- Laboratuvar, radyoloji, yatış, acil, ameliyat, yoğun bakım ve uzmanlık süreçlerini aynı klinik çekirdek üzerinde göstermek.
- Yetkiyi rolün yanında hasta ilişkisi, atama ve bölüm kapsamıyla sınırlamak.
- Klinik kayıtların imza, düzeltme, iptal ve denetim geçmişini korumak.
- Web uygulamasından sonra aynı API ve uygun ortak UI bileşenleriyle Windows ve Android istemcileri sunmak.
- Başka bir geliştiricinin yalnızca dokümantasyonla kurup test edebileceği bir portföy ürünü teslim etmek.

## Kullanıcılar

| Kullanıcı | Ürün içindeki temel amaç |
|---|---|
| Hasta | Kendi profilini, randevularını, reçetelerini ve yayımlanmış sonuçlarını yönetmek/görmek |
| Doktor | Yetkili hastayı değerlendirmek; tanı, istem, reçete, konsültasyon ve klinik not oluşturmak |
| Hemşire | Atandığı hastalarda ön değerlendirme, vital, bakım ve ilaç uygulama kaydı yürütmek |
| Başhekim | Klinik gözetim, gerekçeli kayıt yeniden açma ve bölüm kapsamlı operasyon görünürlüğü sağlamak |
| Kayıt/Danışma personeli | Hasta kaydı, randevu, check-in ve sıra işlemlerini yürütmek |
| Laboratuvar personeli | Numune zincirini ve teknik laboratuvar sonuç akışını yürütmek |
| Radyoloji personeli | Radyoloji iş listesi, çekim ve rapor sürecini yürütmek |
| Eczacı | Geçerli reçeteleri ve gerekli hasta güvenliği bilgisini görüp teslim kaydı oluşturmak |
| Sistem yöneticisi | Kullanıcı, rol, izin ve organizasyon atamalarını yönetmek; klinik içerik görmemek |
| Hastane yöneticisi | Kimliksizleştirilmiş veya minimum verili operasyonel raporları görmek |
| Muhasebe personeli | Gelecek kapsam için rol kataloğunda bulunmak; bu sürümde işlev kullanmamak |
| İnsan kaynakları personeli | Gelecek kapsam için rol kataloğunda bulunmak; bu sürümde işlev kullanmamak |

## Teslim seviyeleri

### İlk çalışan dikey dilim

- Güvenli giriş ve rol/izin altyapısı
- Hasta ana kaydı
- Doktor takvimi ve randevu alma
- Kayıt/check-in ve sıra
- Temel gerçek zamanlı bildirim

### İlk portföy MVP

İlk dikey dilime ek olarak:

- Poliklinik karşılaşması
- Alerji, problem, vital, klinik not ve tanı
- Reçete ve kural tabanlı demo güvenlik uyarıları
- Eczane iş listesi, ilaç teslimi ve klinik stok
- Hasta randevu, reçete ve sınırlı klinik zaman çizelgesi görünümü

### Kapsamlı klinik web ürünü

MVP'ye ek olarak:

- Laboratuvar, numune/barkod, kritik sonuç bildirimi
- Radyoloji ve PACS/DICOM simülasyonu
- Patoloji ve kan bankası dikey dilimleri
- Yatış, servis, yatak, transfer, hemşirelik ve taburculuk
- Acil, ameliyathane ve yoğun bakım
- Gebelik/doğum, diş ve evde sağlık dikey dilimleri
- FHIR, HL7, DICOM, MHRS, e-Nabız, MEDULA, İTS/ÜTS mock sınırları
- Gerçek zamanlı operasyon raporları

### Çoklu istemci ve portföy teslimi

- Windows personel istemcisi
- Android hasta istemcisi
- Tam regresyon, erişilebilirlik, performans ve güvenlik sertleştirmesi
- GitHub README, teknik dokümantasyon, kullanıcı rehberi ve demo senaryosu

## Fonksiyonel kapsam

| Alan | Kapsam | Teslim seviyesi |
|---|---|---|
| Kimlik ve organizasyon | Kullanıcı yaşam döngüsü, MFA, roller, izinler, hastane/bölüm/atama | Temel |
| Hasta yönetimi | Demografi, iletişim, acil kişi, güvenli arama ve kendi profilini görme | İlk dilim |
| Randevu | Uygunluk, slot, alma, iptal, yeniden planlama, check-in, sıra | İlk dilim |
| Klinik kayıt | Encounter, vital, alerji, problem, not, tanı, konsültasyon, ek | MVP |
| Eczane | Reçete, güvenlik uyarısı, iş listesi, teslim, ilaç/sarf stoğu | MVP |
| Tanısal hizmetler | Laboratuvar, radyoloji, patoloji, kan bankası | Kapsamlı web |
| Yataklı servis | Yatış, yatak, transfer, hemşirelik, eMAR, taburculuk/sevk | Kapsamlı web |
| Kritik alanlar | Acil, triyaj, ameliyat ve yoğun bakım simülasyonu | Kapsamlı web |
| Uzmanlık | Gebelik/doğum, diş, evde sağlık dikey dilimleri | Kapsamlı web |
| Entegrasyon | Yalnızca standart/kurum mock adaptörleri | Kapsamlı web |
| Raporlama | Yetki kapsamlı gerçek zamanlı operasyon panoları | Kapsamlı web |
| Native istemci | Windows ve Android; çevrimiçi çalışma | Çoklu istemci |

## Kapsam dışı

- Gerçek kişi veya gerçek hasta verisi
- Gerçek hastane ortamında üretim kullanımı
- Tıbbi cihaz veya klinik karar destek sistemi iddiası
- Uygulamaya gömülü yapay zekâ/LLM/ajan
- Otonom triyaj, tanı, tedavi veya ilaç kararı
- Gerçek MHRS, e-Nabız, MEDULA, SGK, İTS, ÜTS, PACS veya cihaz bağlantısı
- Faturalandırma, muhasebe, ödeme, sigorta ve gerçek provizyon
- Satın alma, tedarikçi, bordro, tam İK ve demirbaş
- İnternetsiz/çevrimdışı çalışma ve çevrimdışı yazma kuyruğu
- iOS/macOS istemcisi ve genel tüketici SaaS/çoklu tenant ürünleştirmesi
- “Tam FHIR/HL7/DICOM uyumlu ürün” veya mevzuat sertifikasyonu iddiası

## Ürün varsayımları ve kısıtları

- Tek geliştirici, yapay zekâ geliştirme araçlarıyla ilerler.
- Tarih/bütçe taahhüdü yerine faz kapıları kullanılır.
- Varsayılan dil Türkçe, saklama zamanı UTC, gösterim saat dilimi `Europe/Istanbul` olur.
- Sistem çevrimiçi çalışır; sunucu/veritabanı erişilemezse doğrulanmamış işlem başarılı gösterilmez.
- Tüm demo verileri deterministik, kurgusal ve `DEMO` işaretlidir.
- Mikroservis, broker, Redis veya arama motoru ancak ölçülmüş ihtiyaç ve ADR ile eklenebilir.

## Ürün düzeyi kabul ölçütleri

1. `ROADMAP.md` içindeki 12 başarı ölçütünün tamamı test kanıtına bağlanmıştır.
2. Başka hastanın veya bölümün verisine ID değiştirerek erişilemez.
3. İmzalı klinik kayıt sessizce değiştirilemez; düzeltme ve iptal gerekçeli iz bırakır.
4. Kritik yazmalarda randevu, yatak ve stok yarış koşulları veri bütünlüğünü bozmaz.
5. Gerçek zamanlı ekran yeniden bağlandıktan sonra sunucudaki doğru durumu gösterir.
6. Gerçek veri, gerçek dış sistem credential'ı ve mevzuat uyumu/klinik doğruluk hakkında yanıltıcı iddia bulunmaz.
7. Temiz checkout kurulum, migration, seed, test ve demo akışı dokümantasyonla tekrarlanabilir.

## Değişiklik yönetimi

Kapsama yeni bir klinik veya idari alan eklemek, gerçek entegrasyona geçmek, gerçek veri kullanmak ya da klinik karar desteği eklemek ürün risk sınıfını değiştirir. Böyle bir değişiklik önce ürün kararı ve ADR ile ele alınır; mevcut tamamlanmış görevler sessizce genişletilmez.
