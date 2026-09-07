# Faz 6 Manuel Test Rehberi

Bu rehber Faz 6 laboratuvar, radyoloji, patoloji, kan bankası ve tanısal sonuç akışlarını yalnız sentetik `DEMO` veriyle elle kabul etmek içindir. Gerçek kişi/kurum verisi kullanmayın; cookie, antiforgery belirteci, DICOM önizleme belirteci veya klinik serbest metni ekran görüntüsüne ve log dosyasına kopyalamayın.

## 1. Sonuç kayıt şablonu

```text
Test eden:
Tarih/saat:
.NET SDK:
Docker Desktop:
Tarayıcı:

F06-M01 Otomatik ön kapı                         : PASS / FAIL
F06-M02 Hekim istemi ve kaynak kapsamı           : PASS / FAIL
F06-M03 Numune, laboratuvar sonucu ve düzeltme   : PASS / FAIL
F06-M04 Kritik sonuç ve gerçek zamanlı bildirim  : PASS / FAIL
F06-M05 Radyoloji, rapor ve DICOM önizleme       : PASS / FAIL
F06-M06 Patoloji durum makinesi ve düzeltme      : PASS / FAIL
F06-M07 Kan bankası uygunluk ve stok akışı       : PASS / FAIL
F06-M08 Hasta portalı, zaman çizelgesi ve IDOR   : PASS / FAIL
F06-M09 Mobil, erişilebilirlik ve hata güvenliği : PASS / FAIL

Bulgu ve yeniden üretme adımları:
Genel karar: PASS / FAIL
```

Başka hastaya/bölüme veri sızıntısı, beklenmeyen `500`, taslak/final kayıt bütünlüğünün bozulması, aynı kritik bildirimin iki kez oluşması, tokenın URL veya loga yazılması ya da uyumsuz kan ürününün işleme alınması genel kararı `FAIL` yapar.

## 2. F06-M01 — Otomatik ön kapı

Docker Desktop tamamen açılmış ve `docker info` komutu gecikmeden yanıt veriyor olmalıdır. Repository kökünde:

```powershell
docker info --format "{{.ServerVersion}}"
dotnet build .\HospitalManagement.slnx --no-restore
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ComponentTests\HospitalManagement.ComponentTests.csproj --no-build
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj --no-build
dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj --no-build --filter "Roadmap~F06"
dotnet test .\tests\HospitalManagement.EndToEndTests\HospitalManagement.EndToEndTests.csproj --no-build --filter "Roadmap~F06"
dotnet format .\HospitalManagement.slnx --verify-no-changes --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-phase0.ps1
```

Beklenen: build 0 uyarı/0 hata; tüm testler `Passed`, `Skipped: 0`; format ve belge bağlantı kontrolü başarılı. `DockerUnavailableException`, named-pipe timeout veya erişim reddi test başarısızlığı değil ortam engelidir fakat kapının kapanmasına yine de izin vermez.

## 3. Uygulamayı hazırlama

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
```

`https://localhost:7111/health/ready` HTTP 200 dönmelidir. Rolleri ayrı gizli pencere veya tarayıcı profillerinde açın.

| Rol | E-posta | Parola |
|---|---|---|
| Hekim | `DEMO-doctor@hospital.invalid` | `DEMO-Doc-Pass!1` |
| Laboratuvar | `DEMO-labtech@hospital.invalid` | `DEMO-LabTech-Pass!1` |
| Radyoloji | `DEMO-radtech@hospital.invalid` | `DEMO-RadTech-Pass!1` |
| Hemşire | `DEMO-nurse@hospital.invalid` | `DEMO-Nurse-Pass!1` |
| Hasta | `DEMO-patient@hospital.invalid` | `DEMO-Patient-Pass!1` |
| Sistem yöneticisi | `DEMO-admin@hospital.invalid` | `DEMO-Admin-Pass!1` |

Kanonik kimlikler: hasta `00000000-0000-0000-0000-000000000109`, hekim `00000000-0000-0000-0000-000000000102`, laboratuvar personeli `00000000-0000-0000-0000-000000000105`, radyoloji personeli `00000000-0000-0000-0000-000000000106`, Kardiyoloji `30000000-0000-0000-0000-000000000003`.

F4 akışıyla bu hasta ve hekim için `InProgress` bir DEMO karşılaşma hazırlayın ve `encounterId` değerini kaydedin. Yazma uç noktalarını REST istemcisiyle hazırlarken önce `GET /api/v1/identity/antiforgery` çağrısını yapın; aynı cookie oturumunda dönen değeri `X-HMS-CSRF` başlığına koyun. Sözleşme alanları için `https://localhost:7111/openapi/v1.json` ve `docs/development/` altındaki ilgili Faz 6 belgesini kullanın.

## 4. F06-M02 — Hekim istemi ve kaynak kapsamı

