# Performans ve Yarış Koşulları Sertleştirmesi (F13-G07)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin yüksek eşzamanlılık (high concurrency), yarış koşulları (race conditions), bellek tüketim sınırları (bounded memory / pagination clamping) ve asenkron yük altındaki davranışlarını ve alınan mimari önlemleri belgeler.

## 2. Eşzamanlılık ve Yarış Koşulu Güvenlik Mekanizmaları

### 2.1. Randevu Slot Rezervasyonu (Conflicting Appointment Booking)
- **Problem**: İki farklı hastanın aynı hekimin aynı randevu saatini (`AppointmentSlot`) milisaniyeler farkla rezerve etmeye çalışması.
- **Mekanizma**: Domain seviyesinde `AppointmentSlot.Book` durumu atomik olarak kontrol eder ve durumu `SlotStatus.Booked` yapar. İkinci işlem `InvalidOperationException` ile reddedilir.
- **Veri Tabanı Seviyesi**: `UX_AppointmentSlots_Practitioner_Start` PostgreSQL benzersiz indeksi ve `xmin` / `RowVersion` optimistik eşzamanlılık kilidi çift rezervasyonu imkansız kılar.

### 2.2. Yatan Hasta Yatak Rezervasyonu (Conflicting Bed Assignment)
- **Problem**: İki farklı kabul/transfer işleminin aynı servis yatağını (`Bed`) aynı anda rezerve etmeye çalışması.
- **Mekanizma**: `Bed.Reserve` işlemi mevcut yatak durumunu doğrular; yatak müsait değilse (`BedStatus.Reserved` veya `Occupied`) derhal `InvalidOperationException` fırlatılır.
- **Veri Tabanı Seviyesi**: `Version` ve tekil yatak/yatış kısıtları ile çakışan işlem `409 Conflict` üretir.

### 2.3. Eczane Stok Düşümü ve FEFO Tükenmesi (Stock Limit Invariants)
- **Problem**: Eşzamanlı reçete karşılama (`Dispense`) sırasında mevcut parti stok miktarının altına düşülmesi veya negatif stok oluşması.
- **Mekanizma**: `MedicationStockItem.DeductStock` operasyonu talep edilen miktarın `AvailableQuantity`'den büyük olması durumunda işlemi anında engeller ve negatif stok oluşmasını önler.
- **Veri Tabanı Seviyesi**: `CK_MedicationStockItems_AvailableQuantity_NonNegative` veri tabanı CHECK kısıtı ile veri seviyesinde negatif stok oluşması engellenmiştir.

### 2.4. Raporlama Projeksiyonları Yük Doğruluğu (Projection Invariants Under Load)
- **Problem**: Yüksek hacimli klinik olayların asenkron projeksiyon motoruna ardışık ve yoğun gelmesi durumunda metrik sayaçlarının sapması.
- **Mekanizma**: `IReportingProjectionEngine` idempotent olay işleme mantığı ile tasarlanmıştır. `ProjectionProcessedEvent` tekilliği ve deterministik sayaç artırımları (`EncounterStarted`, `EncounterCompleted`) sayesinde yük altında sayaç tutarlılığı %100 korunur.

### 2.5. Sayfalama ve Bellek Sınırlandırması (Pagination Clamping & Bounded Memory)
- **Problem**: İstemcinin `pageSize=100000` gibi kötü niyetli veya aşırı büyük parametreler göndererek sunucu belleğini tüketmesi (DoS).
- **Mekanizma**: Tüm API uç noktalarında ve sorgularda sayfa boyutu `Math.Clamp(pageSize, 1, 100)` (veya güvenli dışa aktarma için en fazla 5000 satır) ile sınırlandırılmıştır. İstemci ne talep ederse etsin tek seferde çekilebilecek nesne sayısı kontrol altındadır.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Performance/ConcurrencyAndPerformanceTests.cs`:
  - `AppointmentSlotBookingConcurrentRequestsOnlyFirstSucceedsAndSecondFails`
  - `BedAssignmentConcurrentReservationOnlyFirstSucceedsAndSecondFails`
  - `MedicationStockDeductionExceedingAvailableQuantityThrowsInvalidOperationException`
  - `ReportingProjectionTransitionAccuracyUnderLoad`
  - `PaginationClampingGuaranteesBoundedMemory`
