# Google Antigravity ile geliştirme rehberi

Bu belge Antigravity'nin projede ana kodlama ajanı, Codex'in ise bağımsız kontrol/iyileştirme/test ajanı olarak kullanılacağı çalışma biçimini tanımlar. Son doğrulama: **27 Ağustos 2026**.

## Neden ayrı `implementation_plan.md` ve `task.md` eklenmedi?

Antigravity Planning Mode zaten görev başında Implementation Plan ve Task List, bitişte Walkthrough Artifact'ı üretir. Bu Artifact'lar geri bildirim ve inceleme için uygundur. Repoda ikinci bir kalıcı plan/checklist zinciri oluşturmak `ROADMAP.md` ile zamanla çelişir. Bu projede:

- `ROADMAP.md`: kalıcı kapsam, sıra, kabul ölçütü ve tamamlanma kanıtı;
- ADR/test/güvenlik belgeleri: kalıcı karar ve kalite sözleşmesi;
- Antigravity Artifact'ları: tek göreve ait geçici plan, görev listesi ve walkthrough;
- Git diff ve test çıktıları: uygulama kanıtıdır.

Antigravity workspace rules ve workflows için resmî konumlar sırasıyla `.agents/rules/` ve `.agents/workflows/` dizinleridir; rule/workflow başına 12.000 karakter sınırı vardır. [Antigravity Rules ve Workflows](https://antigravity.google/docs/ide/rules/)

## İlk kurulum

1. Antigravity'de bu repository'nin kök klasörünü workspace olarak aç.
2. Customizations > Rules bölümünde `.agents/rules/project-context.md` kuralının göründüğünü doğrula ve **Always On** yap.
3. Karmaşık roadmap işleri için **Planning Mode** kullan. Artifact Review Policy'yi **Request Review** seç; bu ayar plan/kod değişikliği öncesinde inceleme kapısı sağlar. [Artifact Review](https://antigravity.google/docs/artifact-review)
4. Terminal Auto Execution, Browser JavaScript Execution ve Artifact Review seçeneklerini **Request Review** tut; mümkünse Strict Mode'u etkinleştir ve workspace dışı erişimi kapalı bırak. [Antigravity güvenlik ayarları](https://antigravity.google/docs/ide/settings/)
5. Gerçek dış web erişimi verme. Browser testini yalnız yerel uygulama adresleri, Mailpit ve proje için açıkça onayladığın resmî doküman alanlarıyla sınırla. Antigravity izinlerinde `Deny > Ask > Allow` önceliği vardır; geniş `command(*)`, `read_url(*)`, `execute_url(*)` veya workspace dışı yazma izni verme. [Antigravity izin modeli](https://antigravity.google/docs/permissions/)
6. Windows'ta aynı checkout üzerinde birden fazla yazan ajan çalıştırma. Gerçek paralellik gerekiyorsa ajan başına ayrı git worktree ve farklı görev kullan.

## İlk konuşmada kullanılacak ana prompt

Aşağıdaki promptu yeni Antigravity konuşmasına yapıştır. `HEDEF_GÖREV` değerini değiştir; görev kimliğini boş bırakırsan ilk uygulanabilir iş seçilir.

```text
Bu repository Hastane Yönetim Sistemi projesidir. Ana kodlama ajanı olarak seçili tek ROADMAP görevini uçtan uca uygula; sonraki göreve geçme.

HEDEF_GÖREV: <ROADMAP_GÖREV_KİMLİĞİ>

Önce .agents/rules/project-context.md, AGENTS.md ve ROADMAP.md içindeki Ajan Çalışma Protokolü'nü oku. Ardından hedefe ilişkin gerçek kodu, testleri, docs/product/glossary.md dosyasını, ilgili ADR'leri, docs/security/authorization-matrix.md dosyasını, docs/testing/test-strategy.md dosyasını ve gerekli privacy belgelerini incele. Varsayımlarını koddan doğrula ve kullanıcıya ait mevcut değişiklikleri koru.

Planning Mode kullan. Önce native Implementation Plan ve Task List Artifact'larını oluştur. Plan; kabul ölçütü eşlemesi, etkilenen dosyalar/modül sahipliği, güvenlik ve veri sınırları, migration etkisi, otomatik/manüel test planı, riskler ve açık kararları içersin. Repo içine implementation_plan.md, task.md veya walkthrough.md ekleme; ROADMAP.md tek kalıcı görev kaynağıdır. Plan inceleme kapısında benden onay bekle.

Onaydan sonra yalnız hedef görevi uygula. Küçük doğrulanabilir dilimler kullan; mutlu yol, validation ve yetkisiz/negatif yolları test et. API yetkisini UI'ya bırakma. Modül sınırlarını, yalnız DEMO sentetik veri kuralını, klinik kayıt değişmezliğini ve hassas verinin log/telemetry/URL/exception'a girmemesi kuralını koru. Testleri zayıflatma veya atlama.

Bitirmeden hedef testleri, etkilenmiş testleri, Release build'i ve riskle orantılı tam çözüm testini çalıştır. DB değiştiyse gerçek PostgreSQL Testcontainer, migration idempotency ve pending-model kontrolü; UI değiştiyse bUnit ile gerçek browser/mobil-masaüstü/erişilebilirlik; permission değiştiyse allow/deny/scope/IDOR testleri yap. Diff'i güvenlik, veri sızıntısı, modül sınırı ve gereksiz kapsam açısından incele.

Yalnız tüm kabul ölçütleri ve doğrulamalar başarılıysa roadmap kutusunu işaretle, aktif görevi Yok yap ve ilerleme günlüğüne gerçek komut/test sonuçlarını yaz. Bir test çalışmadıysa görevi işaretleme; engeli ve tekrar komutunu kaydet. Sonunda native Walkthrough Artifact'ında değişiklikleri, dosyaları, test sayılarını, manuel doğrulamayı ve açık riskleri raporla. Commit veya push yapma.
```

İlk konuşmadan sonra aynı davranış için daha kısa biçimde şu workflow çağrısı yeterlidir:

```text
/roadmap-task <ROADMAP_GÖREV_KİMLİĞİ>
```

Antigravity; Planning Mode'da plan ve task list, uygulama boyunca diff, bitişte walkthrough ve browser kanıtı üretebilir. Plan/diff üzerine yorum verip gönderdiğinde ajan geri bildirimi uygulayabilir. [Google Antigravity başlangıç codelab'i](https://codelabs.developers.google.com/getting-started-agy-ide)

## Antigravity tamamladıktan sonra Codex'e verilecek kontrol promptu

```text
Antigravity ROADMAP üzerindeki <ROADMAP_GÖREV_KİMLİĞİ> görevini uyguladı. Değişiklikleri bağımsız olarak incele; Antigravity'nin tamamlandı iddiasına güvenme.

Önce AGENTS.md, ROADMAP.md, hedef kabul ölçütü, ilgili ADR'ler, authorization matrix ve test stratejisini oku. Git diff/status ile gerçek değişiklik kapsamını çıkar. Spec uyumu, modül sınırları, API'de Permission + Resource Scope + Care Relationship, IDOR/mass-assignment, klinik kayıt bütünlüğü, sentetik veri, secret/sağlık verisi sızıntısı, concurrency/idempotency, migration ve istemci sözleşmesi açısından kod incelemesi yap.

Bulduğun hataları ve eksik testleri kapsam içinde düzelt. Hedef testleri ve riskle orantılı tam doğrulama setini gerçekten çalıştır; testleri gevşetme veya atlama. UI varsa gerçek browser ve erişilebilirlik, DB varsa PostgreSQL/migration, yetki varsa allow/deny/scope negatif yollarını doğrula. Kabul ölçütlerinin tümü kanıtlanmıyorsa ROADMAP kutusunu [ ] durumuna geri getir ve engeli yaz. Sonuçta bulguları önem sırasıyla, yaptığın düzeltmeleri, çalıştırdığın komut/test sayılarını, manuel test adımlarını ve kalan riskleri raporla. Commit veya push yapma.
```

## Kullanım disiplini

- Antigravity bir görevi bitirip walkthrough sunduktan sonra Codex incelemesini ayrı konuşmada yaptır.
- Codex yeşil doğrulama vermeden sonraki roadmap görevine başlama.
- Büyük fazı tek promptta vermek yerine bir roadmap görevi → bağımsız review → sonraki görev döngüsünü kullan.
- Bir plan yeni paket, dış servis, public contract kırılması, veri kaybı veya Accepted ADR değişikliği öneriyorsa Artifact üzerinde yorum ver ve uygulamadan önce kararı netleştir.
- Antigravity/Codex test sonuçları uyuşmazsa en son temiz checkout/aynı konfigürasyonla yeniden üretilen çıktı esas alınır; “bende geçti” ifadesi kanıt değildir.
