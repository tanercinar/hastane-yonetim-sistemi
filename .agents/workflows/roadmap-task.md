# Roadmap Görevi Uygulama Workflow'u

Bu workflow'u `/roadmap-task <GÖREV_KİMLİĞİ>` biçiminde çağır. Görev kimliği eklenmezse bağımlılıkları tamamlanmış ilk işaretsiz görev seçilir.

1. `.agents/rules/project-context.md`, `AGENTS.md` ve `ROADMAP.md` içindeki Ajan Çalışma Protokolü'nü oku. Hedef görevin bağımlılıklarını, kabul ölçütünü, aktif fazı ve son ilerleme kayıtlarını çıkar.
2. `git status` ve ilgili kaynak/test/dokümanları incele. Kullanıcı değişikliklerini, mevcut mimariyi ve çalışan desenleri koru. Hedefe göre glossary, ilgili ADR, izin matrisi, threat model, privacy envanteri ve test stratejisini oku.
3. Hedef görev başka bir ajan tarafından aktif görünüyorsa veya çalışma ağacında aynı dosyalara ait açıklanamayan değişiklik varsa yazmayı durdur ve çakışmayı bildir.
4. Native **Implementation Plan** Artifact'ı üret. Şu başlıkları kullan:
   - Hedef ve kapsam dışı
   - Mevcut durumdan kanıtlar
   - Kabul ölçütü → kod/test/doküman eşlemesi
   - Etkilenen bileşenler ve modül sahipliği
   - Veri modeli, migration ve geri alma/ileri alma notu
   - Permission, resource scope, care relationship ve kötüye kullanım senaryoları
   - Gizlilik, log/telemetry ve sentetik veri sınırı
   - Otomatik ve manuel doğrulama planı
   - Riskler, alternatifler ve kullanıcı kararı gereken noktalar
5. Native **Task List** Artifact'ı seçili roadmap görevinin küçük dikey dilimlerinden oluştur. Her madde bir gözlemlenebilir sonuç, ilgili test ve gerekirse doküman güncellemesi içersin. Plan incelemesi isteniyorsa kullanıcı onayı gelmeden dosya değiştirme.
6. Onaydan sonra `ROADMAP.md` içindeki `Aktif görev` satırını hedef kimliğe çevir. İş dilimlerini sırayla uygula; mevcut kod tarzına, merkezi paket sürümlerine ve modül sınırlarına uy. Kapsamı ikinci roadmap görevine taşırma.
7. Her dilimde en küçük ilgili testi çalıştır. Bug düzeltmesinde önce hatayı yeniden üreten regresyon testi yaz. Başarısızlıkta semptomu maskeleme; çıktıyı incele, kök nedeni düzelt ve aynı testi yeniden çalıştır.
8. Tamamlanma öncesi hedefe uygun doğrulamaları seç ve sonuçlarını kaydet:
   - `dotnet restore .\HospitalManagement.slnx --locked-mode`
   - `dotnet format .\HospitalManagement.slnx --verify-no-changes --no-restore`
   - `dotnet build .\HospitalManagement.slnx --configuration Release --no-restore`
   - `dotnet test .\HospitalManagement.slnx --configuration Release --no-build`
   - Paket/config değiştiyse `tools/test-dependency-vulnerabilities.ps1`
   - DB değiştiyse gerçek PostgreSQL integration testi, migration ilk/ikinci koşu ve `dotnet ef migrations has-pending-model-changes`
   - UI değiştiyse bUnit + gerçek browser; mobil ve masaüstü viewport, klavye, console/network ve erişilebilir durumlar
   - API/yetki değiştiyse contract, anonymous, yanlış permission, yanlış scope ve IDOR negatif testleri
9. Değişiklikleri güvenlik, secret/sağlık verisi sızıntısı, mass assignment, modül bağımlılığı, klinik kayıt bütünlüğü, concurrency/idempotency ve gereksiz dosya/paket açısından gözden geçir. Test çıktısı olmadan “geçti” deme.
10. Tüm kabul ölçütleri sağlandıysa roadmap maddesini `[x]`, `Aktif görev` değerini `Yok` yap ve ilerleme günlüğüne gerçek komutları, test sayılarını ve ana dosyaları ekle. Bir kontrol çalışmadıysa `[ ]` bırak; engeli ve yeniden deneme komutunu yaz.
11. Native **Walkthrough** Artifact'ında sonuç, önemli dosyalar, doğrulama tablosu, migration/browser kanıtı, manuel test adımları ve açık riskleri özetle. Bir sonraki roadmap görevini yalnız ad olarak bildir; uygulamaya başlama.
