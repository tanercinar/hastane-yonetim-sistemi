# Hasta Zaman Çizelgesi (Patient Longitudinal Timeline - F04-G08)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G08 — Hasta zaman çizelgesi` görevine ait mimariyi, çok kaynaklı klinik olay harmanlamasını, sayfalama ve yetki/gizlilik filtreleme kurallarını açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Servis Arayüzü:** `IPatientTimelineService`
- **Servis Uygulaması:** `PatientTimelineService`
- **REST Uç Noktası:** `GET /api/v1/clinical-records/timeline/by-patient/{patientId:guid}`

---

## 2. Zaman Çizelgesinde Birleştirilen Klinik Olay Türleri (`PatientTimelineEventType`)

Hasta zaman çizelgesi, hastanın sağlık serüvenindeki tüm kritik klinik olayları kronolojik (en yeniden en eskiye) olarak toplar ve sayfalar:

1. **`Encounter` (Karşılaşma / Muayene):** Başlangıç zamanı, karşılaşma tipi (`Outpatient`, `Inpatient`, `Emergency`), şikayet ve durum.
2. **`VitalSigns` (Vital Bulgular):** Ölçülen nabız, tansiyon, ateş, SpO2 değerleri ve klinik yorumu (`Interpretation`: Normal, Critical, vb.).
3. **`ClinicalNote` (Klinik Notlar):** SOAP, epikriz, progres notu başlığı ve değerlendirme özeti.
4. **`Diagnosis` (Tanılar):** ICD-10 kodlu veya serbest metin tanılar (`Preliminary`, `Differential`, `Final`, `Secondary`).
5. **`Consultation` (Konsültasyonlar):** İstek gerekçesi, hedef bölüm, tamamlanma raporu ve uzman önerisi.
6. **`Attachment` (Klinik Ekler):** Yüklenen lab/radyoloji raporları, dosya tipi ve boyutu.
7. **`Allergy` (Alerji ve İntoleranslar):** Madde adı, reaksiyon ve önem derecesi (`Criticality`).
8. **`Problem` (Klinik Problemler):** Aktif ve kronik problem kayıtları.

---

## 3. Güvenlik, Gizlilik ve Filtreleme Kuralları

- **Gizli / Taslak Not Koruması:**
  - Hekim tarafından hazırlanan ancak henüz **imzalanmamış (`Draft`)** klinik notlar hastanın kendi zaman çizelgesinde kesinlikle **gözükmez**.
  - Yalnızca klinik personel (`Doctor`, `Nurse`, `ChiefMedicalOfficer`) taslak notları görebilir.
- Zaman çizelgesi `EncounterView + bakım ilişkisi/katılım` veya hastanın kendi kaydıyla açılır; rol adı ve sistem yöneticiliği tek başına erişim sağlamaz.
- Serbest klinik metinler (not içeriği, konsültasyon soru/raporu, dosya adı/açıklaması ve serbest notlar) özet akışına kopyalanmaz. Hasta `EnteredInError` kayıtları hiçbir sorgu parametresiyle açamaz.
- **Hatalı Giriş Kayıtları (`IsEnteredInError`):**
  - Varsayılan olarak zaman çizelgesinden filtrelenir. `includeEnteredInError=true` parametresi ile yalnızca yetkili hekimler tarafından incelenebilir.
- **Erişim ve IDOR Engelleme:**
  - Hastalar yalnızca kendi kimliklerine ait zaman çizelgesini sorgulayabilir. Başka hastanın zaman çizelgesine erişim `403 Forbidden` ile engellenir.
- **Sayfalama (`Pagination`):**
  - `pageNumber` ve `pageSize` (varsayılan: 20, maks: 100) parametreleri ile sunucu taraflı sayfalama yapılır.

---

## 4. Denetim İzi (Audit Trail)

Her zaman çizelgesi sorgusu `IAuditEventPublisher` aracılığıyla kaydedilir:
- `ClinicalRecords.TimelineView` (Hedef: hasta kimliği, toplam kayıt ve sayfa sayısı bilgisi)

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `TimelineDomainTests.cs` (2 test) — `PatientTimelineItem` haritalaması, `PatientTimelinePagedDto` sayfalama ve sınır hesaplamaları.
- **Entegrasyon Testleri:** `ClinicalTimelineIntegrationTests.cs` (1 test) — Hekim ve hasta perspektifinden kronolojik birleştirme, taslak notların hastadan gizlenmesi (`DraftNotesAreNotVisibleToPatientInTimeline`), başka hasta zaman çizelgesine erişimde IDOR engellemesi (`403 Forbidden`).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
