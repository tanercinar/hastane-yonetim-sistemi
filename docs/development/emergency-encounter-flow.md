# Acil Encounter Akışı Geliştirme Notları

Bu belge, **F08-G03 — Acil encounter akışı** görevinin mimari tasarımını, hızlı tetkik istemlerini, konsültasyon sürecini, disposition karar mekanizmasını ve test kapsamını açıklar.

> **Bağımsız inceleme notu (31 Ağustos 2026):** Görev yeniden açılmıştır. `EmergencyCareOrder` ile `EmergencyConsultation`, mevcut Diagnostics `DiagnosticOrder` ve Clinical Records `ConsultationRequest` yaşam döngülerini kopyalamaktadır. Kabul için acil ekranı kanonik servisleri kullanmalı; ikinci sonuç/konsültasyon kaynağı kaldırılmalı veya yalnız kanonik kayda referans veren bir projection'a dönüştürülmelidir.

---

## 1. Mimari ve Kapsam

Acil Encounter Akışı (`EmergencyEncounterFlow`), acil servise kabul edilmiş ve triyajı tamamlanmış hastaların klinik yönetim süreçlerini kapsar:
- **Hızlı Tetkik & Tedavi İstemleri (`EmergencyCareOrder`):** Laboratuvar (Hemogram, Biyokimya, Troponin, D-Dimer), Radyoloji (EKG, PA Akciğer Grafisi, Toraks BT, Batın USG), İlaç/Serum ve Hemşirelik İstemleri.
- **Konsültasyon İstemi & Yanıtı (`EmergencyConsultation`):** Acil branş konsültasyonları (Kardiyoloji, Genel Cerrahi, Nöroloji, Anestezi), aciliyet seviyeleri (`Immediate15Min`, `Urgent60Min`, `Routine`), konsültan hekim kabul ve yanıt notları.
- **Acil Sonlandırma Kararı (`EmergencyDisposition`):** Şifa ile taburcu (`DischargeHome`), servise yatış (`AdmitToWard`), yoğun bakıma yatış (`AdmitToIcu`), ameliyathaneye acil sevk (`DirectToSurgery`), dış merkeze sevk (`TransferToOtherHospital`) veya vefat (`Exitus`). Zorunlu hekim epikrizi/özeti ve taburculuk/reçete talimatları.

### Temel Prensipler
- **Ayrı Kopya Hasta/Sonuç Modeli Oluşturulmaz:** Mevcut `PatientId` üzerinden doğrudan ilişkilendirme sağlanır.
- **Denetim İzi & Mahremiyet:** Tüm istem oluşturma/sonuçlandırma/iptal, konsültasyon istem/kabul/yanıt ve disposition kararları `AuditEventPublisher` ile denetim izine kaydedilir.
- **Durum ve Concurrency Koruması:** Sonlandırılmış (`Discharged`, `AdmittedToInpatient`, `AdmittedToIcu`, `TransferredOut`, `Deceased`) başvurulara yeni istem veya konsültasyon eklenmesi atomik olarak engellenir (`409 Conflict`).

---

## 2. API Uç Noktaları

| Metot | Yol | Yetki | Açıklama |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/emergency/orders` | `emergency.triage.record` | Acil hastası için hızlı tetkik/tedavi istemi oluşturur. |
| `POST` | `/api/v1/emergency/orders/{id}/complete` | `emergency.triage.record` | İstem sonucunu kaydeder ve istemi tamamlar. |
| `POST` | `/api/v1/emergency/orders/{id}/cancel` | `emergency.triage.record` | İptal gerekçesiyle istemi iptal eder. |
| `GET` | `/api/v1/emergency/orders/by-admission/{admissionId}` | Giriş Yapmış Kullanıcı | Başvuruya ait tüm istemleri listeler. |
| `POST` | `/api/v1/emergency/consultations` | `emergency.triage.record` | Branş konsültasyonu talep eder. |
| `POST` | `/api/v1/emergency/consultations/{id}/accept` | `emergency.triage.record` | Konsültan hekim konsültasyonu kabul eder. |
| `POST` | `/api/v1/emergency/consultations/{id}/respond` | `emergency.triage.record` | Konsültasyon yanıtını ve klinik önerilerini kaydeder. |
| `POST` | `/api/v1/emergency/consultations/{id}/cancel` | `emergency.triage.record` | Konsültasyon isteğini iptal eder. |
| `GET` | `/api/v1/emergency/consultations/by-admission/{admissionId}` | Giriş Yapmış Kullanıcı | Başvuruya ait konsültasyonları listeler. |
| `POST` | `/api/v1/emergency/admissions/{id}/disposition` | `emergency.triage.record` | Zorunlu klinik epikrizle disposition kararını kaydeder ve başvuruyu sonuçlandırır. |

---

## 3. Kullanıcı Arayüzü

- Sayfa: `src/HospitalManagement.Web.Client/Pages/Emergency/EmergencyEncounterFlow.razor` (`/emergency/encounter/{AdmissionId}`)
- Bileşenler:
  - Protokol, triyaj seviyesi, vital bulgular ve şikâyeti içeren üst hasta bilgi kartı.
  - Hızlı İstem Kataloğu (STAT Hemogram, STAT Biyokimya, STAT Troponin, STAT EKG, PA Grafi, Toraks BT, Serum).
  - Canlı istem tablosu, sonuçlandırma ve iptal modalleri.
  - Branş konsültasyon talep ve yanıt modalleri.
  - Zorunlu epikrizli ve taburculuk reçete talimatlı Disposition formu.

---

## 4. Doğrulama ve Testler

- **Birim Testleri (`EmergencyEncounterFlowDomainTests.cs`):**
  - İstem ve konsültasyon yaşam döngüsü ve durum geçişleri (`Ordered` -> `InProgress` -> `Completed`, `Requested` -> `Accepted` -> `Completed`).
  - Disposition kararının başvuru durumunu (`AdmittedToIcu`, `AdmittedToInpatient`, `Discharged`) güncellemesi ve tamamlama zamanını set etmesi.
  - Concurrency ve validasyon testleri.
- **Bileşen Testleri (`EmergencyEncounterComponentTests.cs`):**
  - Hasta banner'ı, vital parametreleri, hızlı tetkik sekmesi ve konsültasyon bileşenlerinin render testi.
- **Entegrasyon Testleri (`EmergencyEncounterFlowIntegrationTests.cs`):**
  - Gerçek PostgreSQL üzerinde tam akış: Acil başvuru -> Kırmızı 1 triyaj -> STAT Troponin ve EKG istemi -> Troponin tamamlama ve EKG iptali -> Kardiyoloji konsültasyon isteği -> Konsültan yanıtı -> KBYÜ yatış disposition'ı -> Başvurunun `AdmittedToIcu` olarak sonuçlandırılması.
