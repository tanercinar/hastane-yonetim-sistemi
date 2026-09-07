# Veri Sınıflandırma Standardı

## Amaç

Tüm proje verileri aşağıdaki seviyelerden biriyle sınıflandırılır. En yüksek bileşen sınıfı tüm DTO, dosya, event, log veya export'un sınıfını belirler. Proje yalnız sentetik veri kullanmasına rağmen kontroller gerçekçi tasarlanır.

## Seviyeler

| Seviye | Ad | Örnekler | Asgari kontrol |
|---|---|---|---|
| C0 | Genel | README, herkese açık demo açıklaması, lisans | Bütünlük, kaynak kontrolü |
| C1 | İç kullanım | Teknik konfigürasyon şeması, kimliksiz aggregate metrik | Yetkili kullanıcı, değişiklik kontrolü |
| C2 | Kişisel | Demo ad/iletişim/adres, personel profili, IP/cihaz bilgisi | Amaç/kapsam kontrolü, aktarım minimizasyonu, at-rest/in-transit koruma |
| C3 | Özel nitelikli/klinik | Tanı, reçete, sonuç, vital, klinik not, görüntü, genetik/biyometrik demo veri | C2 + alan şifreleme değerlendirmesi, sıkı kaynak yetkisi, MFA/yüksek risk kontrolü, ayrıntılı audit |
| C4 | Güvenlik sırrı | Parola hash'i, MFA secret, token, encryption key, production credential | Repodan/veritabanından ayrı secret store, log/telemetry yasağı, rotation, en dar erişim |

## Ek etiketler

- `DEMO_SYNTHETIC`: Gerçek kişiden türetilmemiş kurgusal veri.
- `DIRECT_IDENTIFIER`: İsim, kullanıcı adı, iletişim, adres gibi doğrudan tanımlayıcı.
- `QUASI_IDENTIFIER`: Tarih, bölüm, nadir olay gibi birleşince tanımlayabilecek alan.
- `CLINICAL_CONTENT`: Sağlık hizmeti içeriği.
- `SECURITY_EVENT`: Kimlik/yetki/audit olayı.
- `PUBLIC_EXPORTABLE`: Yalnız ürün sahibi tarafından açıkça onaylanan C0 içerik.

`DEMO_SYNTHETIC` etiketi C2/C3 kontrollerini kaldırmaz; mimari ve test gerçekçi kalır.

## Ortama göre kurallar

| Ortam | C2/C3 | C4 |
|---|---|---|
| Kaynak kodu | Yalnız açık `DEMO-*` fixture; gerçekçi ama gerçek kişiye bağlı olmayan veri | Yasak |
| Veritabanı | Yetki, şifreli aktarım, yedek kontrolü, alan şifreleme değerlendirmesi | Parola hash'i hariç secret tutulmaz; anahtar ayrı |
| Uygulama logu | Kimliksiz teknik ID/correlation; klinik içerik ve token yasak | Yasak |
| Audit | İçerik kopyalamadan aktör/hedef/eylem/sonuç | Secret yasak |
| URL/query | Tahmin edilmesi zor teknik ID gerekebilir; ad/tanı/sonuç yasak | Yasak |
| Telemetry/trace | Redacted ID ve süre; request/response body varsayılan kapalı | Yasak |
| E-posta/SMS/mock bildirim | Minimum randevu/işlem bildirimi; tanı/sonuç varsayılan yasak | Yasak |
| Export | Permission, amaç, satır sınırı, audit, süreli saklama, CSV injection koruması | Yasak |
| Ekran | Role/scope göre maskeleme, shoulder-surfing azaltımı | Kullanıcıya hiçbir zaman düz gösterilmez |

## Kod ve tasarım kuralları

- Yeni entity/DTO alanı veri envanterine sınıf ve amaçla eklenmeden tamamlanmış sayılmaz.
- Entity doğrudan API response veya log payload olarak serialize edilmez.
- Hassas veriler exception mesajına, metric label'ına veya span attribute'a yazılmaz.
- Kişisel veriyi anahtar/cache adı/SignalR grup adı içinde çıplak kullanma.
- Arama sonuçları veri minimizasyonu ve sayfalama uygular; geniş wildcard/toplu enumeration sınırlandırılır.
- Clipboard, browser cache, autocomplete ve local storage kullanımı C2/C3 için ayrıca değerlendirilir.
- Şifreleme anahtarı ile şifreli veri aynı veritabanı/konfigürasyonda tutulmaz.
- Şifreli alanlarda arama gerekiyorsa düz metin kopyası oluşturulmaz; tasarım ADR ve risk incelemesi ister.

## Sentetik veri standardı

- Kimlikler `DEMO-PAT-*`, `DEMO-STAFF-*` gibi açık prefix taşır.
- E-posta için ayrılmış `example.test`/geçersiz alanlar, telefon için gerçek kişiye yönlenmeyen belirgin demo biçimleri kullanılır.
- T.C. kimlik numarası üretilmez ve algoritmik olarak geçerli kimlik taklit edilmez.
- Adresler gerçek konuta karşılık gelmez; “Demo Mahallesi/Test Caddesi” gibi belirgin kurgudur.
- Klinik hikâyeler kurgusal, kısa ve eğitim amaçlıdır; gerçek vaka metni kopyalanmaz.
- Görseller/dosyalar lisanslı açık örnek veya projede üretilmiş demo içeriktir; metadata temizlenir.

## Kaynaklar

- [KVKK — Özel Nitelikli Kişisel Verilerin İşlenmesine İlişkin Rehber](https://www.kvkk.gov.tr/Icerik/8183/Ozel-Nitelikli-Kisisel-Verilerin-Islenmesine-Iliskin-Rehber)
- [T.C. Sağlık Bakanlığı — Kişisel Sağlık Verileri Hakkında Yönetmelik](https://erisilebilir.saglik.gov.tr/TR-28791/kisisel-saglik-verileri-hakkinda-yonetmelik.html)

Bu sınıflandırma hukuki danışmanlık veya uyumluluk sertifikası değildir.
