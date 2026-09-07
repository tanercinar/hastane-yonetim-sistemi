# Hastane Yönetim Sistemi — Antigravity Proje Sözleşmesi

Bu workspace kuralını Antigravity Customizations > Rules ekranında **Always On** olarak etkinleştir.

## Bağlayıcı kaynaklar ve öncelik

- Genel sözleşme: @../../AGENTS.md
- Kanonik plan, aktif görev ve kabul ölçütleri: @../../ROADMAP.md
- Alan terimleri: `docs/product/glossary.md`
- Mimari kararlar: `docs/adr/`
- Yetki: `docs/security/authorization-matrix.md`
- Test: `docs/testing/test-strategy.md`
- Veri/gizlilik: `docs/privacy/`
- Hedef ve kapsam seçiminde sıralama: kullanıcının açık güncel isteği > ROADMAP kabul ölçütü > Accepted ADR > güvenlik/test/veri belgeleri > mevcut kod. `AGENTS.md` içindeki güvenlik, klinik bütünlük ve sentetik veri değişmezleriyle çelişen isteği sessizce uygulama; dur, çelişkiyi ve gereken kapsam/ADR kararını kullanıcıya bildir.

## Tek kaynak ve görev sınırı

- `ROADMAP.md` tek kalıcı görev/status kaynağıdır. Antigravity Implementation Plan, Task List ve Walkthrough öğelerini native Artifact olarak üret; repo köküne `implementation_plan.md`, `task.md` veya `walkthrough.md` kopyaları ekleme.
- Görev kimliği verilmediyse bağımlılıkları bitmiş ilk `[ ]` görevi seç. Açık toplu izin yoksa yalnız bir roadmap görevi uygula ve sonraki göreve geçme.
- Başlangıçta `git status` ile kullanıcı değişikliklerini koru, hedef kaynakları gerçekten oku ve `Aktif görev` satırını güncelle. Commit, push, publish, gerçek dış sistem çağrısı veya destructive git komutu ancak açık kullanıcı isteğiyle yapılabilir.

## Planlama ve uygulama kapısı

- Roadmap görevi için Planning Mode kullan. Implementation Plan en az hedef/kapsam dışı, gereksinim-kabul eşlemesi, etkilenen bileşenler, mimari/modül sınırı, güvenlik-gizlilik, veri/migration, otomatik-manuel doğrulama, riskler ve açık soruları içersin.
- Planı varsayımla doldurma: önce kodu, mevcut test desenlerini ve ilgili belgeleri incele. Geri döndürülemez işlem, yeni dış bağımlılık, public API kırılması, veri kaybı riski veya yeni mimari karar varsa onay almadan uygulama yapma.
- Task List yalnız seçili roadmap görevinin küçük, sıralı ve doğrulanabilir dilimlerini içersin. Her dilimin gözlemlenebilir sonucu ve testi olsun.
- Hata alınca tam çıktıyı incele, kök nedeni kanıtla ve regresyon testi ekle. Testi silerek, gevşeterek, `Skip` ederek veya ilgisiz kapsam büyüterek yeşile boyama.

## Değişmez teknik ve klinik kurallar

- .NET/C# çözümü modüler monolittir. Modül başka modülün Infrastructure, DbContext, entity veya tablosuna doğrudan erişmez; açık contract/application service/event kullanır.
- Domain/Application katmanına ASP.NET Core, EF Core, Web/MAUI veya UI ayrıntısı sokma. UI yalnız API contract tüketir; UI görünürlüğü yetkilendirme değildir.
- Yetki API'de `Permission + Resource Scope + Care Relationship` ile ve varsayılan red yaklaşımıyla uygulanır. Her yeni izin için allow ve anlamlı deny/IDOR testi yaz.
- İmzalı/final klinik kayıt silent update veya hard delete görmez; correction/addendum/entered-in-error semantiğini koru.
- Yalnız gerçek kişiden türetilmemiş `DEMO-*`/reserved-domain sentetik veri ve açıkça `MOCK` entegrasyon kullan. Gerçek endpoint, credential, kişi veya sağlık verisi ekleme.
- Parola, token, secret, klinik içerik ve hassas kimlik log, trace, metric, URL, exception veya ekran görüntüsüne girmez.
- PostgreSQL davranışını kritik integration testinde gerçek PostgreSQL Testcontainer ile doğrula; EF InMemory kullanma. UTC, constraint, concurrency ve idempotency gereksinimlerini uygun katmanlarda test et.

## Tamamlama ve ajan koordinasyonu

- Mutlu yolun yanında validation ve en az bir authentication/authorization/negatif yol çalışmadan görev tamam değildir. API değişiminde contract; DB değişiminde migration + boş PostgreSQL + pending-model; UI değişiminde loading/empty/error/forbidden, klavye/label ve responsive browser kanıtı gerekir.
- Bitirmeden hedef testleri, etkilenmiş testleri, Release build'i ve riskle orantılı tam çözüm doğrulamasını çalıştır; farkı güvenlik, veri sızıntısı, modül sınırı ve gereksiz kapsam açısından incele.
- Yalnız bütün kabul ölçütleri ve doğrulamalar geçtiyse roadmap kutusunu `[x]`, `Aktif görev` değerini `Yok` yap ve ilerleme günlüğüne gerçek komut/sonuç kanıtı ekle. Çalışmayan test varsa kutu `[ ]` kalır; engel ve tekrar komutu yazılır.
- Aynı checkout üzerinde paralel yazan ajan çalıştırma. Paralellik gerekirse ajan başına ayrı git worktree ve farklı roadmap görevi kullan; `ROADMAP.md` birleştirmesini tek koordinatör yapsın.
- Walkthrough; yapılanlar, değişen önemli dosyalar, çalıştırılan komutların geçen/başarısız/atlanan sayıları, migration/browser kanıtı, çalıştırılamayan kontroller, açık riskler ve manuel test adımlarını açıkça raporlasın.
