# Diş Hekimliği ve Odontogram (Odontology and Dental Charting)

## 1. Amaç ve Kapsam

Bu belge, **Faz 9 — Uzmanlık dikey dilimleri** kapsamında `F09-G03 — Diş hekimliği` görevinin teknik mimarisini, veri modelini, API sözleşmelerini ve güvenlik kurallarını açıklar.

Diş hekimliği dikey dilimi; FDI iki basamaklı uluslararası diş numaralandırma standardı (ISO 3950), etkileşimli odontogram görselleştirmesi, yüzey bazlı çürük/dolgu/kuron/kanal tedavisi kayıtları, geçmiş revizyon takibi ve tedavi planlama süreçlerini kapsar.

> [!WARNING]
> **Klinik Simülasyon Bildirimi:** Bu modül bir eğitim ve yönetim simülasyonu bileşenidir; sertifikalı diş hekimliği klinik karar desteği veya otomatik radyoloji tanı sistemi değildir. Tıbbi tanı ve tedavi planı yetkili diş hekiminin sorumluluğundadır.

---

## 2. Mimari ve Modül İzolasyonu

Modüler monolit prensiplerine uygun olarak diş hekimliği ve odontogram kayıtları `SpecialtyCare` modülü altında `specialty` şemasında konumlandırılmıştır.

```
src/Modules/SpecialtyCare/
├── Domain/Odontology/
│   ├── ToothCondition.cs
│   ├── ToothSurface.cs
│   ├── DentalProcedureStatus.cs
│   ├── FdiToothValidator.cs
│   ├── DentalToothCondition.cs
│   ├── DentalProcedure.cs
│   └── DentalExaminationRecord.cs (Aggregate Root)
├── Application/
│   ├── DentalDtos.cs
│   └── IDentalCareService.cs
└── Infrastructure/
    ├── DentalCareService.cs
    └── Persistence/
        ├── Configurations/
        │   ├── DentalToothConditionConfiguration.cs
        │   ├── DentalProcedureConfiguration.cs
        │   └── DentalExaminationConfiguration.cs
        └── Migrations/
            └── 20260831001000_AddOdontologyAndDentalRecords.cs
```

---

## 3. FDI ISO 3950 Diş Numaralandırması ve Yüzeyler

### 3.1 Kadranlar ve Diş Numaraları
- **Üst Sağ (Kadran 1):** 18, 17, 16, 15, 14, 13, 12, 11
- **Üst Sol (Kadran 2):** 21, 22, 23, 24, 25, 26, 27, 28
- **Alt Sol (Kadran 3):** 31, 32, 33, 34, 35, 36, 37, 38
- **Alt Sağ (Kadran 4):** 41, 42, 43, 44, 45, 46, 47, 48
- **Süt Dişleri (Kadran 5-8):** 51-55, 61-65, 71-75, 81-85

### 3.2 Diş Yüzeyleri (`ToothSurface` Flags)
- `Mesial` (1) - Orta hatta bakan ön yüz
- `Distal` (2) - Orta hattan uzak arka yüz
- `Occlusal` / `Incisal` (4) - Çiğneme veya kesici yüzey
- `Buccal` (8) - Yanak / dudak yönü yüzey
- `Lingual` / `Palatal` (16) - Dil / damak yönü yüzey

### 3.3 Klinik Değişmezlik ve Geçmiş Takibi
Diş durumu güncellendiğinde önceki kayıt sessizce silinmez veya üzerine yazılmaz (`Version` artarak yeni kayıt eklenir). Böylece dişin çürük halinden dolgulu veya çekilmiş hale geçiş aşamaları tam tarih ve personel denetim iziyle saklanır.

---

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/specialty/dental/odontogram/{patientId}` | Yetkili Kullanıcı | Hastanın tüm dişlerinin en güncel odontogram durumunu getirir |
| `POST` | `/api/v1/specialty/dental/odontogram/{patientId}/tooth` | `specialty-care.record` | Belirli bir dişin durumunu versiyonlayarak kaydeder |
| `GET` | `/api/v1/specialty/dental/odontogram/{patientId}/tooth/{toothNumber}/history` | Yetkili Kullanıcı | Belirli bir dişin tüm geçmiş revizyonlarını listeler |
| `GET` | `/api/v1/specialty/dental/procedures/patient/{patientId}` | Yetkili Kullanıcı | Hastanın planlanan ve tamamlanan tedavilerini listeler |
| `POST` | `/api/v1/specialty/dental/procedures` | `specialty-care.record` | Yeni diş tedavisi planlar (`DNT-FILLING`, `DNT-ROOTCANAL` vb.) |
| `POST` | `/api/v1/specialty/dental/procedures/{id}/complete` | `specialty-care.record` | Planlanan tedaviyi tamamlandı olarak işaretler |
| `GET` | `/api/v1/specialty/dental/examinations/patient/{patientId}` | Yetkili Kullanıcı | Diş muayenelerini listeler |
| `POST` | `/api/v1/specialty/dental/examinations` | `specialty-care.record` | Yeni diş muayenesi oluşturur |

---

## 5. Doğrulama ve Testler

- **Unit Testleri:** `DentalDomainTests` (FDI doğrulaması, yüzey bit bayrakları, revizyon versiyonlama, işlem durum makinesi).
- **Component Testleri:** `DentalManagementComponentTests` (BUnit odontogram butonları, klavye/erişilebilirlik, tedavi tablosu ve modal tetikleyicileri).
- **Entegrasyon Testleri:** `DentalCareIntegrationTests` (PostgreSQL üzerinde muayene → çürük dişi kaydetme → tedavi tamamlama → dolgulu dişi revize etme → diş geçmişi doğrulama, 403 Forbidden negatif testi).
