# HYS tasarım sistemi temeli

Bu belge `F01-G08` ile oluşturulan paylaşılabilir tasarım sisteminin sözleşmesidir. Sistem; Web ve
gelecekteki .NET MAUI Blazor Hybrid istemcilerinde aynı durum dili, token adları ve erişilebilirlik
kurallarını kullanmayı hedefler.

## Tasarım ilkeleri

1. Klinik ve operasyonel yoğunlukta sakin, okunabilir ve güven veren bir görsel dil kullanılır.
2. Renk tek başına anlam taşımaz; metin, ikon ve semantic markup birlikte kullanılır.
3. UI'da gizleme yetkilendirme değildir. Gerçek karar API'de permission, resource scope ve bakım
   ilişkisiyle verilir.
4. Gerçek hasta verisi kullanılmaz. Örnek sayılar ve içerikler görünür biçimde `DEMO` olarak işaretlenir.
5. Tek seferlik hardcoded değer yerine var olan token kullanılır; yeni değer gerekiyorsa önce token
   kataloğu genişletilir.

## Token kataloğu

Tokenlar `HospitalManagement.UI/wwwroot/design-system.css` içinde `--hys-*` önekiyle tanımlıdır.

| Kategori | Adlandırma | Örnekler |
|---|---|---|
| Renk | `--hys-color-{amaç veya ölçek}` | `brand-700`, `surface`, `text-muted`, `danger-100` |
| Tipografi | `--hys-font-*`, `--hys-line-height-*` | `font-size-sm`, `font-mono`, `line-height-normal` |
| Aralık | `--hys-space-{ölçek}` | `space-2`, `space-4`, `space-8` |
| Köşe | `--hys-radius-{boyut}` | `radius-sm`, `radius-lg`, `radius-pill` |
| Gölge | `--hys-shadow-{seviye}` | `shadow-sm`, `shadow-md` |
| Hareket | `--hys-duration-*`, `--hys-ease-*` | `duration-fast`, `ease-standard` |

`data-theme="dark"` için temel semantic renk eşlemeleri hazırdır; kullanıcı tema seçimi henüz bu
görevin kapsamında değildir. Hareketler `prefers-reduced-motion: reduce` altında kapatılır.

## Responsive kabuk

| Genişlik | Davranış |
|---|---|
| `> 72rem` | Dört kolonlu örnek metrikler ve sabit sol navigasyon |
| `48rem–72rem` | İki kolonlu metrikler ve sabit sol navigasyon |
| `≤ 48rem` | Navigasyon üstte yatay akışa geçer; içerik ve durum galerisi tek kolon olur |
| `≤ 34rem` | Metrikler tek kolon olur, ikincil rozet saklanır ve içerik aralığı daralır |

Kabukta ana içeriğe geç bağlantısı, tek bir `main` landmark'ı, etiketli `nav` ve görünür klavye focus
stili bulunur. Dokunma hedefleri en az `2.75rem` yüksekliğindedir.

## Bileşen: LoadingState

### Amaç

Sunucudan veya istemci servisinden veri beklenirken mevcut alanı güvenli biçimde yerinde tutar.

| Özellik | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Title` | `string?` | Yerelleştirilmiş metin | Kısa yükleme başlığı |
| `Description` | `string?` | Yerelleştirilmiş metin | Kullanıcının neden beklediğini açıklar |

- `role="status"`, `aria-live="polite"` ve `aria-busy="true"` üretir.
- Dekoratif ikon screen reader'dan gizlidir.
- Reduced-motion tercihinde dönüş animasyonu kapatılır.

## Bileşen: EmptyState

### Amaç

Başarılı sorgunun gösterilecek kayıt üretmediğini hata durumundan ayırır.

| Özellik | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Title` | `string?` | Yerelleştirilmiş metin | Boş durum başlığı |
| `Description` | `string?` | Yerelleştirilmiş metin | Neden boş olabileceğini açıklar |
| `ActionText` | `string?` | `null` | İsteğe bağlı güvenli sonraki adım |
| `ActionHref` | `string?` | `null` | Link eylemi |
| `OnAction` | `EventCallback` | Atanmamış | İstemci içi eylem |

Eylem ancak metinle birlikte ve gerçek bir link/callback sağlandığında çizilir; işlevsiz düğme
gösterilmez.

## Bileşen: ErrorState

### Amaç

İstek veya bağımlılık hatasını stack trace, endpoint detayı ya da klinik içerik sızdırmadan açıklar.

- `role="alert"` ve `aria-live="assertive"` kullanır.
- Teknik exception metni bileşene verilmez.
- Kullanıcıya yalnız güvenli yeniden deneme veya geri dönüş eylemi sunulur.

## Bileşen: ForbiddenState

### Amaç

Kimliği doğrulanmış kullanıcının permission/kapsam nedeniyle içeriğe erişemediğini anlatır.

- `role="status"` ve `aria-live="polite"` kullanır.
- Gizli kaynağın varlığını, sahibini veya klinik içeriğini açıklamaz.
- Bileşen yalnız sunucu kararını gösterir; kendi başına authorization uygulamaz.

## Ortak durum API'si

Dört public bileşen görsel yapıyı `UiStatePanel` üzerinden paylaşır. Override edilen metinler de
güvenli kullanıcı metni olmalıdır.

```razor
<LoadingState />

<EmptyState Title="Henüz randevu yok"
            Description="Seçilen tarih için bir DEMO randevu bulunmuyor."
            ActionText="Genel bakışa dön"
            ActionHref="/" />

<ErrorState ActionText="Yeniden dene" OnAction="ReloadAsync" />

<ForbiddenState ActionText="Genel bakışa dön" ActionHref="/" />
```

## Yerelleştirme

- Varsayılan istemci kültürü `tr-TR`'dir.
- Kullanıcı metinleri `HospitalManagement.UI/Resources/SharedResources.tr.resx` içinde tutulur.
- Razor içinde `IStringLocalizer<SharedResources>` kullanılır; yeni metin doğrudan component içine
  gömülmez.
- Kültür seçimi ve ek diller ileride kullanıcı tercihiyle genişletilebilir.

## Yap / yapma

| Yap | Yapma |
|---|---|
| Loading, empty, error ve forbidden durumlarını ayrı göster. | Boş listeyi hata gibi sunma. |
| Semantic token ve bileşen varyantı kullan. | Sayfa içinde yeni hex renk veya keyfi spacing üretme. |
| Başlığı kısa, açıklamayı güvenli ve eyleme dönük yaz. | Exception, hasta adı veya erişimi reddedilen kaynak detayını gösterme. |
| Klavye focus'unu ve reduced-motion tercihini koru. | Outline'ı alternatifsiz kaldırma veya sonsuz hareketi zorunlu kılma. |
| UI kararını API authorization sonucuna bağla. | Menü gizlendiği için endpoint'in güvende olduğunu varsayma. |
