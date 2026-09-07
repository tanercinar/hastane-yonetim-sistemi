# Laboratuvar Test Kataloğu ve İstem İş Akışı (Laboratory Catalog & Order Workflow)

## 1. Amaç ve Kapsam

Bu belge, **F06-G02 — Laboratuvar test kataloğu ve istem** kapsamında geliştirilen demo laboratuvar kataloğunu, parametre hiyerarşisini, hekim tetkik istem arayüzünü (`DoctorDiagnosticOrderEditor.razor`) ve sürümlenebilir içe aktarım mimarisini belgeler.

---

## 2. Laboratuvar Kataloğu ve Parametre Modeli

Laboratuvar kataloğu iki seviyeli hiyerarşiye sahiptir:
1. **`LabCatalogItem` (Test / Panel):** Tekil bir tahlil (örn: Glukoz) veya birden fazla alt parametre içeren kapsamlı bir panel (örn: Hemogram, Lipid Profili, Karaciğer Fonksiyon Testleri).
   - `Code`, `Name`, `Category`, `SpecimenType`, `ContainerType`, `IsPanel`, `TurnaroundMinutes`, `CatalogVersion`, `IsActive`.
2. **`LabCatalogParameter` (Alt Parametre / Analit):** Test sonucunda ölçülen spesifik değer ve referans aralığı.
   - `Code` (WBC, RBC, HGB, PLT, ALT, AST, GLU vb.)
   - `Unit` (`10^3/µL`, `g/dL`, `mg/dL`, `U/L`, `mmol/L` vb.)
   - `ReferenceRangeLow`, `ReferenceRangeHigh` (Normal referans sınırları)
   - `CriticalLow`, `CriticalHigh` (Panik / Kritik değer eşikleri)

### 2.1 Tohumlanan Standart Demo Panelleri (`DEMO-LAB-2026.1`)

| Kod | Panel / Test Adı | Kategori | Numune / Tüp Türü | Parametre Sayısı |
|---|---|---|---|:---:|
| `DEMO-LAB-CBC` | Tam Kan Sayımı (Hemogram 18 Parametre) | Hematoloji | Venöz Tam Kan (Mor Kapaklı EDTA Tüp) | 7 |
| `DEMO-LAB-GLU` | Açlık Kan Şekeri (Glukoz) | Klinik Biyokimya | Serum (Sarı Kapaklı Jelli Tüp) | 1 |
| `DEMO-LAB-LIPID` | Lipid Profili Paneli | Klinik Biyokimya | Serum (Sarı Kapaklı Jelli Tüp) | 4 |
| `DEMO-LAB-LFT` | Karaciğer Fonksiyon Paneli (LFT) | Klinik Biyokimya | Serum (Sarı Kapaklı Jelli Tüp) | 5 |
| `DEMO-LAB-RFT` | Böbrek Fonksiyon Paneli (RFT & Elektrolit) | Klinik Biyokimya | Serum (Sarı Kapaklı Jelli Tüp) | 5 |
| `DEMO-LAB-URINE` | Tam İdrar Tahlili (TİT) | Mikrobiyoloji & İdrar | Spot İdrar (Steril İdrar Kabı) | 5 |
| `DEMO-LAB-TSH` | Tiroid Fonksiyon Paneli | Hormon & İmmünoloji | Serum (Sarı Kapaklı Jelli Tüp) | 2 |
| `DEMO-LAB-CRP` | C-Reaktif Protein (Kantitatif CRP) | Klinik Biyokimya | Serum (Sarı Kapaklı Jelli Tüp) | 1 |
| `DEMO-LAB-COAG` | Koagülasyon Paneli (PT, aPTT, INR) | Koagülasyon | Plazma (Mavi Kapaklı Sitratlı Tüp) | 3 |
| `DEMO-LAB-VITS` | Vitamin B12 & Ferritin Paneli | Hormon & İmmünoloji | Serum (Sarı Kapaklı Jelli Tüp) | 2 |

---

## 3. Hekim İstem Arayüzü (`DoctorDiagnosticOrderEditor.razor`)

- **Canlı Arama & Filtreleme:** Arama kutusu ile tahlil adı, kod veya kategoriye göre hızlı filtreleme.
- **İstem Sepeti:** Seçilen testlerin sepet formatında görüntülenmesi, öncelik (`Routine`, `Urgent`, `Stat`), klinik gerekçe ve özel hazırlık notlarının girilmesi.
- **Onaylama & Taslak:** Taslak kaydetme veya doğrudan `Place` ederek laboratuvara iletme.
- **Klinik Kilit:** Tamamlanmış karşılaşmalarda form salt okunur kilit moduna geçer.
- **İptal Modalı:** İletilmiş istemlerin zorunlu klinik gerekçe ile iptal edilmesi.
