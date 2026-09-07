# Yatan Hasta Klinik Panosu (Inpatient Clinical Board)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G04 — Yatan hasta klinik panosu** görevi için geliştirilen servis bazlı hasta izlem panosu, risk/uyarı göstergeleri, izolasyon takibi ve klinik özet akışını açıklar.

## Mimari ve Kapsam Kuralları

1. **Minimum Gerekli Veri Görünürlüğü (Data Minimization):**
   - Klinik pano yalnızca bakım ekibinin hastayı güvenle tanıması, yatak konumunu görmesi, klinik riskleri ve diyet/izolasyon kısıtlarını izlemesi için gereken özet veriyi sunar.
   - Hassas tam kimlik yerine sentetik ve maskeli tanıtıcılar kullanılır (`DEMO-P-...`, `DEMO Hasta ...`).

2. **Servis ve Bölüm Bazlı Filtreleme (Care Scoping):**
   - Hekim ve hemşireler servise veya bölüme göre filtreleme yaparak yalnızca kendi sorumluluk alanlarındaki hastaları izler.
   - İlgisiz bölümlere ait hasta kalabalığı filtrelenir.

3. **Klinik Risk ve Uyarı Göstergeleri:**
   - **Düşme Riski (Fall Risk):**
     - Skor $\ge 50$: Yüksek Risk (`High` - Kırmızı uyarı)
     - Skor $25 - 49$: Orta Risk (`Medium` - Sarı uyarı)
     - Skor $< 25$: Düşük Risk (`Low` - Yeşil)
   - **İzolasyon Durumu (Isolation Required):** `None`, `Contact`, `Droplet`, `Airborne`, `Protective` rozetleri ile enfeksiyon kontrolü.
   - **Transfer Süreci:** Aktif onay/tamamlama bekleyen transferi olan hastalar panoda `Transfer Sürecinde` rozeti ile vurgulanır.

## API Endpoint'leri

- `GET /api/v1/inpatient/board` — Yatan hasta klinik panosu listesi (`wardId`, `departmentId`, `riskLevel`, `isolationOnly` filtreleri ile)
- `GET /api/v1/inpatient/board/{admissionId}/summary` — Tekil hasta klinik özeti

## Web UI

- `/inpatient/board` — Blazor klinik servis panosu sayfası:
  - Üst istatistik sayaçları (Toplam Yatan, Yüksek Düşme Riski, İzolasyonlu, Transfer Bekleyen).
  - Servis ve risk düzeyi filtreleri, izolasyon switch'i.
  - Kart ve oda/yatak bazlı görselleştirme.
  - Detaylı klinik özet modali ve transfer kısayolları.

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/BoardServiceTests.cs` (7 test — Düşme riski sınıflandırma eşikleri ve DTO veri bütünlüğü)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/InpatientBoardComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/InpatientBoardIntegrationTests.cs` (1 test — Yatış kabulü -> Yatağa yatırma -> Servis bazlı filtreleme -> Risk seviyesi filtreleme -> Klinik özet doğrulama)
