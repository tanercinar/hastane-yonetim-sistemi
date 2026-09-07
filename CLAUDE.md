# Claude Proje Talimatları

Bu proje için bağlayıcı yürütme kaynağı [`ROADMAP.md`](ROADMAP.md), kanonik alan dili `docs/product/glossary.md` ve genel ajan sözleşmesi `AGENTS.md` dosyasıdır. Bir işe başlamadan önce üçünü ve hedef görevle ilgili ADR/güvenlik/test belgelerini oku.

## İş akışı

- Belirli görev verilmediyse bağımlılıkları tamamlanmış ilk `[ ]` roadmap görevini seç.
- Açık toplu izin yoksa tek turda yalnız o görevi uygula.
- `ROADMAP.md` tek kalıcı görev kaynağıdır; araç-özel plan/task/walkthrough çıktıları repoda paralel checklist'e dönüştürülmez.
- Kabul ölçütleri + otomatik test + gerekli manuel kontrol tamamlanmadan `[x]` yapma.
- Başarısız/çalışmayan testi gizleme, silme veya beklentisini keyfî değiştirme.
- Bitişte aktif görev, checkbox ve ilerleme günlüğünü güncelle; test komutlarını kanıt olarak yaz.

## Koruma kuralları

- Yalnız `DEMO` sentetik veri; gerçek sağlık/kimlik verisi ve secret yasaktır.
- Permission + kaynak kapsamı + bakım ilişkisi API'de denetlenir.
- İmzalı/final klinik kayıt silent update/hard delete görmez.
- Modül sınırlarını ve API-öncelikli istemci mimarisini koru.
- Mock entegrasyonlar gerçek endpoint/credential kullanmaz.
- Hassas veri log, trace, URL, metric veya hata metnine girmez.
- Kullanıcının ilgisiz mevcut değişikliklerini koru; destructive git komutu çalıştırma.

Detaylar için `AGENTS.md`, `docs/security/authorization-matrix.md`, `docs/privacy/`, `docs/testing/test-strategy.md` ve `docs/adr/` kaynaklarına uy. Antigravity ile görev üretimi ve bağımsız Codex kontrol devri `docs/agents/antigravity.md` içindedir.
