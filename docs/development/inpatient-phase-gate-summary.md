# Faz 7: Yatan Hasta Yönetimi — Kalite ve Kabul Kapısı

## Genel Bakış

Bu belge `F07-G01`–`F07-G08` kapsamındaki yatış yaşam döngüsünün otomatik kalite kanıtını ve açık kalan manuel kabul kapısını özetler. Sistem eğitim/simülasyon ürünüdür; sertifikalı HBYS veya klinik karar desteği değildir ve yalnız sentetik `DEMO` veriyle doğrulanır.

## Kapsam

1. Servis, oda ve yatak durumları ile aktif yatak tekilliği.
2. Doktor yatış istemi, hedef bölüm/servis uyumu, kabul ve ilk yatak ataması.
3. Kaynak ve hedef servis ekipleri arasında kapsam kontrollü iç transfer; eski yatağın `Cleaning`, yeni yatağın `Occupied` olması ve yatış bölümünün güncellenmesi.
4. Minimum gerekli veriyi gösteren, bölüm/bakım ilişkisine göre kapsamlanan klinik servis panosu.
5. Değiştirilemez hemşire gözlemi ve düzeltme akışı; bakım planı/görevleri ve türetilmiş gecikme görünümü.
6. Aktif imzalı reçeteye bağlı eMAR planlama, 5 Doğru onayı ve terminal doz durumları.
7. Zorunlu epikrizli taburculuk, yatağın temizliğe alınması ve açıkça `MOCK` kurum dışı sevk.
8. Permission tabanlı SignalR üyeliği, kimliksiz yenileme sinyali ve yeniden bağlanınca kapsamlanmış REST verisi çeken doluluk dashboard'u.

## İnceleme Sonrası Güvenlik ve Bütünlük Korumaları

- Tüm Faz 7 okuma/yazma uç noktaları API seviyesinde permission ve kaynak kapsamı uygular; yatış erişimi hasta sahipliği, sorumlu hekim, aktif bakım ilişkisi veya aktif bölüm atamasıyla sınırlandırılır.
- Sistem yöneticisi klinik Faz 7 uç noktalarına ve SignalR grubuna erişemez; arayüz menüsü permission kataloğuna göre gizlenir.
- Kabul eden personel kimliği kalıcı bakım ilişkisi oluşturmaz. Hedef ekip erişimi aktif hedef bölüm ataması üzerinden hesaplanır.
- Aktif yatış, aktif yatak ataması ve aktif transfer PostgreSQL kısmi unique index'leriyle korunur. Yatış, yatak, transfer, bakım görevi, eMAR ve taburculuk yarışlarının kaybedeni güvenli `409 Conflict` alır.
- Audit ayrıntıları klinik serbest metin, hasta içeriği, token veya secret taşımaz; yalnız operasyonel kimlik/durum/zaman kanıtı içerir.
- Gecikmiş bakım görevleri `GET` sırasında veritabanında sessizce değiştirilmez; `Overdue` görünümü sorgu zamanında türetilir.

## Otomatik Kanıt Senaryoları

### Gerçek PostgreSQL entegrasyon

- Aynı hastaya iki eşzamanlı aktif yatış isteğinde tam bir başarı ve bir `409`.
- Aynı yatağa iki eşzamanlı atamada tam bir başarı ve bir `409`.
- Bölüm/servis ve servis/yatak uyumsuzluğunun reddi; başka bölüme atanmış sorumlu doktorun mass-assignment ile eklenememesi.
- Kaynak ekibin transfer açması, hedef ekibin görüp kabul/tamamlama yapması; kaynak erişiminin devir sonrası sona ermesi.
- Bakım görevi ve eMAR uygulamasında iki istemcili yarış; tek terminal klinik sonuç.
- Aktif reçete olmadan veya başka içerikle eMAR planlamasının reddi.
- İki eşzamanlı taburculukta tek taburculuk/yatak geçişi.
- Bölüm dışı kullanıcı ve sistem yöneticisi için `403`; audit canary değerlerinin saklanmaması.

### Gerçek tarayıcı E2E

`Phase7ProductGateEndToEndTests.cs`, Chromium ve izole PostgreSQL üzerinde hemşire ile vital kaydı ve aktif reçeteden eMAR planlama/5 Doğru uygulamasını, ardından doktor ile epikrizli taburculuğu gerçek Blazor arayüzünden tamamlar. Tarayıcı sayfa hatası, yatak/yatış/eMAR bütünlüğü ve audit veri minimizasyonu birlikte doğrulanır.

## Son Doğrulama Metrikleri

- Release build: 0 uyarı, 0 hata.
- Unit: 262/262.
- Component: 76/76.
- Architecture: 13/13.
- Gerçek PostgreSQL integration: 111/111.
- Playwright E2E: 7/7.
- Toplam: 469/469, skipped 0.

Manuel tarayıcı kabulü otomatik testten ayrıdır ve [`F07_Test.md`](../../F07_Test.md) tamamlanana kadar `F07-KAPI` açık kalır.

## Kapı Kararı

Otomatik inceleme ve regresyon paketi başarılı olsa bile Faz 7 kapısı manuel kullanılabilirlik, responsive görünüm, klavye/odak, iki pencere SignalR gözlemi ve hata/forbidden durumları doğrulanmadan kapatılmaz. Manuel sonuçlar `F07_Test.md` şablonuna kaydedilip `ROADMAP.md` ilerleme günlüğüne eklenmelidir.
