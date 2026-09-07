# Veri Envanteri

## Kullanım

Envanter, tasarım düzeyinde veri kategorilerini ve koruma beklentisini tanımlar. Yeni alan/modül eklendiğinde güncellenir. `TBD-LEGAL` gerçek kullanımdan önce hukuk/sağlık kayıt uzmanı kararı gerektiğini belirtir.

| Veri kategorisi | Örnek alanlar | Sınıf | Amaç | İlgili roller/alıcı | Sistem kaynağı | Saklama anahtarı |
|---|---|---|---|---|---|---|
| Kullanıcı hesabı | UserId, kullanıcı adı, e-posta, durum | C2 | Kimlik doğrulama ve hesap yaşam döngüsü | Kendi kullanıcı, ADM | Identity | RET-IDENTITY |
| Kimlik doğrulama sırrı | Parola hash, MFA secret, recovery code, tek kullanımlık kod hash/metadata | C4 | Güvenli giriş ve hesap kurtarma | Sistem; çok sınırlı ADM işlemi | Identity/secret store | RET-AUTH-SECRET |
| Personel profili | Demo ad, sicil kodu, meslek, uzmanlık | C2 | İş atama ve yetkilendirme | Personel, ADM, görev kapsamındaki kullanıcı | Organization | RET-STAFF |
| Organizasyon ataması | Şube, bölüm, görev, başlangıç/son | C1/C2 | Kaynak kapsamı | Personel, ADM | Organization | RET-STAFF |
| Hasta ana kaydı | Hasta no, demo ad, doğum tarihi, cinsiyet | C2 | Hastayı ayırt etme ve hizmet sunumu | PAT, REG, bakım ekibi | Patients | RET-PATIENT-MASTER |
| İletişim/adres | Demo telefon, e-posta, adres, acil kişi | C2 DIRECT | İletişim/evde sağlık planı | PAT, REG; ASSIGNED evde sağlık | Patients | RET-CONTACT |
| İletişim tercihleri | Kanal, izin/tercih, zaman | C2 | Bildirim tercihi | PAT, REG, sistem | Patients/Notifications | RET-CONTACT |
| Randevu | Slot, bölüm, doktor, durum, iptal nedeni | C2/C3 bağlam | Planlama ve check-in | PAT, REG, ilgili ekip | Scheduling | RET-APPOINTMENT |
| Sıra/check-in | Geliş, sıra, durum | C2/C3 bağlam | Günlük operasyon | PAT, REG, ilgili ekip | Scheduling | RET-APPOINTMENT |
| Encounter | Hasta, katılımcı, tür, zaman, durum | C3 | Klinik hizmet kaydı | Bakım ekibi, sınırlı PAT | ClinicalRecords | RET-CLINICAL |
| Vital/observation | Ölçüm, birim, zaman, kaydeden | C3 | Klinik değerlendirme | Bakım ekibi, yayın politikasıyla PAT | ClinicalRecords | RET-CLINICAL |
| Alerji/problem/tanı | Kod, metin, durum, doğrulama | C3 | Hasta güvenliği ve bakım | Bakım ekibi, sınırlı PAT/PHA | ClinicalRecords | RET-CLINICAL |
| Klinik not | Şikâyet, öykü, muayene, plan, imza | C3 | Klinik belge | Bakım ekibi, sınırlı PAT | ClinicalRecords | RET-CLINICAL |
| Klinik ek | Dosya, MIME, boyut, hash, metadata | C3 | Klinik destekleyici belge | Bakım ekibi, sınırlı PAT | Blob/ClinicalRecords | RET-CLINICAL-FILE |
| Konsültasyon | İstek, gerekçe, yanıt, durum | C3 | Uzman değerlendirmesi | İsteyen/yanıtlayan ekip, sınırlı PAT | ClinicalRecords | RET-CLINICAL |
| Reçete | İlaç, doz, yol, sıklık, talimat, imza | C3 | İlaç talimatı | DOC, PHA, NUR, PAT | Pharmacy | RET-CLINICAL |
| Eczane teslimi | Reçete kalemi, lot, miktar, teslim eden | C3 | İlaç teslim ve izlenebilirlik | PHA, sınırlı bakım ekibi/PAT | Pharmacy | RET-DISPENSE |
| Klinik stok | Ürün, lot, son kullanma, miktar | C1; hasta ilişkili hareket C3 | Güvenli stok yönetimi | PHA, ilgili yönetici | Inventory | RET-INVENTORY |
| Klinik istem | Tür, gerekçe, öncelik, isteyen | C3 | Tanı/hizmet talebi | Bakım ve hedef hizmet ekibi | Diagnostics | RET-CLINICAL |
| Numune | Barkod, tür, zaman, hareketler | C3 | Laboratuvar izlenebilirliği | LAB, bakım ekibi | Diagnostics | RET-SPECIMEN |
| Tanısal sonuç/rapor | Değer, birim, bayrak, rapor, sürüm | C3 | Tanı ve tedavi desteği | Bakım ekibi, final ise PAT | Diagnostics | RET-CLINICAL |
| Görüntüleme metadata/dosyası | Study/series/instance, mock görüntü | C3 | Radyoloji simülasyonu | RAD, bakım ekibi, sınırlı PAT | Diagnostics/blob | RET-CLINICAL-FILE |
| Yatış ve yatak hareketi | Kabul, servis, oda, yatak, transfer | C3 | Yataklı bakım | Bakım ekibi, REG minimum, PAT | Inpatient | RET-CLINICAL |
| Bakım planı/eMAR | Problem, hedef, görev, doz uygulama | C3 | Hemşirelik bakımı | NUR, DOC, sınırlı PAT | Inpatient | RET-CLINICAL |
| Acil/triyaj | Geliş, şikâyet, demo seviye, disposition | C3 | Acil operasyon | Acil bakım ekibi | Emergency | RET-CLINICAL |
| Ameliyat/ICU | Plan, ekip, kayıt, saatlik gözlem | C3 | Kritik bakım simülasyonu | Yetkili ekip | SurgeryCriticalCare | RET-CLINICAL |
| Uzmanlık kayıtları | Gebelik, doğum, diş, evde sağlık | C3 | Uzmanlık bakımı | Yetkili ekip, sınırlı PAT | SpecialtyCare | RET-CLINICAL |
| Audit olayı | Aktör ID, hedef ID/tür, eylem, sonuç, gerekçe | C2/C3 SECURITY | Hesap verebilirlik ve olay inceleme | Sınırlı ADM/CHM/güvenlik rolü | AuditPrivacy | RET-AUDIT |
| Teknik log/trace | Correlation ID, endpoint şablonu, süre, hata kodu | C1/C2 | İşletim ve hata ayıklama | Geliştirici/operasyon | Observability | RET-TECH-LOG |
| Bildirim | Template, alıcı ref, kanal, durum | C2; içerik klinikse C3 | İşlem bildirimi | Alıcı ve sistem | Notifications | RET-NOTIFICATION |
| Operasyon projection | Sayım, süre, doluluk | C1; küçük grupta C2/C3 olabilir | Gerçek zamanlı yönetim | Klinik roller/MGR | Reporting | RET-REPORTING |
| Export/geçici dosya | Yetkili sorgu çıktısı | İçeriğin en yüksek sınıfı | Kullanıcı talebi | Talep eden | Reporting/blob | RET-EXPORT |
| Mock entegrasyon mesajı | Sentetik FHIR/HL7/DICOM/MHRS payload | C2/C3 DEMO | Entegrasyon simülasyonu | Yetkili roller/geliştirici | Interoperability | RET-INTEGRATION |
| Backup | Veritabanı/blob kopyası | İçeriğin en yüksek sınıfı | Kurtarma simülasyonu | Sistem/operasyon | Infrastructure | RET-BACKUP |

