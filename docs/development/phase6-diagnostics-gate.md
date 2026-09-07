# Faz 6 Tanısal Hizmetler Kapısı Doğrulaması (F06-KAPI)

Bu doküman, **Faz 6 — Tanısal Hizmetler (Laboratuvar, Radyoloji, Patoloji ve Kan Bankası)** fazının kabul kriterlerini, uçtan uca akışlarını, güvenlik ve rol ayrımı testlerini ve kanıtlarını özetler. Kod incelemesi sonrasında otomatik kapı 30 Ağustos 2026 tarihinde yeniden doğrulanmıştır; kullanıcı tarafından uygulanacak manuel kabul tamamlanmadan Faz 6 tamamlanmış sayılmaz.

## 1. Faz 6 kapsamı ve uygulanan hedefler

Faz 6 kapsamında aşağıdaki dikey dilimler, güvenlik kuralları ve kullanıcı arayüzleri uygulanmıştır:

1. **F06-G01 — Ortak klinik istem modeli**: `DiagnosticOrder` ve `DiagnosticOrderItem` ortak istem yaşam döngüsü (`Draft -> Placed -> InProgress -> Completed`).
2. **F06-G02 — Laboratuvar test kataloğu ve istem**: 10 sentetik laboratuvar panel/test tohumlaması (`LabCatalogDataSeeder`), laboratuvar/radyoloji seçebilen hekim istem düzenleyicisi (`DoctorDiagnosticOrderEditor.razor`).
3. **F06-G03 — Numune ve barkod zinciri**: Benzersiz barkod üretimi (`DEMO-SMP-YYYYMMDD-XXXXXX`), numune toplama, pnömatik transfer ve laboratuvar kabul/red akışı (`SpecimenWorklistAndScanner.razor`).
4. **F06-G04 — Laboratuvar sonucu**: Parametre bazlı referans aralığı, teknik onay (`TechnicallyApproved`), yetkili LAB kesinleştirmesi (`FinalApproved`), değişmezlik ve gerekçeli düzeltme zinciri (`LabResultEntryAndApproval.razor`).
5. **F06-G05 — Kritik sonuç bildirimi**: Panik değer otomatik tespiti, çok kademeli eskalasyon, hekim alındı teyidi kanıtı (`CriticalResultWorklist.razor`).
6. **F06-G06 — Radyoloji iş akışı**: XR, CT, MR, US ve MG modalitelerinde 9 sentetik tetkik, istemden iş listesi oluşturma, randevu/çekim, radyolog raporlama ve ek rapor (`Addendum`) akışı (`RadiologyWorklistAndReporting.razor`).
7. **F06-G07 — PACS/DICOM simülasyonu**: Kişiye bağlı HMAC-SHA256 imzalı beş dakikalık geçici erişim belirteçleri, tokenı URL'ye yazmayan yetkili POST önizleme akışı ve tarayıcı içi SVG DICOM görüntüleyici modalı.
8. **F06-G08 — Patoloji dikey dilimi**: Makroskopi, mikroskopi, fiksatif takibi, patolog onayı ve düzeltme zinciri.
9. **F06-G09 — Kan bankası dikey dilimi**: Sentetik stok, deterministik uygunluk matrisi (`BloodCompatibilityMatrix`), uyumsuzluk reddi, rezervasyon, çıkış ve transfüzyon takibi (`BloodBankManagement.razor`).
10. **F06-G10 — Sonuçların klinik ve hasta görünümü**: Hekim klinik zaman çizelgesi (`PatientDiagnosticTimeline.razor`) ve hasta portalı (`MyDiagnosticResults.razor`) ile taslak sızıntısı önleme ve kritik değer ertelemeli yayın politikası.
11. **F06-KAPI — Faz 6 tanısal hizmetler kapısı**: PostgreSQL ve Playwright otomatik doğrulamaları geçti; yalnız `F06_Test.md` manuel kabulü bekliyor.

