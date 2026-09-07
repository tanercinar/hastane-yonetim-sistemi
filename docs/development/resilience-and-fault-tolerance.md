# Hata ve Dayanıklılık Sertleştirmesi (F13-G09)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin veri tabanı kesintileri, harici servis (e-Nabız, MHRS, HL7, DICOM PACS) arızaları, ağ kopmaları, asenkron projeksiyon yeniden kurma hataları ve beklenmeyen sunucu istisnaları karşısındaki dayanıklılık (resilience), güvenli hata ifşası (safe error disclosure) ve veri tutarlılığı mekanizmalarını belgeler.

## 2. Dayanıklılık ve Hata Yönetim Mimarisi

### 2.1. Güvenli Yeniden Deneme (SafeRetryHandler & Idempotency)
- **Katı Kural**: Yalnızca güvenli ve idempotent HTTP istekleri (`GET`, `HEAD`, `OPTIONS`, `PUT`, `DELETE` veya `Idempotency-Key` başlığı taşıyan istekler) 503 (Service Unavailable) veya ağ zaman aşımında üstel geri çekilme (exponential backoff) ile yeniden denenir.
- **Güvensiz POST İstekleri**: `Idempotency-Key` başlığı bulunmayan `POST` istekleri (örneğin reçete karşılama, yatak atama, ödeme simülasyonu) mükerrer kaynak yaratılmasını ve mükerrer stok düşümünü önlemek için **asla otomatik olarak yeniden denenmez**.
- **İstemci Hataları**: 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found) gibi istemci kaynaklı hatalar yeniden denenmeden derhal kullanıcıya iletilir.

### 2.2. Güvenli Hata İfşası (RFC 9457 Problem Details)
- **Sıfır Yığın İzi (Zero Stack Trace Leakage)**: Sunucu tarafında beklenmeyen bir hata oluştuğunda (`500 Internal Server Error`), `ApiExceptionHandler` ve `ApiProblemDetailsDefaults` devreye girerek dahili hata ayrıntılarını, SQL sorgu metinlerini, sunucu adlarını veya veri tabanı yığın izlerini istemciye sızdırmaz.
- **Standart Problem Details Biçimi**:
  - `status`: 500
  - `code`: `"server_error"`
  - `title`: `"Sunucu hatası"`
  - `detail`: `"İstek işlenirken beklenmeyen bir hata oluştu."`
  - `correlationId`: Benzersiz istek korelasyon kimliği.
- **Güvenli İstemci Ayrıştırması (`ProblemDetailsDto`)**: Sunucu JSON formatı dışında ham bir hata dönse dahi istemci katmanı kullanıcıya `"API çağrısı başarısız oldu (HTTP 500)."` şeklinde güvenli bir genel hata gösterir.

### 2.3. Asenkron Projeksiyon ve Outbox İdempotent Kurtarma
- **Olay Tekilleştirmesi (`ProjectionProcessedEvent`)**: Raporlama projeksiyon motoru işlediği her olayın `EventId` değerini kaydeder. Bir kesinti veya servis yeniden başlatması sonrası aynı olay tekrar iletildiğinde, motor olayı tespit eder ve sayaçları mükerrer artırmaz.
- **Projeksiyon Kontrol Noktası (`ProjectionCheckpoint`)**: Her projeksiyon en son işlediği pozisyonu monotonik olarak ilerletir.

### 2.4. Harici Entegrasyon Mock Hata Enjeksiyonu ve Sınır Koruması
- **`IIntegrationMockEngine`**: Harici sistemlerin (e-Nabız, MHRS, HL7 v2, DICOM MWL, Medula) simülasyonlarında gerçekçi hata modelleri desteklenir:
  - `Latency`: Ağ gecikmesi simülasyonu.
  - `TransientError`: Geçici servis kesintisi ve yeniden deneme doğrulama.
  - `CorruptPayload`: Bozuk yük simülasyonu ve güvenli doğrulama reddi.
  - `CircuitBroken` & `Offline`: Harici servis tamamen kapalıyken yerel işlemin korunması.
- **Katı Parametre Sınırları**: `MockServerConfiguration` gelen gecikme, hata oranı, yeniden deneme ve zaman aşımı parametrelerini operasyonel sınırlarla kilitler (maksimum 30s gecikme, 10 deneme, 120s zaman aşımı).

### 2.5. Çevrimdışı Bariyeri (Offline Barrier & Zero Uncommitted Queue)
- **Klinik Veri Bütünlüğü**: Sistem yerel istemcide (Web veya MAUI) kontrolsüz çevrimdışı veri yazma kuyruğu tutmaz. Canlı sunucu bağlantısı koptuğunda `ConnectionRequiredState` engeli devreye girer ve yarım kalmış/doğrulanmamış verilerin sunucuya yazılmış gibi gösterilmesi kesin olarak engellenir.

## 3. Otomatik Doğrulama

- `tests/HospitalManagement.UnitTests/Resilience/ResilienceAndFaultToleranceTests.cs`:
  - `MockServerConfigurationEnforcesStrictClampingBounds`
  - `MockServerConfigurationUnderflowParametersClampToFloorLimits`
  - `ProjectionCheckpointAndProcessedEventProvideIdempotencyGuarantees`
  - `FaultInjectionModesCoverAllRequiredResilienceProfiles`
- `tests/HospitalManagement.ComponentTests/Client/SafeRetryHandlerTests.cs`:
  - `SafeRetryHandlerShouldRetryGetOn503ServiceUnavailable`
  - `SafeRetryHandlerShouldNotRetryNonIdempotentPostOn503`
  - `SafeRetryHandlerShouldRetryPostWhenIdempotencyKeyHeaderIsPresent`
  - `SafeRetryHandlerShouldNotRetryClientErrors`
- `tests/HospitalManagement.ComponentTests/Client/ProblemDetailsParsingTests.cs`:
  - `ReadContentOrThrowAsyncShouldParseRfc9457ProblemDetailsOn400`
  - `ReadContentOrThrowAsyncShouldFallbackGracefullyOnNonJsonError`
