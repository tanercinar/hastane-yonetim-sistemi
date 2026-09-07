# Ortak Klinik İstem Modeli ve Yaşam Döngüsü (Diagnostic Order Model & Lifecycle)

## 1. Amaç ve Kapsam

Bu belge, **F06-G01 — Ortak klinik istem modeli** kapsamında tanısal ve yardımcı sağlık hizmetleri (Laboratuvar, Radyoloji, Patoloji ve Kan Bankası) için geliştirilen ortak istem çekirdeğini, veri modelini, durum makinesini ve güvenlik/denetim kurallarını açıklar.

---

## 2. Mimari ve Veri Modeli

Ortak klinik istem modeli `diagnostics` şemasında konumlandırılmış olup diğer modüllerin (ClinicalRecords, Patients, IdentityAccess) veritabanı veya tablolarına doğrudan erişim sağlamadan bağımsız aggregate root olarak çalışır.

### 2.1 İstem (`DiagnosticOrder`)
- **Id:** `Guid`
- **OrderNumber:** `string` (Örn: `DEMO-LAB-20260830-A1B2C3`, `DEMO-RAD-...`, `DEMO-PAT-...`, `DEMO-BB-...`)
- **PatientId:** `Guid` (İstem yapılan hasta)
- **EncounterId:** `Guid` (İlişkili klinik karşılaşma)
- **PlacingDoctorId:** `Guid` (İstemi veren hekim)
- **DepartmentId:** `Guid` (İstemin yapıldığı poliklinik/servis)
- **OrderType:** `DiagnosticOrderType` (`Laboratory`, `Radiology`, `Pathology`, `BloodBank`)
- **Priority:** `DiagnosticOrderPriority` (`Routine`, `Urgent`, `Stat`)
- **Status:** `DiagnosticOrderStatus` (`Draft`, `Placed`, `InProgress`, `Completed`, `Cancelled`, `EnteredInError`)
- **ClinicalIndication:** `string?` (Ön tanı / klinik gerekçe)
- **OrderNotes:** `string?` (Özel notlar ve hazırlık talimatları)
- **CancellationReason:** `string?` (Gerekçeli iptal açıklaması)
- **EnteredInErrorReason:** `string?` (Hatalı giriş açıklaması)
- **Version:** `uint` (`xmin` satır sürümü ile iyimser eşzamanlılık)

### 2.2 İstem Kalemi (`DiagnosticOrderItem`)
- **Id:** `Guid`
- **DiagnosticOrderId:** `Guid`
- **CatalogCode:** `string` (Örn: `LAB-CBC`, `RAD-XRAY-CHEST`, `PAT-BIOPSY-SKIN`, `BB-RBC-PACK`)
- **CatalogItemName:** `string`
- **Category:** `string` (Örn: `Hematology`, `Biochemistry`, `Radiology`, `Histopathology`, `BloodBank`)
- **Status:** `DiagnosticOrderItemStatus` (`Pending`, `SampleCollected`, `SampleReceived`, `InAnalysis`, `Reported`, `Cancelled`)
- **SpecialInstructions:** `string?`

---

## 3. Durum Makinesi ve Kurallar

```
[Taslak (Draft)] ──── (Place) ───► [İletildi (Placed)] ──── (Sample / Schedule) ───► [İşlemde (InProgress)]
      │                                   │                                                │
      ├─── (Cancel/EnteredInError)        ├─── (Cancel/EnteredInError)                     │
      ▼                                   ▼                                                ▼
[İptal / Hatalı Giriş]             [İptal / Hatalı Giriş]                             [Tamamlandı (Completed)]
```

1. **Kalem Bütünlüğü:** Taslak aşamasında isteme mükerrer katalog kodu eklenemez (`InvalidOperationException`).
2. **Onaylama (Place):** En az bir test/tetkik kalemi içermeyen taslak istem onaylanamaz.
3. **Klinik Kilit ve Değişmezlik:** Onaylanan (`Placed`) istemlerin kalemleri sessizce değiştirilemez veya silinemez.
4. **Gerekçeli İptal:** İstem iptal edilirken zorunlu açıklama (`Reason`) girilmelidir; ilişkili tüm kalemler `Cancelled` durumuna çekilir.
5. **Denetim İzi (Audit Logging):** Her işlem (`OrderCreateDraft`, `OrderPlace`, `OrderCancel`, `OrderEnteredInError`) `audit_privacy.audit_logs` tablosuna aktarılır.
