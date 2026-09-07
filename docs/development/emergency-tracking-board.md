# Acil Takip Panosu Geliştirme Notları

Bu belge, **F08-G02 — Acil takip panosu** görevinin mimari tasarımını, SignalR gerçek zamanlı bildirim altyapısını, KPI hesaplamalarını ve test kapsamını açıklar.

---

## 1. Mimari ve Kapsam

Acil Takip Panosu (`EmergencyTrackingBoard`), acil servisteki hastaların durumlarını (Triyaj Bekleyen, Hekim Bekleyen, Değerlendirmede, Gözlemde, Yatış Bekleyen, Taburcu) ve triyaj önceliklerini gerçek zamanlı olarak izlemek, bekleme sürelerini analiz etmek ve acil servis personelinin hızlı aksiyon almasını sağlamak üzere tasarlanmıştır.

### Temel Prensipler
- **Minimum Veri İlkesi & Rol Kapsamı:** Pano, hasta mahremiyetini korumak adına yalnız acil servisle ilişkili klinik ve operasyonel verileri (protokol no, geliş şekli, triyaj kategorisi ve gerekçesi, atanan alan/yatak, bekleme süresi) sunar.
- **Gerçek Zamanlı SignalR Senkronizasyonu:** `HospitalHub` üzerinden `emergency-staff` grubuna gönderilen olaylarla (`EmergencyAdmissionCreated`, `EmergencyTriageRecorded`, `EmergencyDoctorAssigned`, `EmergencyStatusChanged`, `EmergencyDashboardUpdated`) pano anlık yenilenir.
- **Triyaj Öncelikli Sıralama:** İş listesi öncelik ağırlığına göre sıralanır: `Red1Resuscitation` (1) > `Red2Emergency` (2) > `YellowUrgent` (3) > `GreenStandard` (4) > `BlackExpectant` (5), ardından en uzun bekleyen hastalar öne çıkar.

---

## 2. API Uç Noktaları

| Metot | Yol | Yetki | Açıklama |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/emergency/board/summary` | Giriş Yapmış Kullanıcı | Aktif hasta sayısı, triyaj dağılımı, durum sayaçları ve ortalama bekleme süreleri (KPI). |
| `GET` | `/api/v1/emergency/board/worklist` | Giriş Yapmış Kullanıcı | Triyaj öncelikli, alan/durum filtrelenebilir canlı acil iş listesi. |

---

## 3. KPI ve Metrik Hesaplamaları

- **Ortalama Triyaj Bekleme Süresi (`AverageWaitMinutesTriage`):** `WaitingTriage` durumundaki hastaların başvuru anından (`AdmittedAtUtc`) itibaren geçen ortalama dakika.
- **Ortalama Hekim Bekleme Süresi (`AverageWaitMinutesDoctor`):** `TriagedWaitingDoctor` durumundaki hastaların triyaj anından (`TriagedAtUtc`) itibaren geçen ortalama dakika.
- **Ortalama Acil Kalış Süresi (`AverageLengthOfStayMinutes` - LOS):** Tüm aktif acil hastalarının serviste geçirdiği ortalama dakika.
- **Bekleme Süresi Görsel İndikatörleri:**
  - 🟢 Normal: &lt; 30 dk
  - 🟡 Orta: 30 - 60 dk
  - 🔴 Kritik / Uzun: &gt; 60 dk

---

## 4. Kullanıcı Arayüzü

- Sayfa: `src/HospitalManagement.Web.Client/Pages/Emergency/EmergencyTrackingBoard.razor` (`/emergency/board`)
- Bileşenler:
  - Canlı SignalR bağlantı durumu rozeti ve otomatik yeniden bağlanma desteği.
  - 6'lı üst KPI özet kartları (Aktif, Triyaj Bekleyen, Hekim Bekleyen, Değerlendirmede, Gözlemde, Bugün Taburcu).
  - 5'li triyaj seviyesi dağılım filtre butonları (Kırmızı 1, Kırmızı 2, Sarı, Yeşil, Siyah).
  - Alan bazlı (Kırmızı Alan, Sarı Alan, Yeşil Alan, Resüsitasyon Odası), durum bazlı ve serbest metin arama filtre çubuğu.
  - Hızlı aksiyon modalleri: Triyaj Giriş/Güncelleme, Hekim/Alan Atama, Durum Güncelleme.

---

## 5. Doğrulama ve Testler

- **Birim Testleri (`EmergencyTrackingBoardDomainTests.cs`):**
  - KPI metriklerinin ve ortalama bekleme sürelerinin matematiksel doğrulaması.
  - Triyaj seviyesi öncelik sıralaması ve alan/triyaj filtreleme testleri.
- **Bileşen Testleri (`EmergencyTrackingBoardComponentTests.cs`):**
  - Blazor arayüzünün KPI sayaçlarını, protokol kartlarını ve iş listesini render testi.
- **Entegrasyon Testleri (`EmergencyTrackingBoardIntegrationTests.cs`):**
  - Gerçek PostgreSQL üzerinde tam yaşam döngüsü: Başvuru oluşturma -> Pano sayaç artışı -> Triyaj kaydı -> Kırmızı seviye sayaç artışı -> Alan atama -> Alana göre filtreleme -> Taburculuk ve aktif liste temizliği.
