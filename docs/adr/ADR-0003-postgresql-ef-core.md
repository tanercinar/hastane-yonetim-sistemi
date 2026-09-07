# ADR-0003: PostgreSQL 18 ve EF Core 10

- **Durum:** Accepted
- **Tarih:** 2026-08-13
- **İlgili:** ADR-0001, `docs/privacy/data-inventory.md`

## Bağlam

Ürün ilişkisel bütünlük, transaction, concurrency, tarih aralığı, JSON metadata ve gerçekçi sorgular gerektirir. Yerel/CI ortamının lisanssız Docker ile kolay kurulması ve ileride farklı platformlarda çalışması istenir. Eğitim projesinde veritabanı erişim katmanının açık, test edilebilir ve C# ekosistemiyle uyumlu olması gerekir.

## Karar

- PostgreSQL 18'in desteklenen güncel minor sürümü kullanılacaktır.
- EF Core 10 ve uyumlu Npgsql provider central package management ile sabitlenecektir.
- Her modül kendi PostgreSQL şeması ve mapping/migration sahipliğini korur; başka modülün tablosuna doğrudan erişmez.
- Migration'lar kaynak kontrolünde ve ileri yönlüdür; production-benzeri ortamda uygulama açılışında otomatik migration yapılmaz.
- Integration testleri gerçek PostgreSQL Testcontainer kullanır; kritik davranışlar EF InMemory ile test edilmez.
- Zaman damgaları UTC `timestamptz`; kullanıcı gösterimi `Europe/Istanbul` olur.
- Teknik anahtar UUID; kullanıcıya görünen iş numarası ayrı unique alandır.
- Kritik aggregate'larda optimistic concurrency token ve veritabanı constraint birlikte kullanılır.
- Randevu/yatak zaman çakışması, stok negatifliği, benzersiz barkod gibi değişmezler yalnız uygulama kontrolüne bırakılmaz.
- Klinik içerik için hard delete varsayılan değildir; durum/sürüm/düzeltme modeli uygulanır.
- Hassas alan şifrelemesi uygulama düzeyinde değerlendirilir; anahtar veritabanından ayrı secret store'da tutulur.

## Alternatifler

### SQL Server

Uygun ve güçlü bir .NET seçeneğidir; ancak platform/lisans bağımsız Docker geliştirme ve portföy taşınabilirliği için PostgreSQL tercih edildi.

### SQLite

Reddedildi: hızlı prototip için yararlı olsa da concurrency, PostgreSQL semantiği ve gerçekçi integration testini temsil etmez. Yerel kalıcı ana veritabanı olmayacaktır.

### MongoDB/NoSQL

Reddedildi: ilişkisel klinik süreç, referential integrity ve transaction ihtiyaçları baskındır.

### Dapper-only

Reddedildi: ince kontrol sağlar fakat migration/change tracking/üretkenlik dengesi tek geliştirici için EF Core lehinedir. Ölçülmüş rapor sorgusunda Dapper/raw SQL sınırlı ve testli kullanılabilir.

## Sonuçlar

### Olumlu

- Güçlü ilişkisel constraint ve transaction.
- Docker/Testcontainers ile tekrarlanabilir yerel ve CI ortamı.
- EF Core migration, LINQ ve change tracking üretkenliği.

### Olumsuz

- PostgreSQL'e özgü constraint/sorgular taşınabilirliği azaltabilir.
- EF Core yanlış kullanılırsa N+1, izleme yükü veya geniş sorgu üretebilir.
- Modül başına migration sırası dikkatle yönetilmelidir.

## Uygulama korumaları

1. Query'lerde projection, pagination ve `AsNoTracking` uygun yerde kullanılır.
2. Her migration integration testinde boş veritabanına uygulanır.
3. Şema değişimi veri kaybı/geri dönüş notu ve rollout planı içerir.
4. Entity/DbContext dış modüle API olarak verilmez.
5. PII/klinik değer SQL/EF logging'de kapalı/redacted olur.
6. Supported minor sürümler Faz 1 ve yayın öncesi yeniden doğrulanır.

## Yeniden değerlendirme koşulları

- Hedef kurum zorunlu veritabanı standardı koyarsa
- PostgreSQL/EF Core desteği yaşam döngüsü dışında kalırsa
- Ölçüm belirli sorgu için alternatif erişim/desen gerektirirse
- Veri yerleşimi veya şifreleme gereksinimi tasarımı değiştirirse
