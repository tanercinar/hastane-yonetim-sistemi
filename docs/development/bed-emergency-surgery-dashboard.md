# Yatak, Acil ve Ameliyathane Operasyon Panosu

Bu belge, **F11-G04** görevi kapsamında hayata geçirilen klinik servis yatak dolulukları, bekleyen hasta transferleri, acil triyaj grupları ve ameliyathane kullanım panosu mimarisini açıklar.

## 1. Amaç ve Kapsam

Hastanedeki yatak kapasitesini ve acil başvuru yükünü operasyonel seviyede gerçek zamanlı sunarak yatak tahsis ve transfer kararlarını desteklemek.

- **Kapsam:**
  - Genel ve servis bazlı toplam, dolu, müsait yatak sayaçları ve doluluk yüzdesi.
  - Servisler arası veya acilden servise bekleyen hasta transferleri.
  - Acil triyaj bekleme grupları (Kırmızı / Sarı / Yeşil kategorileri).
  - Ameliyathane ve yoğun bakım doluluk/kullanım göstergeleri.
  - Kişisel sağlık verisi (hasta adı, teşhis vb.) içermeyen salt operasyonel sayaç görünümü.

## 2. Mimari Bileşenler

```
[Inpatient / Emergency Events]
              │
              ▼
  [IReportingProjectionEngine]
              │
              ▼
    [BedOccupancyMetrics] (PostgreSQL reporting şeması)
              │
              ▼
[IInpatientOperationsDashboardService] (Aggregates & Wards)
              │
              ▼
[REST Endpoints /api/v1/reporting/dashboards/inpatient-operations/*]
              │
              ▼
[Blazor InpatientOperationsDashboard.razor & IReportingApiClient]
```

## 3. API Uç Noktaları

Tüm uç noktalar `report.operations.view` yetkisi gerektirir:

| Metod | Uç Nokta | Açıklama |
|---|---|---|
| `GET` | `/api/v1/reporting/dashboards/inpatient-operations/summary?date={date}` | Günlük toplam/dolu/boş yatak, doluluk %, transfer ve acil sayaçları |
| `GET` | `/api/v1/reporting/dashboards/inpatient-operations/wards?date={date}` | Servis ve bölüm bazında yatak doluluk kırılımı |
| `GET` | `/api/v1/reporting/dashboards/inpatient-operations/emergency-triage?date={date}` | Acil triyaj seviyesi (Kırmızı/Sarı/Yeşil) bekleme grupları ve ortalama süreler |

## 4. Güvenlik Prensipleri

1. **Yalnızca Sayısal ve Anonim Metrik:** Panoda hasta isimleri, protokol numaraları veya tıbbi detaylar yer almaz.
2. **Rol Tabanlı Erişim:** Yalnızca operasyonel raporlama yetkisi (`report.operations.view`) olan personel erişebilir.
3. **Sentetik DEMO Veri:** Canlı testler sentetik `DEMO-*` verileri ve test projeksiyon olayları ile yürütülür.