1. Hekimle `/doctor/diagnostic-orders/{encounterId}` adresini açın. `Klinik Tanısal İstem Yönetimi` ve istem paneli görünmelidir.
2. `Tam Kan` arayın, hemogramı sepete ekleyin ve sentetik endikasyon girin. Önce `Taslak Kaydet`, ardından mevcut istem satırındaki `Gönder` işlemini kullanın. Durum `İletildi (Placed)` olmalıdır.
3. İstem türünü `Radyoloji` yapıp bir tetkik arayın; laboratuvar sepetinin temizlendiğini ve radyoloji kaleminin doğru türle kaydedildiğini doğrulayın.
4. Kapanmış veya rastgele encounter kimliğiyle aynı rotayı açın; istem oluşturulamamalı ve varlık ayrıntısı sızmamalıdır.
5. Hemşire ve sistem yöneticisi oturumuyla aynı `POST /api/v1/diagnostics/orders` isteğini deneyin; `403` bekleyin.
6. Hekim başka hekimin bakım ilişkisi bulunmayan karşılaşmasına erişmeye çalıştığında `403` veya güvenli `404` dönmelidir.

## 5. F06-M03 — Numune, laboratuvar sonucu ve düzeltme

1. Laboratuvar hesabıyla `/laboratory/specimens` ekranını açın. `İletildi` durumundaki istemi `Numune Al ve Barkodla` ile başlatın; barkod sunucu tarafından `DEMO-SMP-*` biçiminde üretilmelidir. REST istemcisiyle yanlış `PatientId` kullanıldığında ayrıca `400` doğrulayın.
2. Sırayla `Taşımaya Ver` ve `Laboratuvara Kabul Et` işlemlerini yapın; her tıklamadan sonra ekrandaki durumun sırasıyla `Taşımada` ve `Kabul Edildi` olmasını bekleyin. Devir-teslim zamanları ve LAB aktörü görünmelidir.
3. Aynı barkodu tekrar oluşturmaya veya geçersiz durumdan transfer yapmaya çalışın; işlem reddedilmeli, zincir değişmemelidir.
4. LAB oturumunda `/laboratory/results` ekranını açın. Kabul edilmiş numune `Sonuç Taslağı Bekleyen İstem Kalemleri` bölümünde görünmeli; `Sonuç Taslağı Oluştur` ile taslağı UI üzerinden başlatın.
5. Taslakta `Klinik Onay` butonu görünmemeli; önce değerleri kaydedip `Teknik Onay` verin. Ardından `Klinik Onay (Kesinleştir)` görünmeli ve işlem başarılı olmalıdır.
6. Final sonucu doğrudan güncelleme reddedilmelidir. `Düzeltme Yap` ile zorunlu sentetik gerekçe girin; yeni kayıt eski sonuca bağlı olmalı, eski final kayıt değişmemelidir.
7. Hemşire numune/sonuç uç noktalarında, radyoloji personeli laboratuvar iş listesinde `403` almalıdır.

## 6. F06-M04 — Kritik sonuç ve gerçek zamanlı bildirim

1. Hekim hesabında `/diagnostics/critical-notifications` sayfasını açık bırakın; ikinci pencerede LAB hesabını kullanın.
2. LAB ile örneğin `GLU = 520 mg/dL` gibi katalog tarafından kritik işaretlenen sentetik bir taslak sonuç oluşturun.
3. Hekim sayfasında yenilemeye basmadan bildirim görünmelidir. Aynı sonucun tekrar işlenmesi ikinci bildirim üretmemelidir.
4. Önce zorunlu gerekçeyle eskale edin, sonra alındı teyidi verin. Durum, seviye, zaman ve aktör kanıtı değişmelidir.
5. Hasta, sistem yöneticisi ve bakım ilişkisi olmayan hekim bildirim listesini/ayrıntısını görememeli ve teyit verememelidir.
6. Hasta portalında teyit edilmemiş kritik sonucun ham parametresi yerine `Hekim Değerlendirmesi Bekleniyor` politikası uygulanmalıdır.

## 7. F06-M05 — Radyoloji, rapor ve DICOM önizleme

1. Hekim oturumuyla `Radyoloji` tipinde sentetik istem oluşturup kesinleştirin. RAD oturumunda `/radiology/worklist` sayfasını açın; bekleyen istemi `İş Listesine Al` ile UI üzerinden çalışma kaydına dönüştürün.
2. `/radiology/worklist` ekranında `Ordered` durumunda yalnız randevu işlemi bulunmalı; çekimi doğrudan tamamlama denemesi API'de reddedilmelidir.
3. Randevuyu kaydedin, ardından çekimi tamamlayın. Durum sırasıyla `Scheduled` ve `Acquired` olmalıdır.
4. DICOM görüntüleyiciyi açın. Görselde `MOCK DICOM PREVIEW` filigranı olmalı; tarayıcı Network panelinde önizleme `POST /api/v1/diagnostics/radiology/dicom-preview` gövdesiyle gitmeli ve URL'de `token=` bulunmamalıdır.
5. RAD oturumunda üretilen tokenı hasta/başka personel oturumunda tekrar kullanma `403`, tahrif edilmiş token `400`, beş dakikadan eski token `403` dönmelidir.
6. Taslak rapor kaydedin, final raporu kesinleştirin ve ek rapor ekleyin. Final metin sessizce değişmemeli; ek rapor ayrı bütünlük kaydı olmalıdır.
7. Hasta yalnız kendi final raporunu ve görüntüsünü görebilmeli; `Ordered`, `Scheduled` ve `Acquired` kayıtlarını görememelidir.

