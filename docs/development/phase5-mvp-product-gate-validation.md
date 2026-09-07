# Faz 5 MVP Kapısı Doğrulama Raporu

Bu belge Faz 5 reçete, eczane ve klinik stok kapısının güncel otomatik kanıtını ve manuel kabul durumunu kaydeder. Kod incelemesi ve otomatik kapılar 30 Ağustos 2026 tarihinde tamamlanmıştır; kullanıcı tarafından [`F05_Test.md`](../../F05_Test.md) uygulanana kadar `F05-KAPI` açık tutulur.

## İnceleme sonucunda giderilen kritik riskler

- Reçete işlemleri `Permission + Resource Scope + Care Relationship` zincirine bağlandı; istekten gelen hekim kimliğine güvenme ve rol/izin alternatifli geçiş kaldırıldı.
- Güvenlik kontrolü gerçek ve yetkili açık encounter gerektiriyor; alerji kaynağı hatasında fail-open yerine kritik uyarı üretiliyor.
- Eczacı yanıtından tanı, encounter, bölüm/hekim kimlikleri ve düzeltme gerekçeleri redakte edildi; iş listesi tesis kapsamı ve geçerlilikle sınırlandı.
- Stok eczane bölümüyle kapsamlandı; rezervasyonun tüketilmesi engellendi; stok/reçete yazmalarına optimistic concurrency eklendi.
- Teslim istekleri kalıcı `IdempotencyKey` ve SHA-256 istek parmak izi kullanıyor. Aynı istek ikinci stok düşümü yapmıyor; aynı anahtarın farklı içerikle kullanımı reddediliyor.
- Klinik gerekçeler audit kaydına ham metin olarak yazılmıyor.
- Canonical rol kodları (`DOC`, `PHA`, `NUR` vb.) istemci oturumunda tanındı; gerçek girişte doktor ve eczacı ekranlarının yanlışlıkla yasaklanması giderildi.
- Temiz PostgreSQL kurulumunu bozan migration işlemleri düzeltildi ve eczane bölümü deterministik organizasyon verisine eklendi.

## Otomatik doğrulanan senaryolar

1. Gerçek PostgreSQL üzerinde karşılaşma, vital, tanı/SOAP, reçete, FEFO teslim, hasta görünümü, IDOR, stok ve audit akışı.
2. Playwright/Chromium üzerinde doktorun encounter bağlı reçete oluşturup imzalaması, eczacının FEFO lotuyla teslimi ve hastanın 390x844 görünümde sade talimatları görmesi.
3. Bayat reçete/stok sürümü, tekrar istek, farklı içerikle idempotency anahtarı kullanımı, miktar aşımı ve terminal durum negatif yolları.
4. Eczacı klinik veri redaksiyonu, hasta sahipliği ve klinisyen bakım ilişkisi sınırları.

## Güncel test kanıtı

| Kapı | Sonuç |
|---|---:|
| Build | 0 uyarı / 0 hata |
| Unit | 162 / 162 |
| Component | 43 / 43 |
| Architecture | 13 / 13 |
| Integration | 91 / 91 |
| Playwright E2E | 4 / 4 |

Toplam: 313/313 otomatik test başarılıdır (162 unit + 43 component + 13 architecture + 91 integration + 4 E2E).

Ek kontroller: `dotnet format --verify-no-changes`, bağımlılık zafiyet kapısı ve `tools/validate-phase0.ps1` başarılıdır.

## Manuel kabul durumu

Manuel testler henüz yapılmadı. [`F05_Test.md`](../../F05_Test.md) içindeki rol geçişi, güvenlik uyarısı, klinik izolasyon, FEFO, tekrar gönderim, mobil hasta görünümü ve negatif erişim adımları tamamlanıp sonuç kaydedilmeden `F05-KAPI` işaretlenmez ve aktif görev Faz 6'ya geçirilmez.
