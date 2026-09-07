# Faz 0 Kapı İncelemesi

- **Tarih:** 2026-08-13
- **Kapsam:** F00-G01–F00-G08
- **Karar:** Geçti
- **İnceleme türü:** Belge bütünlüğü, masa başı senaryo ve otomatik yapı kontrolü

## Kapı ölçütleri

| Ölçüt | Kanıt | Sonuç |
|---|---|---|
| Tüm roller ve kapsam izlenebilir | `scope.md`, `user-stories.md`, `authorization-matrix.md` | Geçti |
| Kanonik alan dili ve durum geçişleri açık | `glossary.md`, `state-machines.md`, `UBIQUITOUS_LANGUAGE.md` | Geçti |
| Rol, eylem ve kaynak kapsamı ayrılmış | Permission kataloğu + zorunlu negatif test çiftleri | Geçti |
| Veri amacı, sınıfı, saklama ve imha yaklaşımı tanımlı | `data-inventory.md`, `data-classification.md`, `retention-policy.md` | Geçti |
| Yüksek riskler önleme ve tespit kontrolüne bağlı | `threat-model.md` yüksek risk eşlemesi | Geçti |
| Altı temel mimari karar kaydedilmiş | ADR-0001–ADR-0006 | Geçti |
| Codex, Claude ve Cursor talimatları çelişmiyor | `AGENTS.md`, `CLAUDE.md`, `.cursor/rules/project-workflow.mdc` | Geçti |
| Test seviyeleri, kapılar ve başarısızlık politikası tanımlı | `test-strategy.md` | Geçti |
| Yerel belge bağlantıları ve gerekli dosyalar mevcut | `tools/validate-phase0.ps1` | Geçti |

## Masa başı senaryo 1 — Hasta randevusu

### Akış

1. `PAT` kendi hesabıyla uygunluk/slot arar (`US-PAT-001`).
2. `appointment.book-own` izni `OWN` kapsamındaki hasta için çalışır.
3. Randevu `Reserved → Confirmed` geçişi yapar.
4. Aynı slotta ikinci kullanıcı yarışırsa veritabanı constraint/transaction yalnız birini başarılı kılar.
5. Kayıt personeli `FACILITY` kapsamında check-in yapar; randevu `Confirmed → CheckedIn` olur.
6. İşlem audit ve minimum içerikli bildirim üretir.

### Çapraz kanıt

| Boyut | Kaynak |
|---|---|
| Alan dili | Randevu Slotu, Randevu, Check-in, Sıra |
| Kullanıcı hikâyesi | US-PAT-001–003, US-REG-002, AC-E2E-01 |
| Yetki | `appointment.*`, `appointment.check-in`; OWN/FACILITY |
| Durum | Randevu durum makinesi |
| Veri | Randevu/check-in C2/C3 bağlam, RET-APPOINTMENT |
| Tehdit | TM-04 IDOR, TM-07 yarış, TM-20 DoS |
| Test | API allow/deny, IDOR, concurrency, Playwright hasta→kayıt akışı |

### Sonuç

Çelişki yok. “Randevu” hizmet planı, “Karşılaşma” fiilî hizmet olarak ayrı tutulmuştur.

## Masa başı senaryo 2 — Muayene, imza, reçete ve teslim

### Akış

1. Check-in olmuş randevudan yetkili doktor `encounter.start` ile karşılaşma başlatır.
2. Atanmış hemşire vital girer; doktor klinik not ve tanı taslağını oluşturur.
3. Doktor notu/reçeteyi imzalar; final içerik immutable sürüm olur.
4. Eczacı `ASSIGNED/FACILITY` kapsamında yalnız reçete ve gerekli güvenlik özetini görür.
5. Eczacı kısmi/tam teslimi lot ve miktarla kaydeder; stok atomik azalır.
6. Hasta yalnız kendi imzalı reçetesini ve teslim durumunu görür.

### Çapraz kanıt

| Boyut | Kaynak |
|---|---|
| Alan dili | Karşılaşma, Klinik Not, İmza, Düzeltme, Reçete, Teslim |
| Kullanıcı hikâyesi | US-NUR-002, US-DOC-001–006, US-PHA-001–004, AC-E2E-02 |
| Yetki | `encounter.*`, `clinical-note.*`, `prescription.*`; CARE_TEAM/ASSIGNED |
| Durum | Karşılaşma, klinik not, reçete/teslim durum makineleri |
| Veri | Encounter/not/reçete/teslim C3; RET-CLINICAL/RET-DISPENSE |
| Tehdit | TM-04 IDOR, TM-06 silent update, TM-07 negatif stok, TM-10 sızıntı |
| Test | Final kayda illegal update, yanlış eczacı kapsamı, reçete üstü/çift teslim, E2E |

### Sonuç

Çelişki yok. Eczacıya genel encounter notu verilmez; klinik final kayıt correction/addendum olmadan değişmez.

## Masa başı senaryo 3 — Laboratuvar istemi ve kritik sonuç

### Akış

