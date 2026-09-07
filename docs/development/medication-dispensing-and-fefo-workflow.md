# İlaç Teslimi ve FEFO Karşılama İş Akışı (Medication Dispensing and FEFO Workflow)

## 1. Genel Bakış ve Amaç

Bu belge, **Faz 5 (Eczane & İlaç Yönetimi)** kapsamında uygulanan `F05-G07 — Teslim/ilaç verme` mimarisini, kısmi ve tam ilaç karşılama mantığını, atomik stok düşümünü ve denetim mekanizmalarını açıklar.

---

## 2. Teslim İş Akışı ve Doğrulama Kuralları

1. **Reçete Durumu ve Geçerlilik:** Yalnızca `Signed` veya `PartiallyDispensed` durumunda olan ve son geçerlilik tarihi (`ValidUntilUtc`) dolmamış reçeteler için teslim işlemi başlatılabilir.
2. **Miktar Doğrulaması (Over-dispense Prevention):** Her kalem için teslim edilecek miktar `0 < Miktar <= (Reçete Miktarı - Teslim Edilmiş Miktar)` kuralına uymak zorundadır. Fazla teslim talepleri `409 Conflict` ile reddedilir.
3. **FEFO Lot Seçimi ve Miat Kontrolü:** Eczacı ekranında (`PharmacyWorklist.razor`), her ilaç kalemi için `GET /api/v1/pharmacy/inventory/fefo-candidates/{medicationCatalogItemId}` çağrılarak kullanılabilir ve miadı dolmamış partiler FEFO sıralamasıyla listelenir.
   - Sunucu yalnız istemcinin seçimine güvenmez; seçilen lotun aynı ilaç, eczane bölümü ve sunucu tarafı FEFO sıralamasıyla uyumunu yeniden doğrular.
4. **Atomik Stok Düşümü:**
   - İlgili stok partisinin `QuantityOnHand` miktarı düşülür (`stockItem.DeductStock(quantity, nowUtc)`).
   - `medication_stock_transactions` tablosuna `Dispense` türünde, reçete numarası referansıyla işlem kaydı yazılır.
   - Reçete kaleminin `DispensedQuantity` miktarı artırılır (`prescription.RecordDispense(itemId, quantity, nowUtc)`).
   - Tüm kalemler karşılandığında reçete durumu otomatik olarak `Dispensed` olur; kısmi teslimde `PartiallyDispensed` olarak kalır.
   - `Pharmacy.PrescriptionDispense` denetim olayı `audit_privacy.audit_logs` tablosuna kaydedilir.
5. **Tekrarlı istek ve yarış koruması:**
   - Her istek zorunlu bir `IdempotencyKey`, reçetenin `ExpectedVersion` değeri ve her lotun `ExpectedStockVersion` değerini taşır.
   - Aynı anahtar ve aynı içerikle yeniden gönderim önceki sonucu döndürür, ikinci kez stok düşmez. Aynı anahtar farklı içerikle kullanılırsa veya sürüm bayatsa `409 Conflict` döner.
   - Başarılı istek parmak izi, klinik serbest metin içermeyen `prescription_dispense_operations` kaydında kalıcı tutulur.

---

## 3. API Uç Noktası

- **Metot / Yol:** `POST /api/v1/pharmacy/prescriptions/{id}/dispense`
- **Yetki:** Kesin `prescription.dispense` izni, reçetenin tesis kapsamı ve eczacının eczane bölüm ataması birlikte zorunludur; rol adına dayalı geçiş yoktur.
- **İstek Gövdesi (`DispensePrescriptionRequest`):**
  ```json
  {
    "idempotencyKey": "uuid",
    "expectedVersion": 2,
    "items": [
      {
        "itemId": "uuid",
        "stockItemId": "uuid",
        "expectedStockVersion": 1,
        "quantity": 1,
        "notes": "Teslim notu"
      }
    ]
  }
  ```

---

## 4. Kullanıcı Arayüzü Deneyimi

`PharmacyWorklist.razor` içerisinde:
- İmzalı veya kısmen karşılanan reçeteler incelendiğinde "İlaç Teslim Et" butonu belirir.
- Açılan modalda, kalan ihtiyacı olan her kalem için FEFO aday lotları listelenir ve teslim edilecek miktar seçilebilir.
- "Teslimi Onayla ve Stoktan Düş" tıklandığında teslim kaydedilir ve iş listesi canlı güncellenir.

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri (`PrescriptionDispenseUnitTests`):** Kısmi teslim, tam teslim, miktar aşımı engeli ve taslak reçete teslim reddi.
- **Bileşen Testleri (`PharmacyWorklistDispenseComponentTests`):** bUnit ile eczacı teslim modalı, FEFO lot seçimi ve teslim onayı akışı.
- **Entegrasyon Testleri (`PrescriptionDispenseIntegrationTests`):** PostgreSQL üzerinde kısmi/tam teslim, stok hareketleri, aynı isteğin güvenli tekrarı, aynı idempotency anahtarının farklı içerikle reddi, bayat sürüm çatışması ve miktar aşımı doğrulanır.
