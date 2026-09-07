# Kural Tabanlı İlaç Güvenlik Uyarıları (Rule-Based Medication Safety Warnings)

## 1. Genel Bakış ve Amaç

Bu belge, **Faz 5 (Eczane & İlaç Yönetimi)** kapsamında uygulanan `F05-G04 — Kural tabanlı güvenlik uyarıları` mimarisini, sentetik kontrol kurallarını, hekim geçersiz kılma (override) iş akışını ve klinik denetim izi mekanizmalarını açıklar.

> [!IMPORTANT]
> **Yasal ve Düzenleyici Bildirim:**
> Bu modül sentetik `DEMO` verilerle çalışan, deterministik kural tabanlı bir eğitim/gösterim bileşenidir. **Sertifikalı bir Klinik Karar Destek Sistemi (CDSS), tıbbi cihaz veya AI/makine öğrenmesi tabanlı bir tanı/tedavi sistemi DEĞİLDİR.** Her uyarı yanıtında ve hekim arayüzünde zorunlu yasal feragat metni sunulur.

---

## 2. Mimari ve Modüller Arası Ayrım

- **Modül İzolasyonu (`IPatientAllergyLookup`):** Modüller doğrudan birbirlerinin veritabanlarına veya `DbContext`'lerine bağlanamaz. Bu ilke doğrultusunda `HospitalManagement.BuildingBlocks.Clinical` altında `IPatientAllergyLookup` arayüzü tanımlanmış, `ClinicalRecords` modülü tarafından uygulanmış ve `Pharmacy` modülü tarafından bağımsız olarak tüketilmiştir.
- **Kural Denetleyicisi (`IMedicationSafetyChecker`):** `Pharmacy.Infrastructure` katmanında `MedicationSafetyChecker` sınıfı aracılığıyla saf deterministik kontroller gerçekleştirilir.
- **Kaynak yetkisi ve fail-safe davranış:** Güvenlik kontrolü yalnız gerçek, açık, hasta/bölüm eşleşmesi doğrulanmış ve hekimin bakım ilişkisi bulunan karşılaşmada çalışır. Alerji kaynağı okunamazsa kontrol sessizce atlanmaz; imzayı gerekçesiz engelleyen kritik `AllergyCheckUnavailable` uyarısı üretilir.

---

## 3. Sentetik Güvenlik Kuralları

1. **Alerji Çapraz Kontrolü (`AllergyCrossReaction` - Kritik):**
   - Hastanın aktif alerji kayıtları (`clinical_records.allergy_intolerances`) taranır.
   - Penisilin / Beta-laktam alerjisi olan hastaya Amoksisilin (`DEMO-MED-AMX500`) reçete edildiğinde `Critical` uyarı tetiklenir.
   - NSAİİ / Aspirin alerjisi olan hastaya İbuprofen reçete edildiğinde uyarı üretilir.
2. **Yinelenen Etken Madde Kontrolü (`DuplicateTherapy`):**
   - **Reçete İçi (Intra-Prescription - Kritik):** Aynı reçete taslağında aynı etken maddeyi (örn. Parasetamol) içeren birden fazla kalem bulunması durumunda `Critical` uyarı üretilir.
   - **Aktif Tedavi İle (Active-Prescription - Orta):** Hastanın halihazırda imzalanmış veya kısmen karşılanmış başka bir reçetesinde aynı etken madde bulunuyorsa `Moderate` uyarı üretilir.
3. **Basit İlaç-İlaç Etkileşim Matrisi (`DrugInteraction`):**
   - `DEMO-INT-ASP-IBU` (Aspirin + İbuprofen): İki NSAİİ eşzamanlı kullanımı -> `Critical`.
   - `DEMO-INT-AMX-MTX` (Amoksisilin + Metotreksat): Metotreksat klirensi azalması -> `Moderate`.
   - `DEMO-INT-CIP-THEO` (Siprofloksasin + Teofilin): Serum teofilin toksisitesi -> `Critical`.
   - `DEMO-INT-CLA-ATOR` (Klaritromisin + Atorvastatin): CYP3A4 inhibisyonu ve rabdomiyoliz -> `Critical`.
   - `DEMO-INT-ENA-SPRO` (Enalapril + Spironolakton): Ciddi hiperkalemi riski -> `Moderate`.

---

## 4. Hekim Geçersiz Kılma (Override Reason) ve Denetim İzi

- Reçete imzalanırken (`SignAsync`), `Critical` veya `Moderate` düzeyde aktif güvenlik uyarısı tespit edilirse:
  - İstekte geçerli bir `OverrideReason` (en az 5 karakter) bulunmadığı takdirde imzalama işlemi engellenir ve HTTP `422 Unprocessable Entity` (`SafetyWarningOverrideRequired`) döner.
  - Hekim geçerli bir gerekçe girip mevcut zorunlu uyarı kodlarının tamamını `AcknowledgedWarningCodes` ile kabul ettiğinde reçete imzalanır.
  - `audit_privacy.audit_logs` tablosuna `Pharmacy.PrescriptionSafetyWarningOverride` eylemi ve uyarı sayısı yazılır; klinik serbest metin olan override gerekçesi audit/log/telemetry alanına kopyalanmaz.

---

## 5. API Uç Noktaları

| Metot | Uç Nokta | İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/pharmacy/prescriptions/safety-check` | `Pharmacy.PrescriptionCreate` + encounter kapsamı | Yetkili karşılaşmadaki taslak kalemler için kural tabanlı güvenlik kontrolü çalıştırır |
| `POST` | `/api/v1/pharmacy/prescriptions/{id}/sign` | `Pharmacy.PrescriptionSign` | Reçeteyi imzalar (gerekirse `OverrideReason` ve `AcknowledgedWarningCodes` alır) |

---

## 6. Doğrulama ve Test Kapsamı

- **Birim Testleri (`MedicationSafetyCheckerTests`):** Alerji çapraz reaksiyonu, reçete içi mükerrer doz, NSAİİ etkileşimi, antibiyotik etkileşimi ve feragat metni doğrulanmıştır.
- **Bileşen Testleri (`DoctorPrescriptionEditorComponentTests`):** bUnit ile uyarı paneli görünümü, uyarı varlığında imza butonunun override modalını tetiklemesi ve gerekçeli imzalama test edilmiştir.
- **Entegrasyon Testleri (`MedicationSafetyIntegrationTests`):** PostgreSQL üzerinde alerji çapraz kontrolü, gerekçesiz imzanın 422 ile reddedilmesi, gerekçeli imzanın 200 ile tamamlanması ve denetim günlüğü kaydı uçtan uca doğrulanmıştır.
