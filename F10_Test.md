# Faz 10 Manuel Test Rehberi

Bu rehber yalnız sentetik `DEMO` veriyle çalıştırılmalıdır. Gerçek kimlik, hasta verisi,
kurum endpoint'i, API anahtarı veya iletişim bilgisi kullanmayın.

> **Kapı durumu:** Otomatik ve manuel yürütme sonucu `ROADMAP.md` ilerleme günlüğünde tutulur.

## 1. Ön koşullar

Repository kökünde:

```powershell
docker info --format "{{.ServerVersion}}"
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F10"
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F10"
dotnet test .\HospitalManagement.slnx --configuration Release --no-build --no-restore
dotnet format .\HospitalManagement.slnx --no-restore --verify-no-changes
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Beklenen: build 0 uyarı/0 hata; testlerde fail/skip yok; belge doğrulamasında bozuk bağlantı yok.

Yerel sistemi başlatın:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` için `200` bekleyin. HTTP isteklerinde önce
`GET /api/v1/identity/antiforgery` çağırın; yazma isteklerine dönen cookie ile
`X-HMS-CSRF` header'ını birlikte ekleyin.

| Rol | E-posta | Parola |
|---|---|---|
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |
| Başhekim | `DEMO-chief@hospital.invalid` | `DEMO-Chief-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |

## 2. Sonuç matrisi

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:

F10-M01 MOCK sınırı ve contract       : PASS / FAIL
F10-M02 Timeout ve güvenli log         : PASS / FAIL
F10-M03 Retry ve bozuk cevap           : PASS / FAIL
F10-M04 Duplicate/idempotency          : PASS / FAIL
F10-M05 Yetkisiz FHIR export           : PASS / FAIL
F10-M06 Dış arıza/ana kayıt dayanıklılığı: PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

## 3. F10-M01 — MOCK sınırı ve contract

1. OpenAPI belgesini açın: `https://localhost:7111/openapi/v1.json`.
2. FHIR, HL7, DICOM, MHRS, e-Nabız, MEDULA ve mock-engine yollarının `/api/v1/interoperability`
   altında olduğunu doğrulayın.
3. Kaynak kodu ve Network panelini inceleyin. Gerçek bakanlık/SGK/PACS/SMS/SMTP endpoint'ine
   giden istek, gerçek credential veya AI servisi olmamalıdır; çıktılar `MOCK`/`DEMO` etiketlidir.
4. Hasta hesabıyla `GET /api/v1/interoperability/mock-engine/configs` çağırın: `403` bekleyin.
   Sistem yöneticisiyle aynı çağrıda `200` bekleyin.

## 4. F10-M02 — Timeout ve güvenli log

1. Sistem yöneticisiyle FHIR ayarını `LatencyMilliseconds=1500`, `TimeoutSeconds=1`,
   `MaxRetryAttempts=0`, `FaultMode=Latency` yapın.
2. `/mock-engine/simulate` çağrısında payload'a yalnız sentetik bir canary yazın.
3. Yaklaşık bir saniyede `IsSuccess=false` ve `MOCK_INTEGRATION_TIMEOUT` bekleyin.
4. `/mock-engine/logs?systemType=Fhir` yanıtında canary/payload/exception yerine sabit teknik
   sınıflandırma ve güvenli hata kodu bulunmalıdır.

## 5. F10-M03 — Retry ve bozuk cevap

1. DICOM ayarını `TransientError`, `MaxRetryAttempts=2` yapıp simulate çağırın.
   `IsSuccess=true`, `RetryAttempts=1` ve tek `Retried` log bekleyin.
2. HL7 ayarını `CorruptPayload` yapın. Çağrı `MOCK_INTEGRATION_CORRUPT_PAYLOAD` ile
   başarısız olmalı, retry sayısı `0` kalmalı ve payload loga taşınmamalıdır.
3. Ayarları test sonunda `FaultMode=None`, gecikme `0`, hata oranı `0` değerlerine döndürün.

## 6. F10-M04 — Duplicate/idempotency

1. Aynı MHRS slotu, hasta ve idempotency anahtarıyla iki kez randevu gönderin.
2. İki yanıt aynı `MhrsAppointmentId` değerini döndürmeli; listede tek kayıt bulunmalıdır.
3. Aynı slotu farklı idempotency anahtarı ve hasta için ayırmayı deneyin. İkinci klinik kayıt
   oluşmamalı ve güvenli conflict/başarısız contract dönmelidir.

## 7. F10-M05 — Yetkisiz FHIR export

Kanonik sentetik hasta GUID'i `00000000-0000-0000-0000-000000000121` ile
`GET /api/v1/interoperability/fhir/r4/Patient/{id}/$export` çağırın:

- oturumsuz: `401`;
- hasta: `403`;
- başhekim: `200`, `Bundle` ve `type=collection`;
- FHIR ayarı `Offline` iken başhekim: klinik/exception ayrıntısı içermeyen `503 Problem Details`.

## 8. F10-M06 — Dış arıza ana kaydı bozmamalı

1. Otomatik `MockProviderFailureDoesNotRollBackBookedAppointment` testini tek başına çalıştırın.
2. Mock bildirim taşıyıcısı üç denemede hata verirken randevu API'si `201` dönmeli ve Scheduling
   kaydı veritabanında kalmalıdır.
3. Outbox `Failed`, retry sayısı `3` olmalı; taşıyıcı exception canary'si yanıt, log veya outbox
   hata alanında bulunmamalıdır.

Herhangi bir gerçek dış ağ çağrısı, yetkisiz export, payload/exception sızıntısı, timeout'un
uygulanmaması, duplicate kayıt veya dış sistem arızasında randevunun kaybolması genel kararı
`FAIL` yapar.
