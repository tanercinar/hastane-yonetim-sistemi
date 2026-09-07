# Yatan Hasta Servis ve Yatak Doluluk Dashboard'u (Occupancy & Ward Dashboard)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G08 — Doluluk ve servis dashboard'u** görevi için geliştirilen hastane geneli ve klinik servis bazlı yatak doluluk metrikleri, bekleyen kabul/transfer ve taburculuk süreçleri ile SignalR gerçek zamanlı (real-time) izleme paneli mimarisini açıklar.

## Mimari ve İş Kuralları

1. **Kapasite ve Doluluk Hesaplama:**
   - **Genel Doluluk Oranı:** Toplam dolu yatak sayısının (`BedStatus.Occupied`), toplam aktif yatak sayısına bölünmesi ile yüzdesel (`%XX.X`) olarak hesaplanır.
   - **Yatak Durum Dağılımı:** `Available` (Müsait), `Occupied` (Dolu), `Cleaning` (Temizlikte), `Maintenance` (Bakımda) durumları servis ve hastane genelinde toplanır.
   - **Süreç Metrikleri:**
     - `PendingAdmissionsCount`: İstek (`Requested`) veya Kabul Edilmiş (`Accepted`) aşamasındaki bekleyen yatış sayısı.
     - `PendingTransfersCount`: İstek veya kabul aşamasındaki bekleyen transfer sayısı.
     - `TodayDischargesCount`: Bugün (UTC başlangıcından itibaren) başarıyla tamamlanmış taburculuk / sevk sayısı.

2. **Gerçek Zamanlı İletişim (SignalR):**
   - Yalnız ilgili Faz 7 klinik permission'larından en az birini taşıyan kullanıcılar SignalR `HospitalHub` üzerindeki `inpatient-staff` grubuna dahil edilir. Rol adı tek başına üyelik vermez; sistem yöneticisi klinik gruba alınmaz.
   - Aşağıdaki olaylar gerçekleştiğinde anlık SignalR olayları fırlatılır:
     - `InpatientBedChanged` (Yatak durumu değiştiğinde)
     - `InpatientAdmissionChanged` (Yatış durumu değiştiğinde)
     - `InpatientTransferChanged` (Transfer durumu değiştiğinde)
     - `InpatientDischargeCompleted` (Taburculuk tamamlandığında)
     - `InpatientDashboardUpdated` (Genel dashboard yenileme sinyali)
   - Olay gövdeleri kaynak/hasta/servis kimliği veya klinik içerik taşımaz; yalnız değişiklik zamanını içeren yenileme sinyalidir.
   - Blazor arayüzü bu olayları dinler ve her olayda, ayrıca bağlantı kesilip tekrar bağlandığında (`Reconnected`), canonical ve yeniden kapsamlanmış REST verisini çeker.

3. **Yetki ve Güvenlik:**
   - Dashboard ve servis listesi `Permission + Resource Scope + Care Relationship/bölüm ataması` ile filtrelenir; hastane geneli toplamlar dahi yalnız erişilebilir servislerden hesaplanır.
   - Gerçek zamanlı SignalR bağlantısı yetkilendirilmiş oturum (`[Authorize]`) ve Faz 7 permission üyeliği gerektirir.

## API Endpoint'leri

- `GET /api/v1/inpatient/dashboard` — Hastane geneli veya filtrelenmiş yatak/servis doluluk dashboard verisi
- `GET /api/v1/inpatient/dashboard/ward/{wardId}` — Belirli bir servisin doluluk dashboard verisi

## Web UI

- `/inpatient/dashboard` — Blazor Doluluk ve Servis Dashboard'u:
  - Canlı SignalR bağlantı rozeti (`Canlı Bağlantı Aktif` / `Gerçek Zamanlı Bağlantı Bekleniyor`).
  - Genel Doluluk Oranı İlerleme Çubuğu ve Kartı.
  - Yatak Kapasitesi ve Durum Dağılımı (Müsait, Dolu, Temizlikte, Bakımda).
  - Bekleyen Kabul ve Transfer Sayacı.
  - Bugün Taburcu Edilenler Sayacı.
  - Klinik Servisler Tablosu (Servis adı, kod, toplam yatak, dolu/müsait/temizlik/bakım dağılımı, doluluk ilerleme çubuğu, aktif hasta sayısı, Pano ve Yatak sayfalarına hızlı erişim).

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/InpatientDashboardDomainTests.cs` (2 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/InpatientDashboardComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/InpatientDashboardIntegrationTests.cs` ve `Phase7InpatientGateIntegrationTests.cs` (kapsamlı doluluk yaşam döngüsü, bölüm dışı erişim reddi, sistem yöneticisi klinik erişim reddi ve kimliksiz SignalR yenileme sözleşmesi)
