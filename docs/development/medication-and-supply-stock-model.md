# İlaç ve Sarf Stok Modeli (Medication and Supply Stock Model)

## 1. Genel Bakış ve Amaç

Bu belge, **Faz 5 (Eczane & İlaç Yönetimi)** kapsamında uygulanan `F05-G06 — İlaç ve sarf stok modeli` mimarisini, stok kalemleri, FEFO (First-Expired-First-Out / İlk Miatı Dolan İlk Çıkar) kuralını, stok hareket türlerini ve denetim mekanizmalarını açıklar.

---

## 2. Stok Modeli ve Değişmezleri (Invariants)

- **Negatif Stok Yasağı:** Stok miktarı (`QuantityOnHand`), rezerve miktar (`QuantityReserved`) ve kritik stok seviyesi (`ReorderLevel`) hiçbir koşulda negatif olamaz. Bu kural hem domain nesnesinde (`MedicationStockItem`) hem de veritabanı CHECK kısıtları (`ck_medication_stock_items_quantity_on_hand`, `ck_medication_stock_items_quantity_reserved`, `ck_medication_stock_items_reorder_level`) ile garanti altına alınmıştır.
- **Rezervasyon bütünlüğü:** `QuantityReserved <= QuantityOnHand` veritabanı kısıtı vardır ve teslim yalnız `QuantityAvailable` üzerinden düşebilir; rezerve miktar başka teslim tarafından tüketilemez.
- **İyimser Eşzamanlılık (Optimistic Concurrency):** Her stok düşümünde, eklemesinde veya düzeltmesinde `Version` kolonu otomatik artırılır ve EF Core `IsConcurrencyToken()` ile çakışmalar engellenir.
- **Bölüm ve lot kapsamı:** Her stok kalemi zorunlu `DepartmentId` taşır. Aynı bölümdeki aynı ilaç, lokasyon ve lot yalnız tek stok kalemi olabilir; görünüm, FEFO, düzeltme ve hareket geçmişi oturumdaki eczane bölüm atamasıyla filtrelenir.

---

## 3. FEFO (First-Expired-First-Out) Algoritması

İlaç teslim ve dağıtım süreçlerinde miat kaybını ve israfı önlemek amacıyla:
1. İlaç kataloğu kimliğine (`MedicationCatalogItemId`) ait,
2. Kullanılabilir stoku olan (`QuantityOnHand - QuantityReserved > 0`),
3. Son kullanma tarihi dolmamış (`ExpirationDateUtc > nowUtc`)

tüm lotlar son kullanma tarihine göre artan sırada (`ORDER BY expiration_date_utc ASC`) sıralanarak döner (`ix_medication_stock_items_fefo` indeksi ile hızlandırılmıştır).

---

## 4. Stok Hareket Türleri (`StockTransactionType`)

Tüm stok değişiklikleri `medication_stock_transactions` tablosunda geçmişe dönük denetim iziyle loglanır:

| Tür | Değer | Açıklama |
|---|---|---|
| `InitialReceipt` | 1 | İlk Giriş / Satınalma / Demo Devri |
| `Dispense` | 2 | Reçete Teslimi ile Düşüm |
| `AdjustmentIn` | 3 | Fiziksel Sayım Fazlası Düzeltme Girişi |
| `AdjustmentOut` | 4 | Fiziksel Sayım Eksiği Düzeltme Çıkışı |
| `Return` | 5 | İlaç İadesi Girişi |
| `Expired` | 6 | Miat / Son Kullanma Tarihi Dolumu Çıkışı |

---

## 5. API Uç Noktaları

| Metot | Yol | Açıklama |
|---|---|---|
| `GET` | `/api/v1/pharmacy/inventory/stock` | Stok genel bakışı (ilaç, lokasyon ve kritik stok filtresi). |
| `GET` | `/api/v1/pharmacy/inventory/fefo-candidates/{medicationCatalogItemId}` | FEFO kuralına göre sıralı kullanılabilir lot listesi. |
| `POST` | `/api/v1/pharmacy/inventory/adjust` | `ExpectedVersion` taşıyan gerekçeli stok miktarı düzeltme (en az 5 karakter gerekçe zorunludur). |
| `GET` | `/api/v1/pharmacy/inventory/stock/{stockItemId}/transactions` | Belirli bir stok kalemine ait hareket geçmişi. |

---

## 6. Doğrulama ve Test Kapsamı

- **Domain Birim Testleri (`MedicationStockDomainTests`):** Pozitif stok kontrolleri, negatif stok fırlatmaları, rezervasyon mekanizması, düzeltme ve versiyonlama.
- **Entegrasyon Testleri (`MedicationStockIntegrationTests`):** PostgreSQL üzerinde 15 ilaç için 30 lotun FEFO sıralaması, stok düzeltme işlemi, hareket kaydı oluşturma, yetkisiz erişim ve geçersiz girişlerin reddedilmesi.
