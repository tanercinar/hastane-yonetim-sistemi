# Hasta Arama ve Kayıt Personeli Ekranı (F03-G02)

Bu belge, Hastane Yönetim Sistemi projesinde Faz 3 kapsamındaki **Hasta Arama ve Kayıt Personeli Ekranı (Patient Search and Registration UI)** bileşenini, gizlilik sınırlarını, veri sızıntısı koruma önlemlerini ve arayüz durumlarını açıklar.

## 1. Mimari Prensipler ve Güvenlik Sınırları

- **Yetki ve Erişim Denetimi:** `/staff/patients` ekranı yalnızca `HospitalPermissions.Patient.Search`, `HospitalPermissions.Patient.DemographicsView` ve `HospitalPermissions.Patient.DemographicsCreate` izinlerine sahip roller (Kayıt Personeli, Hekim, Hemşire) tarafından kullanılabilir. Yetkisiz girişlerde `ForbiddenState` gösterilir.
- **Toplu Veri Sızdırma Koruması:**
  - Arama terimlerinde tek karakterli geniş sorgulamalar engellenir (minimum 2 karakter zorunluluğu).
  - Sonuçlar varsayılan olarak sayfalanır (`page`, `pageSize: 50`).
  - Hassas alanlar (TC Kimlik No, Telefon) arama sonuç listesinde maskeli formatta (`99*******01`, `+905*******01`) sunulur.
- **Mükerrer Kayıt Uyarısı (`Duplicate Check`):**
  - Yeni hasta kayıt formu doldurulurken veya "Mükerrer Kontrolü Yap" butonuna tıklandığında `/api/v1/patients/duplicate-check` uç noktası çağrılır.
  - Aynı sentetik TC veya Ad+Soyad+Doğum Tarihi kombinasyonuna sahip var olan hastalar sarı uyarı kutusunda MRN ve eşleşme sebebiyle birlikte gösterilir.
- **Eşzamanlılık Koruması (`Optimistic Concurrency`):** Hasta güncelleme işlemlerinde `Version` başlığı (`If-Match`) gönderilerek eşzamanlı çakışmalar (`409 Conflict`) engellenir.
- **Denetim İzi:** Yapılan tüm aramalar ve kayıt/güncelleme işlemleri `AuditAction.PatientSearch` ve `AuditAction.PatientRegister` ile loglanır.

## 2. Arayüz Bileşenleri ve Durumlar

- **Arama Formu (`PatientSearch.razor`):** Metin tabanlı filtreleme, hızlı temizleme butonu ve validasyon geri bildirimi.
- **Sonuç Tablosu:** MRN, Ad Soyad, Doğum Tarihi, Cinsiyet, Maskeli TC, Maskeli Telefon, Aktif/Pasif rozeti ve "Detay / Düzenle" eylemi.
- **Yeni Hasta Kaydı Modalı:** Form doğrulama, mükerrer kontrolü paneli, adres ve acil durum iletişim kişisi alanları.
- **Hasta Detay ve Güncelleme Modalı:** Mevcut kaydı görüntüleme ve demografik bilgileri güncelleme.
- **Durum Yönetimi:** `LoadingState`, `ForbiddenState`, `EmptyState`, `ErrorState` ve başarı bildirim mesajları.

## 3. Test ve Doğrulama

- **Bileşen Testleri (`PatientManagementComponentTests`):**
  - Tablo ve sonuç alanlarının render edilmesi.
  - Erişim reddedildiğinde `ForbiddenState` gösterimi.
  - "Yeni Hasta Kaydı" modalının açılması, mükerrer kontrol butonu ve form alanları.
- **Entegrasyon Testleri (`PatientSearchAndRegistrationIntegrationTests`):**
  - Canlı PostgreSQL ve API üzerinde kayıt personeli oturumu ile sayfalama ve filtreleme.
  - Maskeli kimlik ve telefon alanlarının doğrulanması.
  - Mükerrer kontrolü API çağrısı.
  - `audit_privacy.audit_logs` tablosunda arama denetim izinin tespiti.
