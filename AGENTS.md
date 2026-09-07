# Hastane Yönetim Sistemi — Ajan Talimatları

## Her görevden önce

1. [`ROADMAP.md`](ROADMAP.md) içindeki Ajan Çalışma Protokolü'nü, aktif fazı ve sıradaki uygulanabilir görevi oku.
2. `docs/product/glossary.md`, ilgili ADR, izin matrisi ve test stratejisini hedefe göre oku.
3. `git status` ile kullanıcı değişikliklerini koru; ilgisiz dosyaları geri alma.

## Çalışma sözleşmesi

- Kullanıcı bir görev kimliği vermediyse bağımlılıkları bitmiş ilk `[ ]` görevi uygula.
- Kullanıcı açıkça toplu izin vermedikçe bir turda yalnız bir roadmap görevi tamamla.
- `ROADMAP.md` tek kalıcı görev/status kaynağıdır. Araçların ürettiği implementation plan, task list ve walkthrough öğelerini native/geçici artifact olarak tut; repoya aynı işi izleyen paralel kök belgeler ekleme.
- Yalnız kabul ölçütü, otomatik test ve gerekli manuel doğrulama sağlanırsa `[x]` işaretle.
- Test çalışmadıysa kutuyu işaretleme; engeli ve tekrar komutunu ilerleme günlüğüne yaz.
- Görev sonunda `ROADMAP.md` aktif görevini, kutusunu ve ilerleme günlüğünü güncelle.
- Aynı checkout üzerinde paralel yazan ajan çalıştırma. Paralellik gerekiyorsa ajan başına ayrı git worktree ve farklı roadmap görevi kullan; `ROADMAP.md` güncellemesini tek koordinatör birleştirsin.

## Değişmez proje kuralları

- Yalnız sentetik `DEMO` veri kullan; gerçek kimlik, hasta verisi, secret veya gerçek kurum credential'ı ekleme.
- Sistem sertifikalı HBYS veya klinik karar desteği değildir; AI uygulamaya gömülmez.
- Yetki UI'da değil API'de `Permission + Resource Scope + Care Relationship` ile uygulanır.
- Klinik final/imzalı kayıt sessizce güncellenmez/silinmez; düzeltme, ek not veya hatalı giriş kaydı kullanılır.
- Modüller başka modülün Infrastructure/DbContext/tablosuna doğrudan erişmez.
- İş kuralı UI'ya, Web/MAUI ayrıntısı Domain/Application'a girmez.
- Mock entegrasyon açıkça `MOCK` etiketlidir ve gerçek endpoint'e çağrı yapmaz.
- Klinik içerik/token/secret log, telemetry, URL veya exception mesajına yazılmaz.
- Kapsam dışı finans, satın alma, bordro ve tam İK özelliklerini erken ekleme.

## Doğrulama

- Hedef davranış için mutlu, validation ve en az bir yetkisiz/negatif yol test et.
- Kritik yazmalarda concurrency/idempotency; API'de contract; UI'da loading/empty/error/forbidden ve erişilebilirlik kontrolü yap.
- Koddan sonra diff'i güvenlik, veri sızıntısı, modül sınırı ve gereksiz kapsam açısından incele.

Ayrıntılı kaynaklar: [`ROADMAP.md`](ROADMAP.md), `docs/testing/test-strategy.md`, `docs/security/authorization-matrix.md`, `docs/privacy/` ve `docs/adr/`. Antigravity kurulumu, promptları ve Codex inceleme devri için [`docs/agents/antigravity.md`](docs/agents/antigravity.md) kullanılır.