---

## 2. Doğrulama Özeti

| Test Kategorisi | Test Sayısı | Durum |
| --- | --- | --- |
| Birim Testler (Unit Tests) | 212 | PASS |
| Bileşen Testleri (Component Tests) | 63 | PASS |
| Mimari Testler (Architecture Tests) | 13 | PASS |
| Tüm gerçek PostgreSQL entegrasyon testleri | 101 | PASS |
| F6 gerçek PostgreSQL entegrasyon testleri | 10 | PASS |
| Tüm uçtan uca tarayıcı / Playwright testleri | 6 | PASS |
| F6 uçtan uca tarayıcı / Playwright testleri | 2 | PASS |
| Kod Biçimlendirme (`dotnet format`) | - | PASS (0 değişiklik) |
| Bağlantı ve Dokümantasyon Doğrulaması (`validate-phase0.ps1`) | 91 Markdown | PASS (0 kırık yerel bağlantı) |

---

## 3. Güvenlik, İzolasyon ve Klinik Değişmezlik

- **API Yetkilendirmesi**: Her klinik kaynak API seviyesinde `Permission + Resource Scope + Care Relationship` bileşimiyle korunur; LAB ve RAD iş listeleri yalnız kendi organizasyon kapsamını görür.
- **Rol Ayrımı**: Laboratuvar teknisyeni, radyoloji personeli, hekim/hemşire ve hasta yetkileri ayrılmıştır. Kan ürününü hazırlama/çıkış LAB rolünde, yatak başı transfüzyon kaydı `blood-bank.transfusion.record` iznine sahip hekim/hemşire rolündedir.
- **Hasta IDOR Güvenliği**: Hastalar yalnızca kendi onaylanmış sonuçlarına erişebilir; başkalarının kayıtlarına erişim 403 Forbidden ile engellenir.
- **Taslak İzolasyonu**: `Draft`, `TechnicallyApproved`, `Ordered`, `Scheduled`, `Acquired`, `GrossExamCompleted` durumundaki ara kayıtlar hasta portalına sızdırılmaz.
- **Değişmezlik**: Finalize edilen hiçbir klinik rapor sessizce güncellenmez; zorunlu gerekçeli düzeltme (`Corrected`) veya ek not (`Addendum`) kullanılır.
- **Tekilleştirme ve Yarış Koruması**: Bir istem kalemi için yalnız bir kök laboratuvar sonucu ve bir radyoloji çalışması oluşturulabilir; kritik bildirimler aynı final sonuç için tekilleştirilir.
- **DICOM Erişimi**: Önizleme belirteci kişiye bağlıdır, beş dakika geçerlidir ve URL yerine antiforgery korumalı POST gövdesinde taşınır.
- **Denetim İzi (Audit Logs)**: Tanısal süreçteki her işlem `Diagnostics.*` audit logları ile güvenle kaydedilir.

---

## 4. Kapı durumu ve kalan koşul

1. Docker Desktop motoru doğrulandı (`29.4.1`).
2. `dotnet test tests/HospitalManagement.IntegrationTests/HospitalManagement.IntegrationTests.csproj --no-build --filter "Roadmap~F06"`: 10/10 PASS.
3. `dotnet test tests/HospitalManagement.EndToEndTests/HospitalManagement.EndToEndTests.csproj --no-build --filter "Roadmap~F06"`: 2/2 PASS. Testler laboratuvar ve radyoloji yolculuklarını yalnız tarayıcı UI'sı üzerinden tamamlar; klinik durumu doğrudan veritabanından hazırlamaz.
4. Kalan tek kapı koşulu: [`F06_Test.md`](../../F06_Test.md) içindeki `F06-M01`–`F06-M09` adımlarının kullanıcı tarafından `PASS` olarak kaydedilmesi.
5. Manuel kabul sağlanmadan `F06-KAPI` `[x]` yapılamaz ve aktif görev `F07-G01` olamaz.
