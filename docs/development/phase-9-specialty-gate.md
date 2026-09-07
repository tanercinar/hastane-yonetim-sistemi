# Faz 9 — Uzmanlık Dikey Dilimleri Kapı İnceleme Raporu

## 1. Güncel karar

`F09-KAPI` **kapalıdır**. 1 Eylül 2026 yeniden incelemesinde ortak model bağları, hasta portalı yayın politikası, ayrı yenidoğan kimliği, evde sağlık Encounter doğrulaması ve güvenlik düzeltmeleri tamamlandı. Faz 9'a özel gerçek Chromium Playwright testi, erişilebilir `403 Forbidden` component testi ve yerel tarayıcı responsive kabulü başarıyla çalıştırıldı.

Kapı kanıtı aşağıdaki davranışları birlikte doğrular:

- gebelik açılışı ve antenatal ziyaretler kanonik `Encounter -> Appointment` zincirini kullanır;
- yenidoğan, anneden farklı ve başka doğumda kullanılmamış aktif ortak `Patient` kimliği taşır;
- evde sağlık tamamlaması aynı hastaya ait, uygun tür ve durumdaki tekil Encounter'a bağlanır;
- hasta portalı kimliği oturumdan çözer ve yalnız yayınlanabilir, minimize edilmiş alanları döndürür;
- API yetkisi `Permission + Resource Scope + Care Relationship/Assigned Team` ile uygulanır;
- teknik yöneticiye hasta portalında boş liste yerine erişilebilir ve ayrıntı sızdırmayan yasaklı durum gösterilir.

## 2. İncelemede giderilen bulgular

- Tüm Faz 9 GET/POST sınırlarına `Permission + gerçek Patient + Care Relationship/Assigned Team` denetimi eklendi; tahmin edilebilir GUID ile IDOR kapatıldı.
- Evde sağlık açık adresi ve telefonu yalnız tam olarak atanmış personele döndürülüyor; talep eden hekim dâhil diğer kullanıcılarda alanlar maskeleniyor.
- Ziyareti başlatma ve tamamlama yalnız atanmış personele açıldı.
- Operasyonel rapor `report.operations.view` iznine bağlandı; teknik `SystemAdministrator` için `403`, `HospitalManager` için kimliksiz KPI erişimi doğrulandı.
- Geçersiz enum/flag değerlerinin sessiz varsayılana dönüşmesi kaldırıldı; API `400 Validation Problem` dönüyor.
- Audit olaylarından obstetrik, doğum, diş, evde sağlık notları ve ölçümleri çıkarıldı; teknik eylem/hedef bilgisi korunuyor.
- Aynı hastaya iki aktif gebelik ve aynı diş/sürümün iki kez yazılması PostgreSQL benzersiz indeksleriyle engellendi.
- Yerel Development DEMO'sunda yalnız belgelenmiş sentetik hekim/hemşire-hasta bakım ilişkisi kuruluyor; gerçek veri veya dış entegrasyon kullanılmıyor.

## 3. Otomatik kanıt kapsamı

Hedefli PostgreSQL Testcontainers paketi şunları kapsar:

- üç uzmanlık mutlu akışı ve yaşam döngüsü negatifleri;
- anonim `401`, teknik yönetici `403`, bakım ilişkisi olmayan ikinci hasta `403`;
- geçersiz klinik enum için `400` ve kayıt oluşmaması;
- audit canary değerinin `Reason`/`DetailsJson` alanlarına sızmaması;
- atanmış evde sağlık personeli dışındaki kullanıcılara adres maskesi;
- aktif gebelik ve diş sürümü veritabanı yarış kısıtları;
- HospitalManager rapor erişimi ile SystemAdministrator reddi.

`Phase9ProductGateEndToEndTests` gerçek PostgreSQL, Kestrel HTTPS ve Chromium kullanarak:

- hekim için gebelik, doğum ve diş kayıtlarını;
- hemşire için atanmış evde sağlık ziyaretini ve açık adres/telefon sınırını;
- hekim için aynı evde sağlık alanlarının maskelenmesini;
- HospitalManager için kimliksiz operasyonel raporu;
- hasta için mobil uzmanlık zaman çizelgesini ve yayın politikasını;
- SystemAdministrator için erişilebilir `403 Forbidden` görünümünü;
- konsol hatası, URL/console canary sızıntısı, klavye odağı ve yatay taşma kontrollerini doğrular.

Tam koşu sonuçları ve komutları `ROADMAP.md` ilerleme günlüğünde tutulur.

## 4. Etkileşimli tarayıcı kabulü

Yerel Development ortamı tüm kayıtlı modül migration'larıyla hazırlanıp uygulama HTTPS üzerinde açıldı. Hasta hesabıyla uzmanlık portalı `390x844`, `1024x768` ve `1440x900` boyutlarında kontrol edildi; üç görünümde de yatay taşma ve browser console hatası görülmedi. Boş veri durumunda güvenli yayın açıklaması ile ayrı empty-state gösterildi. Çok rollü ve dolu veri görünümü tekrarlanabilir Playwright kapı testiyle kanıtlandı.

## 5. Kapatma koşulu

Ortak model bağları ve hasta portalı politikası tamamlandı; gerçek F9 Playwright senaryosu, component forbidden durumu ve [`F09_Test.md`](../../F09_Test.md) kabul matrisi `PASS` oldu. Faz 9 kapısı kapatılmıştır.
