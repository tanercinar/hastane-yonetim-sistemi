# Laboratuvar Sonuçları ve Onay İş Akışı (F06-G04)

## 1. Amaç ve Kapsam

Bu belge, Hastane Yönetim Sistemi Tanısal Hizmetler modülündeki laboratuvar test sonuç girişi, teknik onay, yetkili klinik laboratuvar personeli kesinleştirmesi, referans aralığı / kritik değer bayraklama ve düzeltme (correction) yaşam döngüsünü belgeler.

> [!NOTE]
> Sistem bir eğitim ve demo platformudur. Tüm parametreler ve referans aralıkları sentetik veridir. Tıbbi tanı veya otomatik klinik karar desteği iddiası taşımaz.

---

## 2. Laboratuvar Sonucu Yaşam Döngüsü

Laboratuvar sonuçları kesinleşme ve düzeltme süreçlerinde klinik değişmezlik ilkelerine göre yönetilir:

```
  [ Taslak Giriş (Draft) ] ──(Teknik Onay)──► [ Teknik Onaylı (TechnicallyApproved) ]
             │                                              │
             │ (Klinik Onay / Kesinleştirme)                │ (Klinik Onay / Kesinleştirme)
             ▼                                              ▼
     [ Kesinleşmiş (FinalApproved) ] ◄──────────────────────┘
             │
             │ (Zorunlu Gerekçe ile Düzeltme)
             ▼
     [ Düzeltilmiş (Corrected) ] (Önceki kayda bağlı yeni sürüm)
```

### Durum Açıklamaları

1. **Draft (Taslak):** Laboratuvar teknisyeni veya yetkili personel parametre değerlerini girer ve günceller.
2. **TechnicallyApproved (Teknik Onaylı):** Laboratuvar teknisyeni analizör doğrulaması ve cihaz kalite kontrolünü tamamlayarak teknik onay verir.
3. **FinalApproved (Kesinleşmiş):** `LAB` rolü içinde `laboratory.result.finalize` izni bulunan yetkili klinik laboratuvar personeli, teknik onaydan sonra sonucu kesinleştirir. İlgili tanısal istem kalemi otomatik olarak `Reported` durumuna geçer.
4. **Corrected (Düzeltilmiş):** Kesinleşmiş bir sonuç doğrudan güncellenemez. Zorunlu gerekçe (`CorrectionReason`) girilerek önceki kayda (`PreviousResultId`) bağlı yeni bir sonuç kaydı üretilir.
5. **EnteredInError / Cancelled:** Hatalı giriş veya test iptal durumları için işaretleme.

---

## 3. Parametre Değerleri ve Bayraklama (Flags)

Her bir test parametresi için referans aralığına göre otomatik bayraklama yapılır:
- **Normal:** Değer referans aralığı sınırları içindedir.
- **Low (Düşük - L):** Değer alt referans sınırının altındadır.
- **High (Yüksek - H):** Değer üst referans sınırının üstündedir.
- **CriticalLow (Kritik Düşük):** Değer alt sınırın %50'si veya altındadır (Acil hekim bildirimi gerektirir).
- **CriticalHigh (Kritik Yüksek):** Değer üst sınırın 2 katı veya üzerindedir (Acil hekim bildirimi gerektirir).
- **Abnormal (Anormal):** Niteliksel testlerde ("Pozitif", "Reaktif", "Üreme oldu") anormal durum.

---

## 4. Güvenlik, Doğrulama ve Audit Kuralları

- **Klinik İmmutability (Değişmezlik):** `FinalApproved` durumundaki bir sonuç doğrudan `UpdateItems` ile sessizce değiştirilemez (`400 Bad Request` veya `InvalidOperationException`). Yalnızca `CorrectAsync` metodu ile yeni sürüm oluşturulabilir.
- **Hasta Portalı Kısıtı:** Hastalar taslak durumundaki sonuçları göremez; yalnızca `FinalApproved` veya `Corrected` durumundaki kesinleşmiş kendi sonuçlarına erişebilirler.
- **Audit Günlüğü:** `Diagnostics.LabResultCreateDraft`, `Diagnostics.LabResultUpdateDraft`, `Diagnostics.LabResultTechnicalApprove`, `Diagnostics.LabResultClinicalApprove` ve `Diagnostics.LabResultCorrect` eylemleri `audit_logs` tablosuna otomatik olarak kaydedilir.

---

## 5. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/diagnostics/lab-results` | `laboratory.result.edit_draft` | Taslak laboratuvar sonucu oluşturma |
| `PUT` | `/api/v1/diagnostics/lab-results/{id}/items` | `laboratory.result.edit_draft` | Parametre değerlerini güncelleme |
| `POST` | `/api/v1/diagnostics/lab-results/{id}/technical-approve` | `laboratory.result.edit_draft` | Teknisyen teknik onayı |
| `POST` | `/api/v1/diagnostics/lab-results/{id}/clinical-approve` | `laboratory.result.finalize` | Yetkili LAB kesinleştirmesi; teknik onay zorunludur |
| `POST` | `/api/v1/diagnostics/lab-results/{id}/correct` | `laboratory.result.finalize` | Gerekçeli sonuç düzeltme |
| `GET` | `/api/v1/diagnostics/lab-results/{id}` | - | Sonuç detayı |
| `GET` | `/api/v1/diagnostics/lab-results/by-order/{orderId}` | - | İstem bazında sonuçlar |
| `GET` | `/api/v1/diagnostics/lab-results/worklist` | `laboratory.worklist.view` | Sonuç iş listesi |
