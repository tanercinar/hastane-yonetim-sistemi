# Faz 3 İlk Ürün Kapısı Doğrulama Raporu ve Manuel Test Kılavuzu (F03-KAPI)

Bu belge, Hastane Yönetim Sistemi (HMS) projesinde **Faz 3 — Hasta Kayıt, Poliklinik ve Randevu Yönetimi** kapsamındaki tüm geliştirilen yeteneklerin mimari, güvenlik, eşzamanlılık ve kullanıcı arayüzü düzeyinde doğrulanmasını; otomatik kapı testlerini ve adım adım **manuel test talimatlarını** içerir.

---

## 1. Faz 3 Kapsamında Tamamlanan Görev Özeti

| Görev Kimliği | Başlık | Tamamlanan Yetenekler |
|---|---|---|
| **F03-G01** | Hasta ana kaydı ve demografi | `patients.patient_records` modeli, `MRN-YYYY-XXXXXX` sentetik numara üretimi, TC Kimlik/telefon/adres maskeleme (`PatientMaskingHelper`), mükerrer hasta tespiti (`DuplicateCheckQuery`), optimistik kilitleme (`IHasConcurrencyVersion`). |
| **F03-G02** | Hasta arama ve kayıt arayüzü | `PatientSearch.razor` ekranı (`/staff/patients`), isim ve kimlik ile arama, minimum 2 karakter sınırı, sunucu taraflı sayfalama, mükerrer aday uyarı kartı, `PatientSearch` denetim izi. |
| **F03-G03** | Doktor uygunluk takvimi | `DoctorSchedule`, `ScheduleBreak`, `DoctorLeaveBlock`, `AppointmentSlot` modelleri, slot üretim motoru (`SlotGenerationEngine`), çakışma önleme (`ux_appointment_slots_doctor_start`), DST/saat dilimi uyumluluğu. |
| **F03-G04** | Randevu yaşam döngüsü | `Appointment` aggregate root, `Confirmed -> CheckedIn -> Completed` ve `Cancelled/NoShow` durum geçişleri, atomik slot rezervasyonu ve `Task.WhenAll` çift rezervasyon yarışı koruması. |
| **F03-G05** | Hasta randevu arama ve alma | `BookAppointmentPage.razor` (`/patient/appointments/book`), poliklinik/doktor/tarih slot seçimi, `MyAppointments.razor` (`/patient/appointments`), randevu listeleme, iptal gerekçesi modalı, IDOR negatif yetki koruması. |
| **F03-G06** | Kayıt/check-in ve sıra | `DailyAppointmentQueue.razor` (`/staff/queue`), kayıt personeli günlük liste, sıralı ve çakışmasız `QueueNumber` bilet üretimi, check-in ve `NoShow` işaretleme. |
| **F03-G07** | Bildirim altyapısı | `notifications.notification_outbox_events` outbox deseni, idempotent işleyici (`ProcessOutboxAsync`), mock e-posta/SMS teslimatı, C3/C4 hassas klinik veri sızdırmama garantisi, `NotificationList.razor` (`/notifications`). |
| **F03-G08** | Gerçek zamanlı randevu ekranı | `HospitalHub` (`/hubs/hospital`) SignalR hub'ı, `IHospitalRealtimeNotifier` olay dağıtıcısı, `SlotRealtimeUpdate`/`QueueRealtimeUpdate` anlık yayınları, otomatik yeniden bağlanma ve sunucudan yeniden senkronizasyon. |

---

## 2. Otomatik Kapı Testleri

`Phase3ProductGateIntegrationTests.cs` gerçek PostgreSQL üzerinde şu senaryoları doğrular:
1. **Hasta Kaydı:** Kayıt personeli yeni bir sentetik hasta (`DEMO-*`) kaydeder, benzersiz `MRN` numarası üretilir.
2. **Doktor Slotları:** Poliklinik hekiminin takviminden uygun slotlar listelenir.
3. **Gerçek Zamanlı SignalR:** Kayıt personeli SignalR Hub'ına bağlanır ve olay dinleyicilerini kurar.
4. **Çift Rezervasyon Yarış Koşulu (Race Condition Gate):** İki farklı hasta (`DEMO-patient@hospital.invalid` ve `DEMO-patient2@hospital.invalid`) aynı slotu aynı anda almak için yarışır (`Task.WhenAll`).
   - Kazanan hasta: `201 Created` yanıtı alır, randevu `Confirmed` olur.
   - Kaybeden hasta: `409 Conflict` yanıtı alır ("Seçilen randevu aralığı artık uygun değildir.").
