# Raporlama ve Operasyon Panoları SignalR Gerçek Zamanlı İletişim ve Dayanıklılık (F11-G07)

Bu belge, **F11-G07** görevi kapsamında uygulanan SignalR gerçek zamanlı iletişim mimarisini, departman ve pano grup üyeliği yetkilendirme modelini, olay sıralama (event ordering) mekanizmasını, istemci yeniden bağlanma (reconnection) stratejisini ve projeksiyon gecikme göstergesini (projection lag indicator) açıklar.

## 1. Mimari ve Güvenlik Modeli

### 1.1. Hub ve Grup Yetkilendirme Sınırı (TM-13 Güvenlik Kontrolü)
- **Hub Uç Noktası:** `/hubs/hospital` (`HospitalHub`).
- **Sunucu Taraflı Grup Katılımı (`OnConnectedAsync`):**
  - İstemci bağlantı kurduğunda, sunucu yalnızca oturum açmış kullanıcının güvenli claim'lerine dayanarak gruplara dahil eder:
    - `person-{personId}`: Kullanıcının kendi bildirim kanalı.
    - `department-{departmentId}`: Kullanıcının atanmış olduğu birim (varsa).
    - `reporting-operations`: Yalnızca `report.operations.view` yetkisine sahip kullanıcılar.
- **İstemci Girdisiyle Yetkisiz Bölüm Grubuna Katılma Engeli:**
  - İstemciler `JoinDepartmentGroup(Guid departmentId)` veya `JoinReportingDashboard(string dashboardName)` metotlarını çağırabilir.
  - `HospitalHub.IsAuthorizedForDepartment` sunucu tarafında doğrulanır:
    - Yönetici veya Başhekim rolleri (`SystemAdministrator`, `ChiefMedicalOfficer`, `HospitalManager`) veya `report.operations.view` izni varsa izin verilir.
    - Hekim veya servis personeli yalnızca kendi claim'inde bulunan `department_id` ile eşleşen gruba katılabilir.
    - Yetkisiz bir bölüme katılmaya çalışan istemciye sunucu tarafından `HubException` fırlatılır ve istek engellenir (TM-13 atlama saldırılarına karşı koruma).

### 1.2. Olay Sıralama (Monotonic Event Ordering)
- Ağ gecikmeleri veya eşzamanlı bildirimlerde istemcinin olayları hatalı sırada işlemesini önlemek için, her gerçek zamanlı güncelleme (`ReportingDashboardRealtimeUpdate`) sürece özgü atomik bir sayaç (`Interlocked.Increment`) tarafından üretilen monotonik artan bir `SequenceNumber` taşır.
- İstemciler gelen güncellemenin `SequenceNumber` değerini denetleyerek eski olayları yoksayabilir.

### 1.3. Yeniden Bağlanma (Reconnection) ve Kanonik Durum Eşitlemesi
- Web istemcisi (`Microsoft.AspNetCore.SignalR.Client`), `.WithAutomaticReconnect()` ile yapılandırılmıştır.
- Bağlantı koptuğunda UI, bağlantı durumunu "Yeniden Bağlanıyor" rozetiyle kullanıcıya gösterir ve REST/manuel yenileme modunda çalışmaya devam eder.
- Bağlantı yeniden kurulduğunda (`Reconnected` olayı), aradaki olay kayıplarını telafi etmek için sunucunun REST API uç noktasından kanonik güncel durum tekrar çekilir (`LoadDashboardAsync`).

### 1.4. Projeksiyon Gecikme Göstergesi (Projection Lag Indicator)
- `GET /api/v1/reporting/projections/lag` uç noktası üzerinden `DailyOutpatient`, `DiagnosticWorkload`, `BedOccupancy` ve `PharmacyDispensing` projeksiyonlarının gecikme durumu sorgulanır.
- Her projeksiyon için `LastProcessedTimestampUtc` ve `DateTime.UtcNow` arasındaki fark (`LagSeconds`) ve sağlık durumu (`IsHealthy`: durum `Active` ve gecikme < 300 saniye) hesaplanır.
- Blazor panosu (`OutpatientDashboard.razor`) başlığında "Canlı Senkronize" ve "Gecikme: 0.2s" rozetleri ile canlı sistem durumu personele gösterilir.

## 2. API ve Hub Sözleşmeleri

### 2.1. SignalR Olayı
```json
{
  "dashboardType": "outpatient",
  "sequenceNumber": 42,
  "metricDate": "2026-09-04",
  "departmentId": "b1a2c3d4-...",
  "projectionLagSeconds": 0.0,
  "emittedAtUtc": "2026-09-04T12:00:00Z"
}
```

### 2.2. Projeksiyon Gecikme Yanıtı (`GET /api/v1/reporting/projections/lag`)
```json
[
  {
    "projectionName": "DailyOutpatient",
    "lastProcessedPosition": 128,
    "lastProcessedTimestampUtc": "2026-09-04T11:59:58Z",
    "lagSeconds": 2.15,
    "isHealthy": true,
    "checkedAtUtc": "2026-09-04T12:00:00Z"
  }
]
```

## 3. Doğrulama ve Test Kapsamı
- **Birim Testleri (`ReportingSignalRTests`, `ProjectionLagServiceTests`):**
  - Yönetici, hekim ve personelin bölüm grubu izin doğrulamaları.
  - Yetkisiz bölüm grubu isteğinde `HubException` fırlatılması.
  - Sıra numaralarının monotonik artışı.
  - Projeksiyon gecikme ve sağlık hesaplama kuralları.
- **Entegrasyon Testi (`ProjectionLagEndpointReturnsCheckpointsAndLag`):**
  - Anonim erişimde `401/challenge` doğrulandı.
  - Hekim/operasyon kullanıcısında `200 OK` ve sağlıklı gecikme listesi doğrulandı.
