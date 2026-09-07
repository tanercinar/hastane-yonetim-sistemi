# PACS ve DICOM Simülasyonu (F06-G07)

Bu doküman, Tanısal Hizmetler modülü altındaki **PACS/DICOM Simülasyonu ve Görüntüleyici (F06-G07)** bileşeninin mimarisini, veri modelini, HMAC imzalı kısa ömürlü erişim belirteci mantığını ve güvenlik kurallarını açıklar.

## 1. Mimari ve Amaç

Sistem gerçek bir hastanede kullanılan onaylı bir PACS veya DICOM cihazı değildir; eğitim ve portföy amacıyla geliştirilmiş güvenli bir **sentetik PACS/DICOM simülasyonudur**.

- Gerçek görüntüleme cihazı bağlantısı veya tescilli DICOM sunucusu bulunmaz.
- Radyoloji çekimleri (`RadiologyStudy`) tamamlandığında sentetik Study Instance UID, Series Instance UID ve SOP Instance UID hiyerarşisi üretilir.
- Çekim serileri ve kesitleri için süreç başına rastgele anahtarla HMAC-SHA256 imzalanan, kullanıcı kişi kimliğine bağlı beş dakikalık geçici erişim belirteçleri (`ViewToken`) kullanılır.
- Görüntüleyici, koyu temalı SVG tabanlı anatomik matriks ve belirgin filigran (`MOCK DICOM PREVIEW / SENTETIK PACS SIMULASYONU`) içeren güvenli sentetik görsel oluşturur.

---

## 2. API Sözleşmeleri ve Uç Noktalar

### Metadata ve Belirteç
- `GET /api/v1/diagnostics/radiology/studies/{id}/dicom-metadata`: İlgili çekimin DICOM çalışma, seri ve kesit meta verilerini listeler.
- `POST /api/v1/diagnostics/radiology/studies/{id}/dicom-preview-token`: Belirli bir SOP Instance UID için kısa ömürlü imzalı belirteç üretir.
- `POST /api/v1/diagnostics/radiology/dicom-preview`: İmzalı belirteç URL'ye yazılmadan JSON gövdesinde iletilir; kimliği doğrulanmış aynı kişi ve güncel kaynak yetkisi doğrulanarak dinamik SVG DICOM önizleme görseli sunulur.

---

## 3. Güvenlik ve Yetkilendirme

- Önizleme uç noktası anonim değildir ve antiforgery doğrulaması ister.
- Belirteç URL/query string, log, telemetry veya hata mesajına yazılmaz.
- Başka bir oturuma aktarılan, tahrif edilen, süresi dolan ya da erişimi sonradan kaldırılan belirteç reddedilir.

1. **HMAC-SHA256 İmzası**: Önizleme belirteci `StudyId`, `InstanceUid`, `Modality`, `ProcedureName`, `AccessionNumber` ve `ExpiresAtUtc` bilgilerini içerir. Tahrif edilmiş belirteçler reddedilir (`400 Bad Request`).
2. **Kısa Ömürlülük (Time-to-Live)**: Belirteçler 5 dakika geçerlidir; süresi dolan istekler `403 Forbidden` ile engellenir.
3. **Hasta IDOR Sınırı**: Hastalar yalnızca kendi adlarına ait ve durumu `ReportFinalized` veya `AddendumAdded` olan çekimlerin DICOM verilerine erişebilir; çekim aşamasındaki veya başkasına ait kayıtlara erişemez.
4. **Denetim İzi (Audit)**: DICOM metadata ve önizleme erişimleri `Diagnostics.DicomMetadataAccess` ve `Diagnostics.DicomPreviewAccess` olarak audit günlüğüne kaydedilir.
