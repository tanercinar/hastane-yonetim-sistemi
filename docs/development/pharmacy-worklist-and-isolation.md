# Eczane İş Listesi ve Klinik İzolasyon (Pharmacy Worklist and Clinical Isolation)

## 1. Genel Bakış ve Amaç

Bu belge, **Faz 5 (Eczane & İlaç Yönetimi)** kapsamında uygulanan `F05-G05 — Eczane iş listesi` mimarisini, eczacının reçete görüntüleme iş akışını, filtreleme parametrelerini ve hassas klinik not izolasyonunu açıklar.

---

## 2. Klinik İzolasyon İlkesi (Principle of Clinical Isolation)

- **Eczacı Erişim Sınırı:** Eczacı rolü (`PHA`), yalnız `prescription.view` ve `prescription.dispense` izinleriyle, kendi tesis kapsamındaki imzalı/kısmen teslim edilmiş ve süresi dolmamış reçeteleri görebilir. Rol tek başına erişim sağlamaz.
- **Minimum veri yanıtı:** Eczacı ayrıntısı hasta doğrulama kimliği, reçete numarası, durum, geçerlilik, ilaç kalemleri ve kullanım talimatlarıyla sınırlıdır.
- **İzole Edilen Veriler:** Tanı özeti, karşılaşma/bölüm/hekim kimlikleri, iptal veya hatalı giriş gerekçeleri, SOAP notları, psikiyatri öyküleri, konsültasyon metinleri ve laboratuvar/radyoloji ayrıntıları eczacı API yanıtına dahil edilmez.
- **Denetim İzi:** Eczacının iş listesini sorgulaması `Pharmacy.WorklistView` ve reçete detayını açması `Pharmacy.PrescriptionView` denetim olayları olarak `audit_privacy.audit_logs` tablosuna kaydedilir.

---

## 3. İş Listesi Filtreleri ve Parametreleri

`GET /api/v1/pharmacy/prescriptions/worklist` uç noktası aşağıdaki filtreleri destekler:

| Parametre | Tip | Açıklama |
|---|---|---|
| `status` | `string?` | Reçete durumu (`Signed`, `PartiallyDispensed`, `Dispensed`, `Cancelled`, `All`). Boş bırakıldığında varsayılan olarak aktif iş listesi (`Signed` ve `PartiallyDispensed`) döner. |
| `prescriptionNumber` | `string?` | Reçete numarasına göre arama (örn. `DEMO-RX-20260829-0001`). |
| `patientId` | `Guid?` | Belirli bir hastaya ait reçeteleri filtreler. |
| `maxResults` | `int?` | Maksimum sonuç sayısı (1-100 arası, varsayılan 50). |

---

## 4. Kullanıcı Arayüzü (`PharmacyWorklist.razor`)

- **Erişim:** `/pharmacy/worklist` rotası yalnız eczacı istemci rolü (`PHA`) için gösterilir. API ayrıca kesin izin, tesis kapsamı ve eczane bölüm atamasını zorunlu tutar. Diğer roller için `ForbiddenState` görüntülenir.
- **Filtreleme Çubuğu:** Durum seçimi ve reçete no arama filtreleri.
- **Reçete Detay Modalı:** Reçetedeki her bir kalemin reçete edilen miktarı, teslim edilen miktarı, kalan miktarı ve karşılama durumunu (`Bekliyor`, `Kısmi`, `Verildi`) gösterir.

---

## 5. Doğrulama ve Test Kapsamı

- **Bileşen Testleri (`PharmacyWorklistComponentTests`):** bUnit ile eczacı iş listesi render'ı, reçete detay modalı ve klinik izolasyon banner'ı, yetkisiz kullanıcılar için erişim engeli doğrulanmıştır.
- **Entegrasyon Testleri (`PharmacyWorklistIntegrationTests`):** PostgreSQL üzerinde doktorun reçete imzalaması, eczacının tesis kapsamlı iş listesini sorgulaması, taslak/süresi dolmuş reçetelerin filtrelenmesi, tanı ve klinik kimliklerin redaksiyonu ve denetim loglarının oluşumu doğrulanmıştır.
