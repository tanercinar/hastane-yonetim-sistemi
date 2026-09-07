# Tanısal Hizmetler Operasyon Panosu

Bu belge, **F11-G03** görevi kapsamında uygulanan laboratuvar ve radyoloji tanısal hizmetleri operasyon panosu mimarisini, okuma modellerini, API uç noktalarını ve güvenlik/yetkilendirme ilkelerini açıklar.

## 1. Amaç ve Kapsam

Tanısal hizmetler panosu, hastanenin laboratuvar (biyokimya, mikrobiyoloji, hematoloji, patoloji vb.) ve radyoloji (röntgen, tomografi, MR, ultrason) birimlerindeki operasyonel akışı gerçek zamanlı özetler.

- **Kapsam:**
  - Toplam istem adedi, bekleyen numuneler, cihazda/işlemde olan testler, sonuçlanan tetkikler.
  - Ortalama sonuç verme süresi (Turnaround Time - TAT).
  - Kritik sonuç alarmları ve modalite bazlı kritik durum sayaçları.
  - Klinik içerikten arındırılmış yönetim düzeyinde metrik görünümü (hasta kimliği veya klinik rapor metni panoda açıkça yer almaz).
  - Klinik detaylara drill-down için ek klinik izinler (`HospitalPermissions.Diagnostics.OrderView` / `ReportView`).

## 2. Mimari Bileşenler

```
[Diagnostic Order/Report Events]
              │
              ▼
   [IReportingProjectionEngine]
              │
              ▼
  [DiagnosticWorkloadMetrics] (PostgreSQL reporting şeması)
              │
              ▼
  [IDiagnosticDashboardService] (Aggregates & Workload Metrics)
              │
              ▼
[REST Endpoints /api/v1/reporting/dashboards/diagnostics/*]
              │
              ▼
[Blazor DiagnosticDashboard.razor & IReportingApiClient]
```

## 3. API Uç Noktaları

Tüm uç noktalar `report.operations.view` izni ve kimlik doğrulama gerektirir:

| Metod | Uç Nokta | Açıklama |
|---|---|---|
| `GET` | `/api/v1/reporting/dashboards/diagnostics/summary?date={date}` | Günlük toplam istem, bekleyen numune, işlemde, sonuçlanan, kritik ve ortalama TAT süresi |
| `GET` | `/api/v1/reporting/dashboards/diagnostics/modalities?date={date}` | Bölüm / modalite bazlı iş yükü, sonuç ve TAT kırılımı |
| `GET` | `/api/v1/reporting/dashboards/diagnostics/critical-alerts?date={date}` | Kritik sonuç üreten modalite alarmları ve son güncelleme zamanları |

## 4. Güvenlik ve Gizlilik Prensipleri

1. **PHI İfşa Koruması:** Yönetim paneli yalnızca sayaçları ve modalite/bölüm adlarını gösterir. Tetkik sonuçları, hasta adı veya TC kimlik numarası bu panoda döndürülmez.
2. **Drill-Down Ayrımı:** Kritik sonuç bildirimine veya tetkik ayrıntısına tıklayan kullanıcılar, Tanı Modülü'nün kendi yetki zincirine (`HospitalPermissions.Diagnostics.OrderView`, `ReportView`) tabi tutulur.
3. **Sentetik Demo Verisi:** Yalnızca sentetik `DEMO` kayıtları ve test olayları kullanılır.
