# Ubiquitous Language

Projenin kanonik ve ayrıntılı alan sözlüğü:

- [`docs/product/glossary.md`](docs/product/glossary.md)
- Durum geçişleri: [`docs/product/state-machines.md`](docs/product/state-machines.md)

## Kanonik terim özeti

| Terim | Tanım | Kaçınılacak eşanlamlı |
|---|---|---|
| **Randevu** | Planlanan ziyaret taahhüdü. | Muayene, karşılaşma |
| **Karşılaşma** | Fiilen sunulan klinik etkileşim. | Randevu |
| **Klinik İstem** | Test, görüntüleme veya başka klinik hizmet talebi. | Sonuç |
| **Reçete** | İmzalı ayaktan ilaç talimatları bütünü. | Teslim |
| **Yatış** | Hastanın yataklı bakım sorumluluğuna alınması. | Check-in |
| **İç Transfer** | Aktif yatışta servis/oda/yatak değişikliği. | Sevk |
| **Sevk** | Kurum dışı hizmet sunucusuna yönlendirme. | İç transfer |
| **Düzeltme** | Eski klinik sürümü koruyan gerekçeli yeni sürüm. | Sessiz update |
| **Denetim Olayı** | Aktör, hedef, eylem ve sonucu taşıyan değişmez güvenlik kaydı. | Klinik not, uygulama logu |
| **Sentetik Veri** | Gerçek kişiden türetilmemiş `DEMO` verisi. | Anonim gerçek veri |

## Temel ilişkiler

- Bir **Randevu** en fazla bir **Karşılaşma** başlatır.
- Bir **Karşılaşma** bir hastaya aittir ve birden çok **Klinik İstem** üretebilir.
- Bir **Reçete**, birden çok kalem ve her kalem birden çok kısmi **Teslim** içerebilir.
- Bir **Yatış**, ardışık ve çakışmayan **Yatak Atamaları** içerir.
- İmzalı klinik kayıt yalnızca **Ek Not**, **Düzeltme** veya **Hatalı Giriş** ile değişen anlam kazanır.

## Örnek diyalog

> **Geliştirici:** “Randevu başladığında aynı kaydı muayeneye çevireyim mi?”
>
> **Alan uzmanı:** “Hayır; **Randevu** planı korur, fiilî hizmet için ayrı **Karşılaşma** açılır.”
>
> **Geliştirici:** “İmzalı nottaki hatayı update edebilir miyim?”
>
> **Alan uzmanı:** “Sessizce değil; eski sürümü koruyan gerekçeli **Düzeltme** oluşturmalısın.”

## İşaretlenen belirsizlikler

- “Kayıt” tek başına kullanılmaz; Hasta Kaydı, Check-in, Klinik Not veya Denetim Olayı denir.
- “Kabul” yerine ayaktan süreçte Check-in, yataklı süreçte Yatış Kabulü denir.
- “Transfer” yerine kurum içinde İç Transfer, kurum dışında Sevk denir.
- “Yetki” rol değildir; izin, kaynak kapsamı ve bakım ilişkisinin birlikte sonucudur.
- “Anonim”, “maskeli”, “takma adlı” ve “sentetik” veri birbirinin yerine kullanılmaz.