1. Bakım ilişkili doktor `diagnostic-order.create` ile laboratuvar istemi oluşturur.
2. LAB iş listesinde minimum hasta doğrulama ve istem bilgisi görür.
3. Numune `Expected → Collected → Received → Accepted → InProcess → Completed` zincirinden geçer.
4. Sonuç preliminary/technical verified/final olur; düzeltme yeni final sürüm üretir.
5. Kritik bayrak bakım ekibine SignalR/uygulama içi bildirim ve alındı kaydı üretir.
6. Taslak sonuç hastaya görünmez; yayın politikası izin veren final sürüm görünür.

### Çapraz kanıt

| Boyut | Kaynak |
|---|---|
| Alan dili | Klinik İstem, Laboratuvar İstemi, Numune, Gözetim Zinciri, Sonuç, Kritik Sonuç |
| Kullanıcı hikâyesi | US-DOC-004, US-LAB-001–004, AC-E2E-03 |
| Yetki | `diagnostic-order.create`, `laboratory.*`, `diagnostic-result.view-final-own` |
| Durum | Klinik istem, numune ve tanısal sonuç durum makineleri |
| Veri | Numune/sonuç C3; RET-SPECIMEN/RET-CLINICAL |
| Tehdit | TM-04 IDOR, TM-06 final sonuç kurcalama, TM-13 SignalR grup atlama |
| Test | Barkod benzersizliği, bozuk geçiş, corrected final, taslak sızıntısı, SignalR deny |

### Sonuç

Çelişki yok. Kritik işareti otomatik klinik karar değil, insan belirlenmiş demo eşik ve bildirim niteliğidir.

## Masa başı senaryo 4 — Yatış, transfer ve taburculuk

### Akış

1. Bakım ilişkili doktor yatış ister; yetkili ekip kabul ve yatak ataması yapar.
2. Bir yatışta tek aktif atama, bir yatakta tek aktif yatış constraint ile korunur.
3. İç transfer eski atamayı kapatıp yenisini aynı transaction'da açar.
4. Atanmış hemşire bakım planı/eMAR; doktor klinik kayıt ve taburculuk özeti yürütür.
5. Taburcu ile son yatak ataması kapanır ve yatak uygun olur.

### Çapraz kanıt

| Boyut | Kaynak |
|---|---|
| Alan dili | Yatış, Yatak, Yatak Ataması, İç Transfer, Taburculuk, Sevk |
| Kullanıcı hikâyesi | US-INP-001–002, US-NUR-003–004, US-DOC-007, AC-E2E-04 |
| Yetki | `admission.*`, `bed.*`, `care-plan.manage`, `medication.administer`, `discharge.complete` |
| Durum | Yatış ve yatak ataması durum makinesi |
| Veri | Yatış/yatak/eMAR C3; RET-CLINICAL |
| Tehdit | TM-04 bölüm dışı erişim, TM-07 çift yatak, TM-08/09 audit |
| Test | Aynı yatak yarış testi, yanlış bölüm deny, hareket geçmişi, taburcu E2E |

### Sonuç

Çelişki yok. Kurum içi yatak/bölüm değişimi “İç Transfer”, kurum dışı yönlendirme “Sevk” olarak ayrılmıştır.

## Belgeler arası karar denetimi

| Soru | Karar |
|---|---|
| Sistem gerçek veri kullanacak mı? | Hayır; ADR-0006 ve tüm veri belgeleri yalnız sentetik veri ister. |
| Uygulama içinde AI var mı? | Hayır; yalnız geliştirme araçlarıdır. Triyaj/uyarı insan seçimi veya deterministik demo kuralıdır. |
| Yetki rol ile mi sınırlı? | Hayır; permission + kaynak kapsamı + bakım ilişkisi zorunludur. |
| ADM klinik içerik görür mü? | Hayır; teknik hesap/yetki yönetimiyle sınırlıdır. |
| Final klinik kayıt CRUD update alır mı? | Hayır; correction/addendum/entered-in-error gerekir. |
| Native istemci offline çalışır mı? | Hayır; çevrimiçi API zorunlu, klinik offline cache/yazma kuyruğu yoktur. |
| Dış sistem gerçek mi? | Hayır; reserved endpoint kullanan açık MOCK adaptördür. |
| Finans ve tam İK var mı? | Hayır; roller gelecek katalogda, işlevleri kapsam dışıdır. |

## Açık kararlar ve engel değerlendirmesi

Aşağıdakiler sonraki fazlarda normal uygulama kararlarıdır ve Faz 1'i engellemez:

- Kesin NuGet minor sürümleri Faz 1 restore sırasında güncel destekle sabitlenecek.
- Native OIDC sağlayıcı/bileşeni Faz 12 başında güncel destekle seçilecek.
- `TBD-LEGAL` sağlık saklama süreleri yalnız gerçek kullanım düşünülürse uzmanla kesinleştirilecek; demo modunda production iddiası yoktur.
- Radyolog gibi rol içi alt uzmanlıklar permission/StaffProfile özelliğiyle modellenir; ayrı üst rol zorunlu değildir.

## Son karar

Faz 0 belgeleri kendi aralarında tutarlı, Faz 1 teknoloji temeline başlamak için yeterli ve kritik belirsizliklerden arınmıştır. Kapı **geçmiştir**. Bir sonraki uygulanabilir görev `F01-G01 — SDK ve repository temeli`dir.
