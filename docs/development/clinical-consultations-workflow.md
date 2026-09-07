# Konsültasyon İş Akışı (F04-G06)

Bu belge, **Faz 4: Klinik Kayıtlar ve Karşılaşma Yönetimi** kapsamındaki `F04-G06 — Konsültasyon` görevine ait mimariyi, durum makinesini, hekimler arası danışma akışını ve gizlilik modelini açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.ClinicalRecords`
- **Şema:** `clinical_records`
- **Tablo:** `consultation_requests`
- **Varlık:** `ConsultationRequest`
- **Servis Arayüzü:** `IConsultationService`
- **Servis Uygulaması:** `ConsultationService`

---

## 2. Konsültasyon Veri Modeli

- `Id`: Benzersiz kimlik
- `EncounterId`: Konsültasyonun istendiği karşılaşma kimliği
- `PatientId`: Hasta kimliği
- `RequestingPractitionerId`: Konsültasyonu talep eden hekim
- `TargetDepartmentId`: Konsültasyonun yönlendirildiği uzmanlık bölümü
- `TargetPractitionerId`: Belirli bir hekime yönlendirildiyse hekim kimliği (opsiyonel)
- `AssignedPractitionerId`: Konsültasyonu kabul eden / yanıtlayan hekim
- `Urgency`: Aciliyet (`Routine = 1`, `Urgent = 2`, `Stat = 3`)
- `Status`: Yaşam döngüsü durumu
- `ReasonForConsultation`: Konsültasyon gerekçesi
- `ClinicalQuestion`: Konsültanda yanıtlanması istenen odak klinik soru
- `ConsultationReport`: Konsültanın klinik değerlendirme raporu
- `Recommendation`: Konsültanın tedavi/takip önerileri
- `DeclineReason`, `CancellationReason`, `EnteredInErrorReason`: Gerekçe alanları
- `RequestedAtUtc`, `AcceptedAtUtc`, `CompletedAtUtc`, `UpdatedAtUtc`, `Version`

---

## 3. Durum Makinesi (`ConsultationStatus`)

```
               [Request]
                   │
                   ▼
               Requested
             /     │     \
    [Accept]/      │      \[Decline]
           ▼       │       ▼
       Accepted    │    Declined
       /      \    │[Cancel]
[Start]        \   │
   ▼            \  │
InProgress       \ │
   \              ▼▼
    \------> Completed   Cancelled
                 │
           EnteredInError
```

- **İstek (`Request`):** Hekim hedef bölüm ve aciliyet ile konsültasyon açar. Durum `Requested` olur.
- **Kabul (`Accept`):** İlgili bölümden bir hekim talebi kabul eder (`AssignedPractitionerId` atanır). Durum `Accepted` olur.
- **Tamamlama (`Complete`):** Konsültan hekim `ConsultationReport` ve `Recommendation` girerek konsültasyonu tamamlar. Durum `Completed` olur. Kabul edilmeden tamamlama denemesi `InvalidOperationException` fırlatır.
- **Reddetme (`Decline`):** İlgili hekim branş dışı vb. gerekçeyle reddeder. Durum `Declined` olur.
- **İptal (`Cancel`):** İsteyen hekim gerekçe belirterek tamamlanmamış konsültasyonu iptal edebilir.
- **Hatalı Giriş (`MarkEnteredInError`):** Gerekçeli olarak hatalı giriş işaretlenir.

---

## 4. Güvenlik, Gizlilik ve İzinler

- **Konsültasyon İsteme:** `ConsultationRequest` izni ve kaynak karşılaşmaya erişim gerekir.
- **Konsültasyon Kabul ve Yanıtlama:** `ConsultationRespond` izniyle birlikte hedef hekim/atanmış hekim eşleşmesi veya hedef bölüm görevlendirmesi gerekir.
- Tüm durum mutasyonları güncel `ExpectedVersion` ister; bayat istek `409 Conflict` olur. İstek, kabul, tamamlama, ret ve iptal durumlarında klinik içerik taşımayan idempotent bildirim üretilir.
- **Görüntüleme Güvenliği:**
  - Klinik hekimler ve sağlık personeli hastaya veya bölüme ait konsültasyonları görebilir.
  - Hasta yalnızca kendi konsültasyon kayıtlarını görüntüleyebilir; başka hastanın konsültasyonunu görmeye çalıştığında IDOR engellenir (`403 Forbidden`).

---

## 5. Denetim İzi (Audit Trail)

- `ClinicalRecords.ConsultationRequest`
- `ClinicalRecords.ConsultationAccept`
- `ClinicalRecords.ConsultationComplete`
- `ClinicalRecords.ConsultationDecline`
- `ClinicalRecords.ConsultationCancel`
- `ClinicalRecords.ConsultationEnteredInError`
- `ClinicalRecords.ConsultationView`

---

## 6. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `ConsultationDomainTests.cs` (7 test) — İstek oluşturma, kabul etme, tamamlama, kabul edilmeden tamamlamanın engellenmesi, reddetme, iptal etme, hatalı giriş kilidi.
- **Entegrasyon Testleri:** `ClinicalConsultationIntegrationTests.cs` (2 test) — Hekim konsültasyon isteme, kabul etme ve tamamlama tam uçtan uca akışı, hasta kendi konsültasyonunu görüntüleme ve IDOR koruması (`403 Forbidden`).
- Güncel toplamlar ve çalıştırma durumu Faz 4 kapı doğrulama belgesinde tutulur.
