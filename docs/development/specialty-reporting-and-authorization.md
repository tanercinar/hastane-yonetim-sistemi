# Uzmanlık Raporları, İzinler ve Güvenlik Sınırları (Specialty Reporting and Authorization)

## 1. Amaç ve Kapsam

Bu belge, **Faz 9 — Uzmanlık dikey dilimleri** kapsamında `F09-G05 — Uzmanlık raporları ve izinler` görevinin teknik mimarisini, rol izinlerini, veri minimizasyonu kurallarını ve operasyonel gösterge modellerini açıklar.

Uzmanlık raporlama ve yetkilendirme katmanı; Kadın Doğum (gebelik, doğum modu oranları), Diş Hekimliği (tedavi tamamlama oranları, muayeneler) ve Evde Sağlık (aktif, bekleyen, acil ziyaret sayıları) verilerini anonimleştirilmiş ve gizliliği korunmuş operasyonel göstergeler halinde sunar.

> [!IMPORTANT]
> **Gizlilik ve Güvenlik Güvencesi:** Genel operasyonel raporlar hasta kimliği, açık ev adresi veya hassas tıbbi ayrıntı sızdırmaz (`report.operations.view`). Klinik kayıt yazma/düzeltme izinleri (`specialty-care.record`) yalnızca izinli klinik rollere verilir. `SystemAdministrator` teknik audit ile sınırlıdır; klinik kayda ve operasyonel rapora erişemez (`403 Forbidden`).

---

## 2. Yetki ve Rol Matrisi

| Rol | Rapor Görüntüleme (`report.operations.view`) | Uzmanlık Kayıt (`specialty-care.record`) | Adres/Klinik Detay Görünürlüğü |
|---|---|---|---|
| **Doctor** | Evet | Evet | Evet (Bakım ilişkisi ve atanan hastalar) |
| **Nurse / Midwife** | Evet | Evet (Yetkili olduğu alanlarda) | Evet (Görevlendirildiği ziyaret/doğum) |
| **HospitalManager** | Evet (kimliksiz toplamlar) | Hayır | Hayır |
| **SystemAdministrator** | **Hayır (`403 Forbidden`)** | **Hayır (`403 Forbidden`)** | Hayır |
| **Patient** | Hayır (Genel KPI kapalı) | Hayır | `specialty-care.view-own`; yalnız oturumdan çözülen kendi minimum portal özeti |

---

## 3. Operasyonel Metrikler (`SpecialtyOperationalSummaryResponse`)

### 3.1 Kadın Doğum Metrikleri
- `ActivePregnanciesCount`: Aktif takip edilen gebelikler
- `HighRiskPregnanciesCount`: Yüksek ve çok yüksek riskli gebelikler
- `TotalDeliveriesCount`: Toplam gerçekleşen doğumlar
- `CesareanDeliveriesCount`: Sezaryen doğumlar (Elektif ve Acil)
- `NormalDeliveriesCount`: Normal vajinal doğumlar

### 3.2 Diş Hekimliği Metrikleri
- `TotalDentalProceduresCount`: Planlanan ve tamamlanan toplam işlemler
- `CompletedDentalProceduresCount`: Tamamlanan tedaviler
- `PlannedDentalProceduresCount`: Planlanan tedaviler
- `TotalDentalExaminationsCount`: Toplam diş muayeneleri

### 3.3 Evde Sağlık Metrikleri
- `ActiveHomeVisitsCount`: Talep, atandı ve yolda olan toplam ziyaretler
- `PendingHomeVisitRequestsCount`: Ekip ataması bekleyen talepler
- `AssignedHomeVisitsCount`: Personel atanmış veya yolda olan ziyaretler
- `CompletedHomeVisitsCount`: Klinik notu girilerek tamamlanan ziyaretler
- `UrgentHomeVisitsCount`: Acil öncelikli aktif ziyaretler

---

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/specialty/reports/operational-summary` | `report.operations.view` | Uzmanlık dikey dilimlerinin kimliksiz operasyonel özetini getirir |
| `GET` | `/api/v1/specialty/patient-portal/my-records` | `specialty-care.view-own` | Oturumdaki Person kimliğinden çözülen Patient için yayınlanmış minimum uzmanlık özetini getirir |

Hasta portalı uç noktası `PatientId` parametresi kabul etmez. İstemci sorguya veya gövdeye bir hasta kimliği eklese bile kapsam bu değerle değişmez; aktif Patient kimliği sunucuda oturumun `PersonId` claim'i üzerinden çözülür. Böylece permission, resource scope (`OWN`) ve kişi–hasta ilişkisi API seviyesinde birlikte uygulanır.

## 5. Hasta Portalı Yayın Politikası

| Dikey | Yayınlanan | Yayınlanmayan |
|---|---|---|
| Gebelik | Protokol, durum, tahmini doğum tarihi, kontrol sayısı ve son kontrol tarihi | Risk sınıfı/notu, obstetrik öykü, ölçümler, klinik notlar, ekip/Encounter kimlikleri |
| Doğum | Protokol, yöntem, tarih, gebelik yaşı ve yalnız yenidoğan sayısı | Anne komplikasyonu, kan kaybı, klinik özet, Apgar/ölçümler ve yenidoğan Patient kimlikleri |
| Diş | Muayene tarih/protokolü; yalnız `Completed` işlem adı, diş no ve tamamlanma tarihi | `Planned`, `InProgress`, `Cancelled` işlemler; şikâyet, tanı/tedavi notu, maliyet ve hekim/Encounter kimlikleri |
| Evde sağlık | Operasyonel durum, hizmet/öncelik, tarih, il ve ilçe | Açık adres, telefon, klinik/vital notu, personel ve Encounter kimlikleri |

Portal okuması `Specialty.PatientPortalView` audit eylemi üretir. Audit hedefi Patient kimliğidir; `DetailsJson` boş tutulur ve klinik içerik, adres veya iletişim bilgisi audit kaydına kopyalanmaz. Ayrı bir yasal temsilci/veli modeli bulunmadığı için annenin portalında yenidoğana ait Patient kimliği veya klinik ayrıntı yayımlanmaz.

---

## 6. Doğrulama ve Testler

- **Unit Testleri:** `SpecialtyReportingDomainTests` (Metrik veri yapısı ve tarih atamaları).
- **Component Testleri:** `SpecialtyReportsComponentTests` (BUnit gösterge kartları, gizlilik bildirimi banner'ı, yenileme butonu).
- **Entegrasyon Testleri:** `SpecialtyReportingIntegrationTests` (PostgreSQL üzerinde Kadın Doğum + Diş + Evde Sağlık kayıtlarının kümülatif göstergelere yansıması; HospitalManager rapor erişimi; SystemAdministrator için `403`; yanıtın hassas PII/adres sızdırmaması).
- **Portal Entegrasyonu:** `SpecialtyGateIntegrationTests` (kendi Patient kapsamı, sorgu parametresiyle IDOR denemesi, taslak diş işlemi gizleme, hassas alan/yenidoğan kimliği sızıntı kontrolü, Doctor/Admin `403`, anonim `401` ve minimize audit).
- **Portal Bileşeni:** `MySpecialtyRecordsComponentTests` (yayınlanmış kayıt gösterimi ve güvenli hata durumu).