5. **Kayıt ve Check-In (Sıra Numarası):** Kayıt personeli hastayı check-in yapar; ardışık `QueueNumber: #1` bilet üretilir, randevu `CheckedIn` durumuna geçer.
6. **Outbox ve Bildirim:** Randevu ve check-in olayları outbox üzerinden işlenir; hastaya mock bildirim üretilir, hasta kendi bildirimini okundu olarak işaretler.
7. **IDOR ve Güvenlik Sınırı:** Diğer hasta, kazanan hastanın randevusunu iptal etmeye veya listelemeye çalıştığında `403 Forbidden` / `404 Not Found` ile engellenir.

`Phase3ProductGateEndToEndTests.cs` ise gerçek Kestrel ve Chromium üzerinde iki ayrı hasta cookie oturumunu aynı slota eşzamanlı gönderir. Tam olarak bir `201 Created` ve bir `409 Conflict` alındığını, kaybeden kullanıcının anlaşılır uyarıyı gördüğünü, kazanan randevunun kayıt personelince check-in yapılabildiğini ve `390x844` görünümde sayfa düzeyinde yatay taşma olmadığını doğrular.

Son bağımsız inceleme sonucu:

- Release build: `0` uyarı, `0` hata;
- unit `47/47`, component `22/22`, architecture `13/13`, PostgreSQL integration `54/54`, Playwright E2E `3/3`;
- toplam `139/139`, atlanan test yok;
- `dotnet format --verify-no-changes`, bağımlılık zafiyet kapısı ve `tools/validate-phase0.ps1`: `PASS`.

---

## 3. Kullanıcı İçin Adım Adım Manuel Test Talimatları

Sistemi tarayıcıda canlı olarak test etmek için aşağıdaki adımları sırayla izleyebilirsiniz:

### Adım 0: Ortamın Başlatılması

1. **Docker Desktop'ı açın** ve hazır olduğunu doğrulayın.
2. Repo kökünde yerel servisleri, secret yapılandırmasını ve tüm migration'ları hazırlayın:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\configure-local-user-secrets.ps1
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
   ```

3. Geliştirme sertifikasını bu Windows hesabında bir kez güvenilir yapın ve işletim sistemi onay penceresini kabul edin:

   ```powershell
   dotnet dev-certs https --trust
   ```

4. HTTPS launch profiliyle uygulamayı başlatın:

   ```powershell
   dotnet run --project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj --launch-profile https
   ```

5. Tarayıcınızda `https://localhost:7111` adresine gidin. `http://localhost:5111` güvenli adrese yönlenir; giriş ve antiforgery cookie'leri için HTTPS zorunludur.

---

### Adım 1: Hasta Kayıt ve Arama Testi (Kayıt Personeli Olarak)
1. **Giriş Yap:**
   - E-posta: `DEMO-receptionist@hospital.invalid`
   - Şifre: `DEMO-Recep-Pass!1`
2. **Hasta Kayıt Ekranına Git:** Sol menüden **"Hasta Kayıt"** (`/staff/patients`) sayfasına tıklayın.
3. **Arama Doğrulaması:** Arama kutusuna `Ayşe` yazın; filtrelenen demo hastanın listelendiğini ve TC Kimlik numarasının maskelendiğini (`***...`) görün.
4. **Yeni Hasta Kaydı:**
   - "Yeni Hasta Kaydı" formunda bilgileri doldurun (Ad: `Mehmet`, Soyad: `Demir`, Doğum Tarihi: `15.03.1990`, Cinsiyet: `Erkek`, E-posta: `DEMO-mehmet@hospital.invalid`, Telefon: `+905559998877`).
   - "Kaydet" butonuna tıklayın.
   - Yeni hastanın başarıyla kaydedildiğini ve `MRN-YYYY-XXXXXX` formatında dosya numarası aldığını doğrulayın.

