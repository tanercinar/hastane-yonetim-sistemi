# Poliklinik ve Randevu Operasyon Panosu

> **FAZ 11 — F11-G02**  
> Bu belge, poliklinik akışı, günlük randevu sayaçları, bekleme kuyruğu, bölüm ve hekim bazlı operasyonel metriklerin sunulduğu poliklinik operasyon panosu altyapısını ve erişim kurallarını açıklar.

---

## 1. Amaç ve Kapsam

Hastane yöneticileri, başhekimlik, poliklinik personeli ve hekimler için güncel randevu durumlarını (`Scheduled`, `CheckedIn`, `InProgress`, `Completed`, `Cancelled`, `NoShow`), bekleme kuyruklarını ve hekim iş yüklerini canlı izleme imkanı sunulur.

### Temel Güvenlik ve Gizlilik Kuralları
- **Hasta Adı / PHI İzolasyonu:** Yönetim ve operasyon özetlerinde hasta adı veya hassas klinik tanı bilgileri gereksiz yere ifşa edilmez.
- **Kapsam Çözümlemesi (Scope Resolution):**
  - Yönetici veya Başhekim rolü (`HospitalManager`, `ChiefMedicalOfficer`) tüm bölümleri ve hekimleri sorgulayabilir.
  - Hekim (`Doctor`), yönetici rolüne sahip değilse varsayılan olarak yalnızca kendi poliklinik iş yükünü ve metriklerini görüntüler.

---

## 2. Mimari Bileşenler

- **Servis:** `IOutpatientDashboardService` ve `OutpatientDashboardService` (`Reporting` modülü)
  - `GetSummaryAsync`: Toplam randevu, bekleyen kuyruk, muayenede, tamamlanan ve iptal/gelmedi özet metrikleri.
  - `GetDepartmentMetricsAsync`: Bölüm bazında detaylı randevu durum dağılımı.
  - `GetDoctorMetricsAsync`: Hekim bazında tamamlanan ve bekleyen hasta iş yükü metrikleri.
- **API Uç Noktaları:**
  - `GET /api/v1/reporting/dashboards/outpatient/summary`
  - `GET /api/v1/reporting/dashboards/outpatient/departments`
  - `GET /api/v1/reporting/dashboards/outpatient/doctors`
- **İstemci:**
  - `IReportingApiClient` & `ReportingApiClient`
  - Blazor sayfası: `src/HospitalManagement.Web.Client/Pages/Reporting/OutpatientDashboard.razor`

---

## 3. Test ve Doğrulama

- **Unit Tests:** `OutpatientDashboardServiceTests` (3/3 PASS).
- **Integration Tests:** `ReportingIntegrationTests.OutpatientDashboardEndpointsReturnSummaryAndBreakdowns` (PASS).
