# Mimari Karar Kayıtları

ADR'ler önemli ve değiştirilmesi maliyetli teknik/ürün kararlarını kaydeder. Kabul edilmiş bir ADR sessizce düzenlenmez; karar değişecekse yeni ADR öncekinin yerini alır (`Supersedes`) ve önceki kayıt `Superseded` yapılır.

| ADR | Karar | Durum |
|---|---|---|
| [ADR-0001](ADR-0001-modular-monolith.md) | Modüler monolit ve modül sınırları | Accepted |
| [ADR-0002](ADR-0002-api-first-blazor-clients.md) | API-öncelikli Blazor istemci modeli | Accepted |
| [ADR-0003](ADR-0003-postgresql-ef-core.md) | PostgreSQL 18 ve EF Core 10 | Accepted |
| [ADR-0004](ADR-0004-identity-and-native-oidc.md) | Web kimliği ve native OIDC geçişi | Accepted |
| [ADR-0005](ADR-0005-clinical-record-integrity.md) | Klinik kayıt değişmezliği/düzeltme modeli | Accepted |
| [ADR-0006](ADR-0006-synthetic-data-only.md) | Yalnız sentetik veri politikası | Accepted |

## Yeni ADR biçimi

Her ADR en az şunları içerir: durum, tarih, bağlam, karar, alternatifler, olumlu/olumsuz sonuçlar, uygulama korumaları ve yeniden değerlendirme koşulları.
