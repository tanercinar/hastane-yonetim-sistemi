# Demo İlaç Kataloğu ve Sürümleme (Medication Catalog & Versioning - F05-G01)

Bu belge, **Faz 5: Reçete, Eczane ve Klinik Stok** aşamasının ilk görevi olan `F05-G01 — Demo ilaç kataloğu` geliştirmesini, sentetik ilaç veri yapısını, form ve uygulama yolu modellerini, lisans ve provizyon güvenliğini ve sürümlenebilir içe aktarma mekanizmasını açıklar.

---

## 1. Mimari Genel Bakış

- **Modül:** `HospitalManagement.Modules.Pharmacy`
- **Şema:** `pharmacy`
- **Tablo:** `medication_catalog_items`
- **Varlık:** `MedicationCatalogItem`
- **Enumlar:** `MedicationForm` (`Tablet`, `Capsule`, `Syrup`, `Suspension`, `Injection`, `Ointment`, `Drops`, `Inhaler`, `Spray`, `Suppository`), `MedicationRoute` (`Oral`, `Intravenous`, `Intramuscular`, `Subcutaneous`, `Topical`, `Inhalation`, `Ophthalmic`, `Otic`, `Nasal`, `Rectal`, `Sublingual`)
- **Servis Arayüzü:** `IMedicationCatalogService`
- **Servis Uygulaması:** `MedicationCatalogService`
- **Seeder:** `IMedicationCatalogDataSeeder` & `MedicationCatalogDataSeeder`
- **REST Uç Noktaları:**
  - `GET /api/v1/pharmacy/medications` (Arama, form/route/active filtreleme, sayfalama)
  - `GET /api/v1/pharmacy/medications/{id:guid}` (Tekil ilaç detay sorgusu)
  - `POST /api/v1/pharmacy/medications/import` (Sürümlenebilir katalog içe aktarma / güncelleme)

---

## 2. Değişmez Güvenlik ve Lisans Kuralları

1. **Sentetik Veri Garantisi (ADR-0006):**
   - Katalogdaki tüm ticari adlar açıkça `DEMO-` ön ekiyle başlar (örn: `DEMO-Amoksilin 500mg Kapsül`, `DEMO-Parasetamol 500mg Tablet`).
   - Gerçek kurumsal/ticari fiyat, SGK provizyon veya geri ödeme kodları **kesinlikle barındırılmaz**.
2. **Katalog Sürümleme Bütünlüğü:**
   - İlaç kayıtları `(code, catalog_version)` ikilisi üzerinden `ux_medication_catalog_items_code_version` benzersiz dizini ile güvenceye alınır.
   - Sürümler arası geriye dönük uyumluluk ve reçete bağlamının korunması sağlanır.
3. **Yetkilendirme Matrisi:**
   - `medication-catalog.view`: Hekim, Hemşire, Eczacı, Başhekim, Kayıt Personeli ve Sistem Yöneticisi erişebilir.
   - `medication-catalog.manage`: Yalnızca Eczacı, Hastane Yöneticisi ve Sistem Yöneticisi katalog içe aktarma yapabilir.

---

## 3. Varsayılan Demo İlaç Kataloğu (`DEMO-MED-2026.1`)

Sistemde başlangıçta 15 temel sentetik ilaç tanımlıdır:
1. `DEMO-MED-AMX500` — Amoksisilin 500mg Kapsül (Oral, J01CA04)
2. `DEMO-MED-CIP500` — Siprofloksasin 500mg Film Tablet (Oral, J01MA02)
3. `DEMO-MED-AZI500` — Azitromisin 500mg Tablet (Oral, J01FA10)
4. `DEMO-MED-PAR500` — Parasetamol 500mg Tablet (Oral, N02BE01)
5. `DEMO-MED-IBU400` — İbuprofen 400mg Draje (Oral, M01AE01)
6. `DEMO-MED-ASA100` — Asetilsalisilik Asit 100mg Enterik Tablet (Oral, B01AC06)
7. `DEMO-MED-RAM05` — Ramipril 5mg Tablet (Oral, C09AA05)
8. `DEMO-MED-AML05` — Amlodipin 5mg Tablet (Oral, C08CA01)
9. `DEMO-MED-MET50` — Metoprolol Suksinat 50mg Kontrollü Salım Tableti (Oral, C07AB02)
10. `DEMO-MED-ATO20` — Atorvastatin 20mg Film Tablet (Oral, C10AA05)
11. `DEMO-MED-SAL100` — Salbutamol 100mcg İnhalasyon Aerosolü (Inhalation, R03AC02)
12. `DEMO-MED-PAN40` — Pantoprazol 40mg Enterik Tablet (Oral, A02BC02)
13. `DEMO-MED-MET850` — Metformin Hidroklorür 850mg Film Tablet (Oral, A10BA02)
14. `DEMO-MED-INS100` — İnsülin Glargin 100 IU/ml Enjeksiyonluk Çözelti (Subcutaneous, A10AE04)
15. `DEMO-MED-CET10` — Setirizin 10mg Film Tablet (Oral, R06AE07)

---

## 4. Denetim İzi (Audit Trail)

Katalog içe aktarma / güncelleme işlemleri `audit_privacy.audit_logs` tablosuna append-only olarak işlenir:
- Eylem: `Pharmacy.MedicationCatalogImport`
- Hedef Kaynak: `MedicationCatalog`
- Hedef ID: İlgili katalog sürümü (örn: `DEMO-MED-2026.2`)

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri:** `MedicationCatalogDomainTests.cs` (5 test) — Alan doğrulamaları, geçersiz parametre engelleri, güncelleme ve aktif/pasif geçişleri, seeder bütünlüğü.
- **Entegrasyon Testleri:** `MedicationCatalogIntegrationTests.cs` (4 test) — Genel ad / ticari ad / ATC kodu / form / route filtreleri ile arama, ID ile tekil getirme, sürümleme ve denetim kayıtlı import, yetkisiz isteklerin (`401`/`403`) engellenmesi.
- **Tüm Süit:** 247/247 test %100 Başarılı.
