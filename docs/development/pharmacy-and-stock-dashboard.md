# Eczane ve İlaç Stok Operasyon Panosu

Bu belge, **F11-G05** görevi kapsamında hayata geçirilen eczane karşılama akışı, bekleyen reçete sayaçları, asgari stok eşikleri ve son kullanma tarihi (SKT / miat) yaklaşan lot uyarıları mimarisini açıklar.

## 1. Amaç ve Kapsam

Hastane eczanesindeki operasyonel ilaç karşılama ve stok akışını izlemek.

- **Kapsam:**
  - Günlük toplam reçete, bekleyen ilaç çıkışı ve teslim edilen reçete sayaçları.
  - Asgari emniyet stok seviyesinin altına inen ilaç kalemleri (Kritik Düşük Stok uyarısı).
  - Son kullanma tarihi yaklaşan demo lotları (Miat Yaklaşan Lot uyarısı).
  - **Değişmez Kural:** Finansal tutar, tedarik zinciri, satın alma veya faturalama özellikleri kesinlikle eklenmez. Yalnızca operasyonel miktar ve birim bilgisi tutulur.

## 2. Mimari Bileşenler

```
[Pharmacy / Dispensing Events]
              │
              ▼
  [IReportingProjectionEngine]
              │
              ▼
  [PharmacyDispensingMetrics] (PostgreSQL reporting şeması)
              │
              ▼
   [IPharmacyDashboardService] (Aggregates & Stock Alerts)
              │
              ▼
[REST Endpoints /api/v1/reporting/dashboards/pharmacy/*]
              │
              ▼
[Blazor PharmacyInventoryDashboard.razor & IReportingApiClient]
```

## 3. API Uç Noktaları

Tüm uç noktalar `report.operations.view` yetkisi gerektirir:

| Metod | Uç Nokta | Açıklama |
|---|---|---|
| `GET` | `/api/v1/reporting/dashboards/pharmacy/summary?date={date}` | Günlük toplam reçete, bekleyen çıkış, tamamlanan, düşük stok ve miat sayaçları |
| `GET` | `/api/v1/reporting/dashboards/pharmacy/stock-alerts?date={date}` | Kritik stok ve SKT yaklaşan lot alarmları (yalnızca miktar ve birim) |

## 4. Güvenlik ve Uyumluluk İlkeleri

1. **Finansal Bilgi Yasağı:** Stok alarmlarında ilaç fiyatı, ihale/tedarikçi bilgisi veya parasal büyüklük yer almaz.
2. **Kişisel Veri Yalıtımı:** Reçete özetlerinde hasta adı, reçete metni veya hekim kaşesi bulunmaz; sadece operasyonel adetler raporlanır.
3. **Sentetik Demo Verisi:** Sentetik `DEMO-*` ilaç tanımları ve yapay lotlar kullanılır.
