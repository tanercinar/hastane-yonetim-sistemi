# MEDULA/SGK ve İTS/ÜTS Sınırları ve Mock Sözleşmeleri

> **MOCK & BOUNDARY CONTRACT** — Bu modül gerçek MEDULA/SGK, İTS (İlaç Takip Sistemi) veya ÜTS (Ürün Takip Sistemi) sunucularına bağlanmaz. Sistem sertifikalı bir HBYS ya da mali/fatura sistemi **DEĞİLDİR**.

## 1. Genel Bakış ve Kapsam Sınırları

Hastane Yönetim Sistemi (HMS) mimarisinde MEDULA/SGK, İTS ve ÜTS süreçleri için demo seviyesinde doğrulama ve olay kontratları tanımlanmıştır. Gerçek SGK provizyonu, faturalandırma, mali mutabakat veya gerçek İTS/ÜTS bildirimi sistem kapsamı dışındadır.

### Temel Prensipler
1. **Sahte Başarılı Provizyon Yasağı:** Gerçek dışı bir finansal başarı durumu kullanıcıya veya sisteme gerçekmiş gibi yansıtılmaz; tüm demo yanıtları `DEMO` ibaresi ve yasal sorumluluk reddi (`DemoDisclaimer`) taşır.
2. **Finansal Model Eklenmez:** Kapsam dışı finans, satın alma, faturalama ve bordro modelleri erken aşamada eklenmez.
3. **Açık Kapsam Dışı / Uygulanmadı Durumu:** Gerçek takip ve onay gerektiren işlemler `NotImplemented` veya `OutOfScope` durumlarıyla açıkça reddedilir.

---

## 2. İşlem Türleri ve Durum Haritası

| İşlem Türü (`MedulaOperationType`) | Kod | Varsayılan Durum | Açıklama |
|---|---|---|---|
| `ProvizyonSorgu` | 1 | `NotImplemented` | Demo provizyon sorgusu; gerçek SGK bağlantısı yoktur. |
| `ProvizyonTeyit` | 2 | `OutOfScope` | Finansal teyit/onay kapsam dışıdır. |
| `HakSahibiDogrulama` | 3 | `DemoSuccess` | Sentetik TC kimlik ile hak sahipliği simülasyonu. |
| `ItsTeslimBildirimi` | 4 | `NotImplemented` | İTS ilaç karekod teslim bildirimi simülasyonu. |
| `UtsDogrulamaSorgusu` | 5 | `NotImplemented` | ÜTS tıbbi cihaz barkod doğrulaması simülasyonu. |

---

## 3. Mimari Bileşenler

- **Domain Model:** `HospitalManagement.Modules.Interoperability.Domain.Medula`
  - `MedulaOperationType`: İşlem enum değerleri.
  - `MedulaOperationStatus`: `NotImplemented`, `DemoSuccess`, `DemoRejected`, `OutOfScope`.
  - `MedulaOperationResult`: Her sonuç için `DemoDisclaimer`, `RequestSummary`, `ResponseSummary` ve zaman damgası üreten fabrika metotları.
- **Application Katmanı:**
  - `IMedulaBoundaryService`: Demo işlem çalıştırma, kapsam dışı reddetme ve sınır bilgilerini sorgulama arayüzü.
  - `MedulaOperationResultDto`: Dışa aktarılan veri transfer nesnesi.
- **Infrastructure Katmanı:**
  - `MedulaBoundaryService`: `IIntegrationMockEngine` üzerinden hata enjeksiyonu ve devre kesici desteği ile çalışan servis.
- **API & Kontratlar:**
  - `GET /api/v1/interoperability/medula/boundaries`: Tüm sınır ve işlem türü bilgilerini listeler.
  - `POST /api/v1/interoperability/medula/demo-operation`: Belirtilen işlem türü için demo yanıtı üretir.
  - `POST /api/v1/interoperability/medula/reject`: Kapsam dışı finansal işlemleri açık gerekçeyle reddeder.

---

## 4. Güvenlik ve Gizlilik

- Gerçek hasta veya kurum bilgisi kullanılmaz; tüm veriler sentetiktir.
- Gönderilen payload'lar loglarda PHI/KVKK kapsamında maskelenir ve özetlenir.
- Sistem yöneticisi veya klinik personelin finansal işlem yapmaya çalışması durumunda açıklayıcı hata/red mesajı döner.

---

## 5. Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Interoperability/MedulaBoundaryDomainTests.cs` (8/8 PASS)
  - Tüm işlem türlerinin doğru statü ve `DemoDisclaimer` üretmesi.
  - Kapsam dışı redlerin (`OutOfScope`) açık gerekçe içermesi.
- **Integration Tests:** `tests/HospitalManagement.IntegrationTests/MedulaBoundaryIntegrationTests.cs` (3/3 PASS)
  - Gerçek PostgreSQL ve WebApplicationFactory ile `/api/v1/interoperability/medula/` uç noktalarının test edilmesi.
