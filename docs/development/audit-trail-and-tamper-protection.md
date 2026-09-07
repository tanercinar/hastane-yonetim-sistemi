# Denetim İzi ve Kurcalama Koruması (F02-G06)

Bu belge, Hastane Yönetim Sistemi projesinde uygulanan denetim izi (`Audit Log`), salt-eklenir (`Append-Only`) veritabanı politikası ve kriptografik kurcalama koruması (`Tamper Detection`) standartlarını açıklar.

## 1. Mimari Kararlar ve Mahremiyet İlkeleri

Yetkilendirme, denetim ve mahremiyet mimarisi [`docs/adr/ADR-0001-modular-monolith.md`](../adr/ADR-0001-modular-monolith.md), [`docs/privacy/data-classification.md`](../privacy/data-classification.md) ve [`docs/security/authorization-matrix.md`](../security/authorization-matrix.md) belgelerine dayanır:

- **Salt-Eklenir (Append-Only) Denetim İzi:** `audit_privacy.audit_logs` tablosundaki kayıtlar değiştirilemez ve silinemez. `AuditPrivacyDbContext` seviyesinde `Modified` ve `Deleted` durumundaki varlık girişleri `InvalidOperationException` ile reddedilir.
- **Kriptografik Kurcalama Koruması (Tamper Detection):** Her denetim kaydı oluşturulurken aktör, eylem, kaynak, hedef, sonuç, zaman damgası ve önceki kaydın hash'i (`PreviousRecordHash`) kullanılarak SHA-256 hash imzası (`RecordHash`) üretilir. `VerifyTamperIntegrityAsync` metodu ve `/api/v1/audit/integrity-check/{id}` uç noktası kaydın bütünlüğünü matematiksel olarak doğrular.
- **Klinik İçerik ve Gizli Veri İzolasyonu:** Denetim izi hedef kaynağın tipini (`target_resource_type`) ve kimliğini (`target_resource_id`) taşır; tanı, reçete metni, klinik not içeriği, parola veya token gibi C3/C4 verileri denetim kayıtlarına asla kopyalanmaz.

## 2. Denetim Modeli ve Olay Kataloğu

| Alan | Tip | Açıklama |
|---|---|---|
| `Id` | `Guid` | Kayıt tekil kimliği |
| `CreatedAtUtc` | `DateTime` | Olay zaman damgası (UTC, milisaniye hassasiyetli) |
| `ActorUserId` | `Guid?` | İşlemi yapan kullanıcı kimliği |
| `ActorPersonId` | `Guid?` | İşlemi yapan personel/hasta kişi kimliği |
| `ActorRole` | `string?` | İşlem anındaki kullanıcı rolü |
| `ActorIpAddress` | `string?` | İstemci IP adresi |
| `ActorUserAgent` | `string?` | İstemci tarayıcı / cihaz bilgisi |
| `Action` | `string` | Gerçekleştirilen eylem (`AuditAction`) |
| `TargetResourceType`| `string` | Erişilen / değiştirilen kaynak türü (`User`, `Patient`, `Encounter`, `Prescription`) |
| `TargetResourceId` | `string` | Erişilen / değiştirilen kaynağın kimliği |
| `Outcome` | `AuditOutcome` | İşlem sonucu (`Success`, `Forbidden`, `Failed`, `Warning`) |
| `Reason` | `string?` | İptal/red gerekçesi veya hata açıklaması |
| `CorrelationId` | `string` | İstek izleme kimliği |
| `RecordHash` | `string` | SHA-256 bütünlük imzası |
| `PreviousRecordHash`| `string?` | Zincirleme bütünlük için önceki kaydın hash'i |

## 3. Uç Noktalar

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/audit/logs` | `audit.technical.view` | Filtrelenebilir ve sayfalanabilir denetim loglarını listeler |
| `GET` | `/api/v1/audit/integrity-check/{id}` | `audit.technical.view` | Belirtilen denetim kaydının hash bütünlüğünü doğrular |

## 4. Güvenlik ve Bütünlük Testleri

1. **Birim Testleri:** `AuditLogUnitTests` altında `AuditLogEntry` hash hesaplama / tahrifat tespiti ve `AuditPrivacyDbContext` üzerinde update/delete engelleme doğrulanır.
2. **Entegrasyon Testleri:** `AuditLogIntegrationTests` ile PostgreSQL Testcontainers üzerinde `UserLogin` logu üretimi, `audit.technical.view` izni olmayan kullanıcıların `403 Forbidden` alması, yetkili yöneticinin logları ve kurcalama kontrolünü sorgulayabilmesi kanıtlanır.