## 8. F06-M06 — Patoloji durum makinesi ve düzeltme

1. Hekimle `Pathology` istemi oluşturun; LAB oturumunda `/api/v1/diagnostics/pathology/cases/ensure` ile vaka hazırlayın ve `/diagnostics/pathology` açın.
2. Sırayla materyal kabulü, makroskopi ve mikroskopi kaydı yapın. Bir adımı atlayıp sonraki işleme geçme API'de reddedilmelidir.
3. Mikroskopi tamamlanmadan finalizasyon reddedilmelidir. Sonra taslak/final rapor akışını tamamlayın.
4. Final rapora doğrudan güncelleme yerine zorunlu gerekçeli düzeltme oluşturun; önceki vaka bağlantısı ve audit aktörü korunmalıdır.
5. Hasta yalnız kendi final/düzeltilmiş raporunu görmeli; ara durumlar sızmamalıdır.

## 9. F06-M07 — Kan bankası uygunluk ve stok akışı

1. Hekimle `BloodBank` istemi oluşturun; LAB oturumunda `/diagnostics/blood-bank` açın.
2. Deterministik uyumsuz donor/recipient grubu seçin. Crossmatch reddedilmeli, ünite rezerve/çıkış/transfüze edilmemelidir.
3. Uyumlu sentetik eşleşmede LAB rolü rezervasyon ve kliniğe çıkışı tamamlamalıdır. Çıkışı yapılmış ünitenin transfüzyon kaydını aynı oturumun antiforgery belirteciyle hekim veya hemşire rolünden `POST /api/v1/diagnostics/blood-bank/units/{id}/transfuse` çağrısıyla oluşturun; durum `Transfused` olmalı ve stok negatif olmamalıdır.
4. Aynı üniteye iki eşzamanlı çıkış isteği gönderin; yalnız biri başarılı olmalı, diğeri `409` almalıdır.
5. Radyoloji ve sistem yöneticisi kan bankası iş listesi/işlem uç noktalarında `403` almalıdır. Hemşire iş listesi, crossmatch ve çıkış işlemlerinde `403` almalı; yalnız `blood-bank.transfusion.record` kapsamındaki transfüzyon kaydını yapabilmelidir.

## 10. F06-M08 — Hasta portalı, zaman çizelgesi ve IDOR

1. Hasta hesabıyla `/patient/diagnostic-results` açın. Yalnız kendi final laboratuvar, radyoloji ve patoloji sonuçları görünmelidir.
2. Başka hastanın GUID'i ile final sonuç, radyoloji çalışma, patoloji vaka ve zaman çizelgesi uç noktalarını deneyin; `403` veya güvenli `404` bekleyin.
3. Hekimle `/doctor/patient/00000000-0000-0000-0000-000000000109/diagnostics` açın. Bakım ilişkili laboratuvar/radyoloji/patoloji kayıtları zaman sıralı görünmelidir.
4. Hasta, LAB, RAD ve sistem yöneticisi doktor zaman çizelgesi uç noktasına doğrudan erişememelidir.
5. Audit kayıtlarında eylem, hedef kaynak, kullanıcı ve kişi aktörü bulunmalı; klinik not, düzeltme gerekçesi, token veya secret bulunmamalıdır.

## 11. F06-M09 — Mobil, erişilebilirlik ve hata güvenliği

1. Laboratuvar, radyoloji ve hasta sonuç sayfalarını `390x844` viewport'ta açın; yatay taşma ve erişilemeyen işlem olmamalıdır.
2. Ana akışları yalnız klavyeyle deneyin. Odak sırası anlaşılır, modal odağı kapalı alanda, buton/alan adları ekran okuyucu için anlamlı olmalıdır.
3. API'yi geçici durdurup sayfaları yenileyin; loading sonsuza kadar kalmamalı, ham exception/stack trace göstermeyen anlaşılır hata durumu oluşmalıdır.
4. Tarayıcı konsolu, Network URL'leri ve sunucu loglarında klinik serbest metin, cookie, antiforgery/DICOM tokenı veya secret bulunmamalıdır.
5. LAB/RAD iş listelerinde loading, empty, error ve forbidden durumlarını ayrı ayrı doğrulayın.

## 12. Kapı kararı

Tüm otomatik kontroller ve `F06-M01`–`F06-M09` adımları `PASS` ise sonucu `ROADMAP.md` ilerleme günlüğüne ekleyip `F06-KAPI` kutusunu işaretleyin ve aktif görevi `F07-G01` yapın. Bir adım başarısızsa `F06-KAPI` açık kalır; güvenli yeniden üretme adımlarını kaydedin ve düzeltmeden sonra ilgili otomatik regresyon testini ekleyin.
