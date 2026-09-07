# Yatan Hasta Transfer ve Yatak Hareketleri (Inpatient Transfers & Bed Movements)

## Genel Bakış

Bu belge, **Faz 7: Yatan Hasta Yönetimi** kapsamındaki **F07-G03 — Transfer ve yatak hareketleri** görevi için geliştirilen servisler ve odalar arası hasta nakil/transfer iş akışını, durum geçişlerini, atomik yatak hareketlerini ve denetim mekanizmalarını açıklar.

## Durum Modeli (TransferStatus)

Hasta transferi yaşam döngüsü aşağıdaki durumları izler:

```
[Requested] (Hekim / Hemşire transfer talebi oluşturdu, hedef servis belirlendi)
   │
   ├─► [Cancelled] (İptal edildi, rezerve hedef yatak varsa serbest bırakılır)
   │
   ▼
[Accepted] (Hedef servis transferi onayladı, opsiyonel yatak rezerve edildi)
   │
   ├─► [Cancelled] (İptal edildi, rezerve hedef yatak serbest bırakılır)
   │
   ▼
[Completed] (Transfer tamamlandı)
   │
   ├──► Eski Yatak: Occupied -> Cleaning (Temizliğe alındı, hasta ilişiği kesildi)
   ├──► Yeni Yatak: Available/Reserved -> Occupied (Hasta ve yatış kaydı atandı)
   └──► Yatış Kaydı: DepartmentId, AdmittingWardId ve AssignedBedId güncellendi, Transferring -> Admitted
```

## İş Kuralları ve Değişmezler (Invariants)

1. **Tek Aktif Transfer Kuralı:**
   - Bir yatış için aynı anda yalnızca bir adet beklemede veya kabul edilmiş (`Requested`, `Accepted`) transfer süreci bulunabilir.
   - Çakışan transfer talepleri `409 Conflict` ile reddedilir.

2. **Atomik Yatak Durumu Güncellemesi (Atomic Bed State Transition):**
   - Transfer tamamlandığında (`CompleteTransferAsync`), eski kaynak yatak (`SourceBedId`) otomatik olarak `Cleaning` (Temizlik) durumuna geçirilir ve hasta ilişiği kesilir.
   - Yeni hedef yatak (`TargetBedId`) atomik olarak `Occupied` durumuna geçer ve hastanın `CurrentAdmissionId` ve `CurrentPatientId` bilgileri atanır.
   - Yatış kaydının `DepartmentId`, `AdmittingWardId` ve `AssignedBedId` alanları hedef servisin organizasyon bilgisiyle güncellenir ve durumu `Admitted` olarak ayarlanır.

3. **Gerekçeli İptal:**
   - Transfer sürecinde (`Requested` veya `Accepted`), işlem gerekçe belirtilerek iptal edilebilir.
   - İptal durumunda hedef yatak rezervasyonu kaldırılır, hasta mevcut yatağında `Admitted` durumunda kalmaya devam eder.

4. **Klinik Denetim İzi (Audit Logging):**
   - Transfer olayları `audit_privacy.audit_logs` tablosuna kaydedilir:
     - `Inpatient.TransferRequest`
     - `Inpatient.TransferAccept`
     - `Inpatient.TransferComplete`
     - `Inpatient.TransferCancel`

5. **Eşzamanlılık (Optimistic Concurrency):**
   - `InpatientTransfer` varlığı `Version` concurrency token'ı ile korunur.
   - Bir yatış için aktif transfer PostgreSQL kısmi unique index'i ile de tekilleştirilir; yarışın kaybedeni `409 Conflict` alır.

6. **Kaynak Kapsamı ve Hedef Ekip Devri:**
   - Kaynak servis ekibi yalnız erişebildiği yatış için transfer isteği açabilir. Hedef servis listesi klinik veri taşımayan operasyonel `transfer-destinations` projection'ından gelir.
   - Hedef servis ekibi kendi servisindeki gelen talebi listeler, kabul eder ve hedef yatak kapsamında tamamlar.
   - Transferi kabul eden kullanıcıya kalıcı klinik erişim verilmez; erişim aktif bölüm ataması/bakım ilişkisiyle yeniden hesaplanır.

## API Endpoint'leri

- `GET /api/v1/inpatient/wards/transfer-destinations` — Klinik veri içermeyen aktif hedef servis kataloğu (`bed.transfer`)
- `POST /api/v1/inpatient/transfers` — Kaynak yatış kapsamında yeni transfer talebi (`bed.transfer`)
- `POST /api/v1/inpatient/transfers/{id}/accept` — Hedef servis kapsamında transferi kabul etme (`bed.transfer`)
- `POST /api/v1/inpatient/transfers/{id}/complete` — Hedef yatak kapsamında transferi atomik tamamlama (`bed.transfer`)
- `POST /api/v1/inpatient/transfers/{id}/cancel` — Görülebilir aktif transferi gerekçeyle iptal etme (`bed.transfer`)
- `GET /api/v1/inpatient/transfers/{id}` — Transfer detayı
- `GET /api/v1/inpatient/transfers` — Transfer hareketleri listesi (yatış, kaynak servis, hedef servis, durum filtreli)

## Test Kapsamı

- **Unit Tests:** `tests/HospitalManagement.UnitTests/Inpatient/TransferDomainTests.cs` (5 test)
- **Component Tests:** `tests/HospitalManagement.ComponentTests/InpatientTransfersComponentTests.cs` (1 test)
- **PostgreSQL Integration Tests:** `tests/HospitalManagement.IntegrationTests/InpatientTransferIntegrationTests.cs` (1 kapsamlı test — kaynak ve hedefte farklı bölüm atamaları, kaynak ekibin istek açması, hedef ekibin liste/kabul/tamamlama işlemi, eski yatağın `Cleaning`, yeni yatağın `Occupied` olması, bölüm/servis/yatak devri, kalıcı kabul-eden erişimi oluşmaması ve audit doğrulaması)
