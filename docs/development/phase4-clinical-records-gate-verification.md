# Faz 4 Klinik Kayıtlar Kapı Doğrulaması (F04-KAPI)

Bu belge Faz 4 klinik kayıt kapısının güncel kanıtlarını ve henüz tamamlanmamış manuel doğrulamayı kaydeder. Otomatik kapı 2026-08-29 tarihinde geçmiştir; `F04-KAPI`, manuel akış tamamlanana kadar açık tutulur.

## 1. Güvenlik ve veri bütünlüğü kapsamı

- Klinik erişim API seviyesinde `Permission + Resource Scope + Care Relationship` birleşimiyle uygulanır. Rol adı tek başına erişim sağlamaz; sistem yöneticisinin klinik IDOR denemesi reddedilir.
- DEMO hekim, hemşire ve başhekim için idempotent organizasyon profili/bölüm ataması seed edilir. Scheduling, UI ve klinik kayıtlar aynı kanonik Kardiyoloji bölüm kimliğini kullanır.
- Karşılaşma, alerji/problem, vital bulgu, klinik not, tanı, konsültasyon ve ek mutasyonlarında `ExpectedVersion` ile iyimser eşzamanlılık uygulanır; eski yazmalar `409 Conflict` olur.
- İmzalı not ve kesin tanı sessizce değiştirilemez. Düzeltme, ek not veya `EnteredInError` akışı kullanılır.
- Yeniden açma yalnız bölüm kapsamındaki başhekim, `ClinicalNoteReopen` izni ve son 5 dakikadaki MFA doğrulamasıyla yapılabilir.
- Klinik eklerde 15 MB sınırı, MIME/magic-byte eşleşmesi, DICOM Part 10 işareti, rastgele blob anahtarı ve açıkça `MOCK` kötü amaçlı dosya tarayıcısı bulunur.
- Hasta zaman çizelgesi taslak/hatalı girişleri yetkiye göre süzer ve serbest klinik metni özet/audit kayıtlarına taşımaz.
- Konsültasyon durum değişimleri idempotent, klinik içerik taşımayan bildirimler üretir.

## 2. Otomatik kanıtlar

| Paket | Sonuç | Kanıtlanan başlıca davranış |
| :--- | :--- | :--- |
| Faz 4 PostgreSQL entegrasyonu | 21/21 geçti | Çok disiplinli akış, kaynak kapsamı, hasta görünürlüğü, stale-write `409`, admin IDOR `403`, audit bütünlüğü |
| Tam PostgreSQL entegrasyonu | 75/75 geçti | Önceki fazlarla regresyon bulunmadı |
| Birim testleri | 125/125 geçti | Domain kuralları, erişim kararları, ek güvenlik doğrulayıcıları |
| Bileşen testleri | 22/22 geçti | Web bileşeni durumları ve kullanıcı akışları |
| Mimari testleri | 13/13 geçti | Modül/katman sınırları |
| Mevcut Playwright E2E | 3/3 geçti | Faz 1–3 tarayıcı akışları; Faz 4 için ayrı tarayıcı testi değildir |

Tam çözüm otomatik toplamı **238/238** testtir. Release build 0 uyarı/0 hata ile tamamlanmıştır. Biçimlendirme, yerel Markdown bağlantıları ve doğrudan/transitif NuGet zafiyet kapıları geçmiştir.

## 3. Faz 4 kapı senaryosu

`ClinicalPhase4GateTests` aşağıdaki akışı gerçek PostgreSQL Testcontainers üzerinde yürütür:

1. Hekim karşılaşmayı oluşturur ve başlatır; hemşire katılımcı olarak eklenir.
2. Hemşire vital panelini, hekim alerji/problem, ICD-10 tanı, imzalı SOAP notu, güvenli PDF ekini ve konsültasyon istemini kaydeder.
3. İmzalı notu doğrudan değiştirme ve eski sürümle yazma `409 Conflict` ile engellenir.
4. Sistem yöneticisinin ilişkisiz klinik kaynağa erişimi ve hastanın taslak nota erişimi `403 Forbidden` ile engellenir.
5. Karşılaşma tamamlanır; hasta için veri-minimize zaman çizelgesi ve append-only audit olayları doğrulanır.

## 4. Açık manuel kapı

Manuel testler henüz çalıştırılmamıştır. [`F04_Test.md`](../../F04_Test.md) içindeki rol bazlı API akışları, özellikle başhekim MFA ile yeniden açma ve yükleme hata durumları tamamlanıp sonuçları kaydedilmeden `F04-KAPI` işaretlenmez. Manuel akışta bulunan hata düzeltilir ve ilgili otomatik regresyon testi eklenir.
