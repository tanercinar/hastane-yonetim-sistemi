# Raporlama Okuma Modelleri ve Projeksiyon Yapısı

> **FAZ 11 — F11-G01**  
> Bu belge, Hastane Yönetim Sistemi (HMS) operasyonel panoları ve analitik raporlamaları için geliştirilen modüller arası okuma modelleri (read models), projeksiyon motoru (projection engine), idempotent olay tüketimi ve projeksiyon yeniden kurma (rebuild) altyapısını açıklar.

---

## 1. Genel Bakış ve Mimari Tasarım

ADR-0001 (Modüler Monolit) uyarınca, modüller birbirlerinin tablolarına, `DbContext` yapılarına veya domain entity'lerine doğrudan erişemez. Çapraz modül veri birleştirme ve operasyonel metrik hesaplamaları **Reporting** modülünün kendi `reporting` PostgreSQL şemasında barındırdığı optimize edilmiş okuma modelleri üzerinden yürütülür.

### Temel İlkeler
1. **Idempotency (Tekillik / Çift İşleme Koruması):** Modül olayları (`AppointmentProjectedEvent`, `DiagnosticProjectedEvent`, `BedOccupancyProjectedEvent`, `PharmacyProjectedEvent`) `EventId` üzerinden tekilleştirilir. Aynı olay birden fazla kez gelse dahi sayaçlar mükerrer artırılmaz (`ProjectionProcessedEvent` kaydı).
2. **Rebuildability (Yeniden Kurulabilirlik):** Projeksiyonlar istenildiğinde API üzerinden (`POST /api/v1/reporting/projections/rebuild`) tek tek veya topluca sıfırlanıp yeniden inşa edilebilir (`IProjectionRebuilder`).
3. **Checkpoint ve Gecikme Takibi:** Her projeksiyon türü için anlık pozisyon, son güncellenme zamanı ve durum (`Active`, `Rebuilding`, `Error`) `projection_checkpoints` tablosunda tutulur (`IReportingReadModelService.GetCheckpointsAsync`).
4. **Veri Minimizasyonu ve Yetkilendirme:** Raporlama modelleri klinik içerikleri gereksiz ifşa etmez. API uç noktaları `HospitalPermissions.ReportingAndAudit.ReportOperationsView` izni ve anti-forgery koruması ile güvence altındadır.

---

## 2. Okuma Modelleri (Read Models)

`reporting` PostgreSQL şemasında yer alan tablolar:

| Tablo Adı | Domain Modeli | Açıklama |
|---|---|---|
| `daily_outpatient_metrics` | `DailyOutpatientMetric` | Günlük randevu sayıları, randevu durumları (`Scheduled`, `CheckedIn`, `InProgress`, `Completed`, `Cancelled`, `NoShow`), poliklinik ve hekim bazlı operasyonel metrikler. |
| `diagnostic_workload_metrics` | `DiagnosticWorkloadMetric` | Laboratuvar ve radyoloji modalite/bölüm bazlı sipariş, numune, işlemde, onaylanan, kritik sonuç sayıları ve ortalama tamamlanma süresi (`AvgTurnaroundMinutes`). |
| `bed_occupancy_metrics` | `BedOccupancyMetric` | Servis ve koğuş bazlı toplam yatak, dolu yatak, boş yatak, bekleyen transfer sayıları ve anlık doluluk oranı yüzdesi (`OccupancyRatePercentage`). |
| `pharmacy_dispensing_metrics` | `PharmacyDispensingMetric` | Günlük toplam reçete, bekleyen karşılama, karşılanan, kritik düşük stoklu ilaç ve son kullanma yaklaşan lot sayıları. |
| `projection_checkpoints` | `ProjectionCheckpoint` | Projeksiyon bazlı pozisyon, durum ve zaman damgası kontrol noktaları. |
| `projection_processed_events` | `ProjectionProcessedEvent` | Olay tekilleştirme ve idempotency güvencesi tablosu. |

---

## 3. Servis Sözleşmeleri

- **`IReportingProjectionEngine`**:
  - `ProjectAppointmentEventAsync(AppointmentProjectedEvent evt)`
  - `ProjectDiagnosticEventAsync(DiagnosticProjectedEvent evt)`
  - `ProjectBedOccupancyEventAsync(BedOccupancyProjectedEvent evt)`
  - `ProjectPharmacyEventAsync(PharmacyProjectedEvent evt)`
- **`IProjectionRebuilder`**:
  - `RebuildAllProjectionsAsync()`
  - `RebuildProjectionAsync(string projectionName)`
- **`IReportingReadModelService`**:
  - `GetOutpatientMetricsAsync(startDate, endDate, departmentId, doctorId)`
  - `GetDiagnosticMetricsAsync(startDate, endDate, modalityOrSection)`
  - `GetBedOccupancyMetricsAsync(targetDate, departmentId, wardType)`
  - `GetPharmacyMetricsAsync(startDate, endDate)`
  - `GetCheckpointsAsync()`

---

## 4. REST API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/reporting/projections/rebuild` | `report.operations.view` | Projeksiyonları sıfırlama ve yeniden kurma komutu (Anti-forgery zorunlu). |
| `GET` | `/api/v1/reporting/projections/checkpoints` | `report.operations.view` | Projeksiyon kontrol noktalarını ve anlık durumlarını listeler. |
| `GET` | `/api/v1/reporting/metrics/outpatient` | `report.operations.view` | Poliklinik ve randevu operasyon metriklerini sorgular. |
| `GET` | `/api/v1/reporting/metrics/diagnostics` | `report.operations.view` | Laboratuvar ve radyoloji iş yükü metriklerini sorgular. |
| `GET` | `/api/v1/reporting/metrics/occupancy` | `report.operations.view` | Yatak doluluk metriklerini sorgular. |
| `GET` | `/api/v1/reporting/metrics/pharmacy` | `report.operations.view` | Eczane karşılama ve stok seviyesi metriklerini sorgular. |

---

## 5. Test Kapsamı ve Doğrulama

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Reporting/` (12/12 PASS)
  - `ReportingDomainTests`: Domain hesaplamaları, sayaç artırım/azaltımları, durum geçişleri, doluluk oranı ve kontrol noktası durumları.
  - `ProjectionEngineAndRebuilderTests`: Idempotency ve çift işleme testleri, projeksiyon sıfırlama ve yeniden kurma yaşam döngüsü.
- **Architecture Tests:** `tests/HospitalManagement.ArchitectureTests/` (13/13 PASS)
  - Reporting modülünün domain ve uygulama katmanlarının katman sınırlarına ve modüler monolit kurallarına tam uyumu.
- **Integration Tests:** `tests/HospitalManagement.IntegrationTests/ReportingIntegrationTests.cs` (5/5 PASS)
  - Gerçek PostgreSQL ve Testcontainers üzerinde migration çalıştırma, idempotency doğrulaması, REST API uç noktaları ve yetkisiz rollere karşı `403 Forbidden` koruması.
