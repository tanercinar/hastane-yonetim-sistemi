# Tanısal Sonuçların Klinik ve Hasta Görünümü (F06-G10)

Bu doküman, Tanısal Hizmetler modülü altındaki **Tanısal Sonuçların Klinik ve Hasta Görünümü (F06-G10)** bileşeninin mimarisini, doktor klinik zaman çizelgesini (Doctor Diagnostic Timeline), hasta portalı onaylı sonuç kurallarını ve ertelemeli yayın politikasını açıklar.

## 1. Mimari ve Amaç

F06-G10, tanısal hizmetler (Laboratuvar, Radyoloji, Patoloji ve Kan Bankası) süreçlerinin çıktılarının iki farklı yetki ve güvenlik kapsamındaki kullanıcı kitlesine güvenli ve düzenli biçimde sunulmasını sağlar:

1. **Hekim ve Bakım Ekibi**: Hastanın tüm tanısal geçmişini (istemler, ara aşamalar, teknik ve klinik onaylar, anormal ve kritik değer bayrakları, patoloji tanıları, kan grubu ve crossmatch uygunluk kayıtları) kronolojik tek bir akışta (Timeline) görür.
2. **Hasta Portalı**: Hasta yalnızca adlarına düzenlenmiş, uzman hekim tarafından kesin onaylanmış (`FinalApproved`, `ReportFinalized`, `Corrected`, `AddendumAdded`) sonuçları görür.

---

## 2. Taslak Sızıntısını Önleme ve Ertelemeli Yayın Politikası

- **Taslak İzolasyonu**: `Draft`, `TechnicallyApproved`, `Ordered`, `Scheduled`, `Acquired`, `SpecimenReceived`, `GrossExamCompleted`, `MicroscopicExamCompleted` ve `ReportDrafted` durumundaki hiçbir kayıt API düzeyinde hasta portalına sızdırılmaz.
- **Kritik Değer Ertelemeli Yayın Kuralı**: Henüz hekim tarafından alındı teyidi (`Acknowledged`) yapılmamış kritik panik değerler için hasta portalında "Hekim Değerlendirmesi Bekleniyor" uyarısı gösterilir; hekim bilgilendirilmeden doğrudan hastaya panik yaratacak ham içerik açılmaz.

---

## 3. Güvenlik, İzolasyon ve Denetim İzi (Audit)

1. **Hasta IDOR Güvenliği**: Hastalar yalnızca kendi adlarına açılmış tanısal sonuçlara erişebilir. Başka bir hastanın kimliği ile yapılan istekler 403 Forbidden ile engellenir.
2. **Denetim İzi (Audit)**: Zaman çizelgesi ve hasta portalı erişimleri `Diagnostics.TimelineDoctorView` ve `Diagnostics.TimelinePatientPortalView` olayları ile denetlenir.
