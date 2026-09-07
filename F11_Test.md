# Faz 11 Manuel Test Rehberi

Bu rehber yalnız sentetik `DEMO` veriyle çalıştırılmalıdır. Gerçek hasta verisi,
gerçek kimlik veya finansal/satınalma bilgisi içermez.

> **Kapı durumu:** Otomatik ve manuel yürütme sonucu `ROADMAP.md` ilerleme günlüğünde tutulur.

## 1. Ön koşullar

Repository kökünde:

```powershell
dotnet build .\HospitalManagement.slnx --configuration Release --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F11|FullyQualifiedName~Reporting"
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Roadmap~F11|FullyQualifiedName~Reporting"
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

`https://localhost:7111/health/ready` için `200` bekleyin.

| Rol | E-posta | Parola |
|---|---|---|
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |
| Başhekim | `DEMO-chief@hospital.invalid` | `DEMO-Chief-Pass!1` |
| Hekim | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` |
| Kayıt personeli | `DEMO-registration@hospital.invalid` | `DEMO-Reg-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |

## 2. Sonuç matrisi

```text
Test eden:
Tarih/saat:
.NET SDK:
PostgreSQL:

F11-M01 Read model idempotency ve rebuild          : PASS / FAIL
F11-M02 Poliklinik ve randevu panosu               : PASS / FAIL
F11-M03 Tanısal hizmetler panosu ve yetki sınırı   : PASS / FAIL
F11-M04 Yatak, acil ve ameliyathane panosu         : PASS / FAIL
F11-M05 Eczane ve stok panosu (finansal verisiz)   : PASS / FAIL
F11-M06 Güvenli CSV export ve formula injection    : PASS / FAIL
F11-M07 SignalR canlı yenileme, TM-13 ve lag       : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

## 3. F11-M01 — Read model idempotency ve rebuild

1. Başhekim hesabı ile `POST /api/v1/reporting/projections/rebuild` çağırın: `200 OK` ve `Success=true` bekleyin.
2. `GET /api/v1/reporting/projections/checkpoints` çağırın: 4 adet projeksiyonun (`DailyOutpatient`, `DiagnosticWorkload`, `BedOccupancy`, `PharmacyDispensing`) `Status=Active` olduğunu doğrulayın.
3. Aynı olay birden fazla kez fırlatıldığında `ProjectionProcessedEvents` tablosundaki `EventId` tekilliği ile sayaçların mükerrer artmadığını doğrulayın.

## 4. F11-M02 — Poliklinik ve randevu panosu

1. Tarayıcıda `/reporting/outpatient` adresini açın.
2. Başhekim veya Hastane Yöneticisi girişinde tüm hastane toplamlarını, bölüm bazlı ve hekim bazlı randevu dağılımlarını görüntüleyin.
3. Hekim hesabı (`DEMO-doctor@hospital.invalid`) ile giriş yapıldığında hastane geneli hasta adlarının görünmediğini, hekimin kendi kapsamıyla filtrelenmiş operasyonel sayaçları gördüğünü doğrulayın.

## 5. F11-M03 — Tanısal hizmetler panosu ve yetki sınırı

1. Tarayıcıda `/reporting/diagnostics` adresini açın.
2. Laboratuvar ve Radyoloji iş listesi sayaçlarını (bekleyen numune, işlemde, sonuçlanan, ortalama TAT) izleyin.
3. Kritik sonuç uyarısına tıklandığında klinik drill-down için hekim yetkisi ve bakım ilişkisi kontrolü yapıldığını, yetkisiz personelin klinik ayrıntıya erişemediğini doğrulayın.

## 6. F11-M04 — Yatak, acil ve ameliyathane panosu

1. Tarayıcıda `/reporting/inpatient-operations` adresini açın.
2. Servis bazlı yatak doluluk tablosunu, boş yatakları ve bekleyen transferleri inceleyin.
3. Acil triyaj bölümünde Kırmızı, Sarı, Yeşil hasta kuyrukları ve bekleme sürelerinin gerçek zamanlı gösterildiğini doğrulayın.

## 7. F11-M05 — Eczane ve stok panosu (finansal verisiz)

1. Tarayıcıda `/reporting/pharmacy` adresini açın.
2. Bekleyen reçete çıkışları, teslim edilenler, asgari stok seviyesi altındaki kritik kalemler ve miat yaklaşan lotları inceleyin.
3. Ekranda veya API çıktısında hiçbir birim fiyat, toplam tutar, fatura veya satınalma verisinin bulunmadığını doğrulayın.

## 8. F11-M06 — Güvenli CSV export ve formula injection

1. Hekim veya kayıt personeli hesabıyla `GET /api/v1/reporting/exports/csv?reportType=outpatient-metrics` çağırın: `403 Forbidden` bekleyin.
2. Başhekim hesabıyla aynı isteği yapın: `200 OK`, `Content-Type: text/csv; charset=utf-8` bekleyin.
3. İndirilen CSV dosyasını metin editöründe açın:
   - Başında UTF-8 BOM bulunduğunu,
   - `=cmd`, `@SUM`, `+`, `-` ile başlayan alanların tek tırnak (`'`) ile nötralize edildiğini,
   - Diskte kalıcı hiçbir dosya bırakılmadığını (in-memory streaming) doğrulayın.
4. Satır sınırını (5000 satır) aşan isteklerin reddedildiğini doğrulayın.

## 9. F11-M07 — SignalR canlı yenileme, TM-13 ve projeksiyon gecikmesi

1. İki tarayıcı penceresi açın: Birincisinde `/reporting/outpatient` panosu, ikincisinde randevu kayıt ekranı.
2. İkinci pencerede yeni bir randevu check-in yapıldığında, birinci penceredeki sayfanın F5 yapmadan SignalR üzerinden otomatik güncellendiğini izleyin.
3. Pano başlığında "Canlı Senkronize" ve "Gecikme: 0.Xs" rozetlerinin göründüğünü doğrulayın.
4. İstemci üzerinden yetkisiz bir bölüm grubuna (`JoinDepartmentGroup`) katılma denemesi yapıldığında sunucunun `HubException` ile isteği reddettiğini (TM-13 koruması) doğrulayın.

Herhangi bir hassas veri sızıntısı, yetkisiz CSV export'u, formula injection açığı, SignalR bölüm atlama zafiyeti veya finansal veri sızması genel kararı `FAIL` yapar.
