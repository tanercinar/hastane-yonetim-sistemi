# Faz 4 Manuel Test Rehberi

Bu rehber Faz 4 klinik kayıt akışını sentetik `DEMO` verilerle elle kabul etmek içindir. Faz 4 için henüz özel bir web çalışma alanı bulunmadığından manuel klinik kontroller API sözleşmesi üzerinden yapılır; mevcut Playwright testleri Faz 1–3 ekranlarını kapsar.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
REST istemcisi:

F04-M01 Otomatik ön kapı                 : PASS / FAIL
F04-M02 Hekim karşılaşma akışı           : PASS / FAIL
F04-M03 Hemşire vital ve kapsam kontrolü : PASS / FAIL
F04-M04 Not/tanı bütünlüğü               : PASS / FAIL
F04-M05 Eşzamanlılık ve IDOR             : PASS / FAIL
F04-M06 Klinik ek güvenliği              : PASS / FAIL
F04-M07 Konsültasyon ve bildirim         : PASS / FAIL
F04-M08 Tamamlama ve MFA yeniden açma    : PASS / FAIL
F04-M09 Hasta zaman çizelgesi            : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Bir kritik güvenlik/bütünlük bulgusu veya beklenmeyen `500` genel kararı `FAIL` yapar. Yanıtlardaki klinik içeriği gerçek kişi bilgisiyle doldurmayın ve token/cookie değerlerini rapora kopyalamayın.

## 2. F04-M01 — Otomatik ön kapı

Repository kökünde Docker Desktop çalışırken:

```powershell
dotnet build .\HospitalManagement.slnx -c Release --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj -c Release --no-build
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj -c Release --no-build
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj -c Release --no-build
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj -c Release --no-build --filter "Roadmap~F04"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj -c Release --no-build
```

Beklenen sonuç: build 0 uyarı/0 hata; test paketleri sırasıyla 125/125, 22/22, 13/13, Faz 4 entegrasyonu 21/21 ve mevcut E2E 3/3 geçer.

## 3. Uygulamayı hazırlama

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` ve `https://localhost:7111/openapi/v1.json` HTTP 200 dönmelidir. REST istemcisinde cookie saklamayı açın. Her durum değiştiren istekten önce `GET /api/v1/identity/antiforgery` çağırın ve dönen `token` değerini `X-HMS-CSRF` başlığına koyun.

Kanonik sentetik kimlikler:

| Kaynak | Kimlik |
| :--- | :--- |
| Hasta person | `00000000-0000-0000-0000-000000000109` |
| Hekim person | `00000000-0000-0000-0000-000000000102` |
| Hemşire person | `00000000-0000-0000-0000-000000000103` |
| Başhekim person | `00000000-0000-0000-0000-000000000108` |
| Kardiyoloji bölümü | `30000000-0000-0000-0000-000000000003` |

DEMO giriş bilgileri `README.md` içindeki hesap tablosundadır. Her rol için ayrı cookie oturumu kullanın.

## 4. Klinik akışlar

### F04-M02 — Hekim karşılaşma akışı

1. Hekim hesabıyla giriş yapın.
2. `POST /api/v1/clinical-records/encounters` ile DEMO hasta, Kardiyoloji bölümü ve hekim için `Outpatient` karşılaşma oluşturun; `201 Created` ve `Version=1` bekleyin.
3. Dönen kimlik ve sürümle `/encounters/{id}/start` çağırın; `200 OK`, `InProgress` ve artmış sürüm bekleyin.
4. Aynı randevu kimliğiyle ikinci aktif karşılaşma oluşturmayı deneyin; `409 Conflict` bekleyin.

### F04-M03 — Hemşire vital ve kapsam kontrolü

1. Hekim olarak hemşireyi güncel `ExpectedVersion` ile katılımcı ekleyin.
2. Hemşire hesabıyla `/vital-signs/panel` üzerinden fizyolojik aralıktaki DEMO ölçümleri girin; `201 Created` ve hesaplanan BMI bekleyin.
3. Karşılaşmaya katılımcı olmayan rastgele bir pratisyen/karşılaşma kimliğiyle aynı işlemi deneyin; `403 Forbidden` bekleyin.
4. Mantıksız birim/değer gönderin; alan bazlı `400` bekleyin.

### F04-M04 — Not ve tanı bütünlüğü