## Veri akışları

1. Kullanıcı → TLS → Web/API → permission + resource authorization → modül veritabanı/blob.
2. Modül işlemi → audit olayı ve güvenilir uygulama olayı → bildirim/projection.
3. Final klinik içerik → yayın politikası → hasta portalı.
4. İzinli sentetik veri → mock adaptör → yerel mock sunucu; gerçek dış alan adı/credential yoktur.
5. İzinli rapor → süreli export → audit → otomatik temizleme.

## Veri sahipliği ve sorumluluk

Buradaki “veri sahibi”, verinin konusu olan ilgili kişiyi değil, ürün içinde kurallarını yöneten modül sorumluluğunu ifade eder.

| Veri alanı | Domain sahibi | Veri steward'ı/iş sorumlusu | Teknik saklama sorumlusu |
|---|---|---|---|
| Hesap, rol ve permission | IdentityAccess | Sistem yöneticisi | IdentityAccess/Platform |
| Organizasyon ve atama | Organization | Sistem yöneticisi + ilgili bölüm sorumlusu | Organization |
| Hasta ana kaydı/demografi | Patients | Kayıt birimi; hasta kendi portal alanlarında | Patients |
| Randevu/check-in | Scheduling | Kayıt birimi + ilgili klinik | Scheduling |
| Encounter ve klinik kayıt | ClinicalRecords | Bakım ekibi; bölümde başhekim gözetimi | ClinicalRecords |
| Reçete/teslim | Pharmacy | Doktor (reçete) ve eczacı (teslim) | Pharmacy |
| Laboratuvar/radyoloji/sonuç | Diagnostics | İlgili tanısal hizmet birimi | Diagnostics |
| Yatış/bakım/eMAR | Inpatient | Servis bakım ekibi | Inpatient |
| Acil/ameliyat/ICU/uzmanlık | İlgili klinik modül | Yetkili klinik ekip | İlgili modül |
| Audit ve retention kanıtı | AuditPrivacy | Güvenlik/mahremiyet sorumluluğu | AuditPrivacy |
| Teknik log/trace | Platform | Teknik operasyon | Platform/observability |
| Projection/rapor/export | Reporting | Kaynak modül + yetkili rapor sahibi | Reporting |
| Mock entegrasyon mesajı | Interoperability | Kaynak modül | Interoperability |

Domain sahibi şema, sözleşme ve yaşam döngüsünü; steward iş doğruluğu ve erişim amacını; teknik sorumlu saklama, yedekleme ve imha uygulamasını yönetir. Bu sorumluluklar yeni modülde açıkça atanır.

## Veri sahibi talepleri için tasarım notu

Bu eğitim sisteminde gerçek ilgili kişi başvurusu yürütülmez. Buna rağmen hasta profilinde kendi verisini görme/düzeltme talebi simülasyonu, retention işinde silme/anonimleştirme ve yapılan işlemin kanıtı planlanır. Klinik kayıt bütünlüğü ile veri imhası çelişirse gerçek kullanımdan önce `TBD-LEGAL` uzman kararı gerekir.

## Envanter kabul kontrolü

- Her yeni veritabanı alanı bir kategori/sınıfa bağlanır.
- Her API DTO'su yalnız amacı için gereken alanları taşır.
- Her event, log ve export için içerik sınıfı belirlenir.
- C3/C4 verinin log/telemetry/URL'ye girmediği test edilir.
- Retention anahtarı olmayan kalıcı veri eklenmez.