---

### Adım 2: Randevu Arama ve Alma Testi (Hasta Olarak)
1. **Çıkış yapın** ve **Hasta hesabı** ile giriş yapın:
   - E-posta: `DEMO-patient@hospital.invalid`
   - Şifre: `DEMO-Patient-Pass!1`
2. **Randevu Alma Sayfasına Git:** Sol menüden **"Randevu Al"** (`/patient/appointments/book`) sayfasına tıklayın.
3. **Filtreleri Seç:**
   - Poliklinik: `Kardiyoloji Polikliniği`
   - Hekim: `Prof. Dr. Ayşe Yılmaz (Kardiyoloji)`
   - Tarih: Uygun saat gösterilen bir iş günü seçin.
4. **Slot Seçimi ve Onay:**
   - Listelenen uygun saat slotlarından birine (örn: `09:00 - 09:20`) tıklayın.
   - Şikayet / Ziyaret Nedeni kutusuna `Rutin kalp kontrolü` yazın.
   - **"Randevuyu Onayla"** butonuna basın.
   - "Randevunuz başarıyla oluşturuldu" onay mesajını görün.
5. **Randevularım Ekranı:** Sol menüden **"Randevularım"** (`/patient/appointments`) sayfasına gidin; yeni randevunuzun `Onaylandı` rozeti ile listelendiğini doğrulayın.

---

### Adım 3: Eşzamanlı Çift Rezervasyon Testi (Yarış Koşulu Kontrolü)
1. Normal ve gizli pencerede hasta hesabıyla oturum açın; her iki pencerede aynı uygun slotu seçin.
2. İki penceredeki **"Randevuyu Onayla"** düğmesine art arda hızlıca basın.
3. Yalnızca bir işlemin başarılı olduğunu; diğerinde slotun başka kullanıcı tarafından alınmış olabileceğini açıklayan uyarının görünür kaldığını doğrulayın. İki ayrı hasta kimliğinin gerçek eşzamanlı yarışı otomatik Playwright ve integration kapılarında ayrıca doğrulanır.

---

### Adım 4: Kayıt Masası Check-In ve Sıra Numarası (Kayıt Personeli Olarak)
1. Kayıt personeli (`DEMO-receptionist@hospital.invalid`) hesabıyla giriş yapın.
2. Sol menüden **"Günlük Randevu ve Sıra Yönetimi"** (`/staff/queue`) sayfasına gidin.
3. Günün randevu listesinde hastayı bulun.
4. Durumu `Onaylandı` olan randevunun yanındaki **"Giriş Yap"** butonuna tıklayın.
5. Randevunun durumunun anında `Giriş Yapıldı` rozetine dönüştüğünü ve hastaya **`#1`**, **`#2`** gibi sıralı bir sıra numarası atandığını doğrulayın.

---

### Adım 5: Gerçek Zamanlı Güncelleme ve Bildirim Testi (SignalR & Bildirimler)
1. **SignalR Canlı Ekran:** Kayıt personeli ekranı açıkken başka bir tarayıcı penceresinde yeni bir randevu alındığında veya check-in yapıldığında, sayfa yenilenmeden listenin anlık olarak güncellendiğini izleyin.
2. **Hasta Bildirimleri:**
   - Tekrar hasta (`DEMO-patient@hospital.invalid`) hesabına geçin.
   - Sol menüdeki **"Bildirimlerim"** (`/notifications`) sayfasına gidin.
   - "Randevunuz Onaylandı" ve "Randevu Girişiniz Yapıldı (Muayene Sıra Numaranız: #1)" bildirimlerinin listelendiğini görün.
   - "Okundu İşaretle" butonuna tıklayarak bildirimin okundu durumuna geçtiğini ve okunmamış sayacının düştüğünü doğrulayın.
