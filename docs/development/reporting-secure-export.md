# Raporlama Dışa Aktarma Güvenliği (Secure CSV Export)

Bu belge, **F11-G06** görevi kapsamında uygulanan güvenli CSV dışa aktarma mimarisini, satır limitlerini, formül enjeksiyonu (CSV Formula Injection) korumasını, audit günlüğünü ve dosya depolama güvenliği ilkelerini açıklar.

## 1. Amaç ve Tehdit Modeli

Operasyonel raporların dışa aktarımı, yanlış yapılandırıldığında iki büyük güvenlik zafiyetine yol açabilir:
1. **CSV / Spreadsheet Formula Injection (CWE-1236):** Hücre içerisine gömülen `=cmd`, `+`, `-`, `@`, `\t` veya `\r` ile başlayan girdilerin Excel veya Calc gibi e-tablo yazılımlarında çalıştırılması.
2. **Yetkisiz Toplu Veri Çıkarımı ve DoS:** Satır sınırı olmadan devasa veri setlerinin sorgulanması ve sistem kaynaklarının tüketilmesi.
3. **Kalıcı Public Dosya Sızıntısı:** Dışa aktarılan dosyaların genel erişime açık statik klasörlerde (`/wwwroot/downloads` vb.) unutulması.

## 2. Alınan Güvenlik Önlemleri

### 2.1. İki Kademeli Yetkilendirme (Permission Boundary)
- Raporları ekranda görüntülemek için `report.operations.view` yeterlidir.
- Raporları dosya olarak dışa aktarmak için yalnızca yönetim rollerine (`ChiefMedicalOfficer`, `HospitalManager`) tanınan `report.operations.export` izni zorunludur. Hekim veya yetkisiz personel doğrudan `403 Forbidden` alır.

### 2.2. Formül Enjeksiyonu Koruması (Formula Injection Sanitization)
`SecureExportService.SanitizeCsvCell` metodu tüm metin alanlarını inceler:
- Hücre değeri `=`, `+`, `-`, `@`, `\t` veya `\r` ile başlıyorsa, değerin başına tek tırnak (`'`) eklenir. Bu işaret, e-tablo programlarının hücreyi kesinlikle formül olarak çalıştırmasını engeller ve düz metin olarak yorumlanmasını sağlar.
- RFC 4180 standardına göre virgül veya tırnak içeren değerler çift tırnakla çevrilir (`"..."`).

### 2.3. Satır Sınırı (Max Row Limit)
- Tek bir dışa aktarma işleminde azami **5000 satır** sınırı uygulanır (`MaxExportRows = 5000`).
- Bu sınırı aşan istekler reddedilir (`400 Bad Request`).

### 2.4. Sıfır Kalıcı Depolama (In-Memory Streaming)
- Sunucu diskinde veya statik dosya dizininde hiçbir geçici CSV dosyası tutulmaz.
- Veri doğrudan bellek içi akış (`Results.File(bytes, "text/csv", filename)`) olarak istemciye iletilir.
- Türkçe karakter desteği için `UTF-8 BOM` kullanılır.

### 2.5. Audit Denetim İzi
Her dışa aktarma işlemi klinik ve kişisel veri sızdırmadan `IAuditEventPublisher` aracılığıyla `report.operations.export` aksiyonu ile günlüğe kaydedilir (aktaran kullanıcı kimliği, rapor türü, satır sayısı, correlation ID).

## 3. API Uç Noktaları

| Metod | Uç Nokta | İzin | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/reporting/exports/csv?reportType={type}&startDate={s}&endDate={e}` | `report.operations.export` | Tarayıcı doğrudan indirmeleri için |
| `POST` | `/api/v1/reporting/exports/csv` | `report.operations.export` | Gövde üzerinden filtreli güvenli export (Antiforgery korumalı) |

Desteklenen rapor türleri: `outpatient-metrics`, `diagnostic-workload`, `bed-occupancy`, `pharmacy-dispensing`.