1. Hekim taslak SOAP notu oluşturup güncellesin ve güncel sürümle imzalasın.
2. İmzalı notu normal güncelleme uç noktasından değiştirmeyi deneyin; `409 Conflict` bekleyin.
3. Gerekçeli addendum ekleyin; özgün notun içeriğinin değişmediğini doğrulayın.
4. `Final` tanı ekleyin ve normal güncelleme ile değiştirmeyi deneyin; `409 Conflict` bekleyin.

### F04-M05 — Eşzamanlılık ve IDOR

1. Aynı taslak notun aynı `ExpectedVersion` değerini taşıyan iki farklı güncellemesini peş peşe gönderin.
2. İlk istek `200`, ikinci istek `409` olmalı; ilk başarılı değer korunmalıdır.
3. Sistem yöneticisi hesabıyla karşılaşma/not/tanı GET isteklerini deneyin; rol adına rağmen `403` bekleyin.
4. Hasta hesabıyla imzasız taslak notu doğrudan okumayı deneyin; `403` bekleyin.

### F04-M06 — Klinik ek güvenliği

1. Geçerli küçük PDF yükleyin; `201 Created` bekleyin ve indirmeyi yalnız ilişkili kullanıcıyla doğrulayın.
2. `.pdf` adlı fakat PNG/EXE içeriği taşıyan dosya gönderin; `400` bekleyin.
3. `application/dicom` bildirip 128. byte sonrasında `DICM` işareti olmayan dosya gönderin; `400` bekleyin.
4. EICAR test dizesi içeren sentetik dosya gönderin; `MOCK` tarayıcının isteği reddettiğini doğrulayın. Gerçek zararlı dosya kullanmayın.

### F04-M07 — Konsültasyon ve bildirim

1. Hekim Kardiyoloji bölümüne/DEMO başhekime konsültasyon oluştursun.
2. İlgisiz kullanıcı kabul/yanıt denediğinde `403`; hedef kullanıcı kabul ve tamamlama yaptığında `200` bekleyin.
3. Bildirim kayıtlarında istem, kabul ve tamamlama olaylarını doğrulayın; klinik soru/rapor metni veya hasta adı bildirim özetinde bulunmamalıdır.

### F04-M08 — Tamamlama ve MFA yeniden açma

1. Boş karşılaşmayı ve taslak not içeren karşılaşmayı tamamlamayı deneyin; ikisi de `400` olmalıdır.
2. İmzalı not veya tanı bulunan karşılaşmayı güncel sürümle tamamlayın; `Completed` bekleyin.
3. Normal hekim ve sistem yöneticisiyle `/reopen` çağrısı yapın; `403` bekleyin.
4. DEMO başhekim hesabında MFA'yı etkinleştirin, MFA ile yeniden giriş yapın ve beş dakika içinde gerekçe + güncel `ExpectedVersion` ile `/reopen` çağırın; `200` ve `InProgress` bekleyin.
5. Aynı MFA oturumu beş dakikayı geçince veya gerekçe boşken tekrar deneyin; sırasıyla `403` ve `400` bekleyin.

### F04-M09 — Hasta zaman çizelgesi

1. Hasta hesabıyla kendi `/timeline/by-patient/{patientId}` kaynağını açın; yalnız kendi verisi ve imzalı/görünür olaylar dönmelidir.
2. Başka hasta kimliği, `IncludeEnteredInError=true` ve taslak nota erişim denemelerini yapın; veri sızıntısı olmamalı, uygun istekler `403` dönmelidir.
3. Özetlerde SOAP içeriği, konsültasyon sorusu/raporu, özgün dosya adı ve hata gerekçesi gibi serbest klinik metinlerin bulunmadığını doğrulayın.

## 5. Kapı kararı

Tüm adımlar `PASS` ise sonuç özetini `ROADMAP.md` ilerleme günlüğüne ekleyip `F04-KAPI` kutusunu işaretleyin. Bir adım başarısızsa kutuyu açık bırakın; HTTP durumu, güvenli/redakte edilmiş yanıt, yeniden üretme adımları ve ilgili kayıt kimliklerini ekleyin. Token, cookie, parola dışındaki gerçekçi olmayan DEMO klinik metni dahi log çıktısına gereksiz yere kopyalamayın.
