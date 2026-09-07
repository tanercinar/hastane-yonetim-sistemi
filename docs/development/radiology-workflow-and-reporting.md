# Radyoloji İş Akışı ve Raporlama (F06-G06)

Bu doküman, Tanısal Hizmetler modülü altındaki **Radyoloji İş Akışı ve Raporlama (F06-G06)** dikey diliminin mimarisini, veri modelini, API sözleşmelerini, iş kurallarını ve güvenlik kontrollerini açıklar.

## 1. Mimari ve Kapsam

Radyoloji iş akışı; hekimin tanısal istem vermesinden başlayarak, modalite randevulama, radyoloji teknisyeni tarafından görüntü çekimi (acquisition), uzman radyolog tarafından rapor taslağı oluşturma, raporu kesinleştirme (finalization) ve kesinleşmiş rapora ek not/düzeltme (addendum) ekleme süreçlerini uçtan uca yönetir.

### Modaliteler (`RadiologyModality`)
- `XR`: Direkt Röntgen / Konvansiyonel Grafi (PA Akciğer, Diz grafisi vb.)
- `CT`: Bilgisayarlı Tomografi (Toraks BT, Abdomen BT vb.)
- `MR`: Manyetik Rezonans Görüntüleme (Beyin MRG, Lomber Spinal MRG vb.)
- `US`: Ultrasonografi (Tiroid USG, Tüm Batın USG vb.)
- `MG`: Mamografi (Bilateral Dijital Mamografi vb.)

---

## 2. Durum Yaşam Döngüsü ve Klinik Değişmezlik

Radyoloji tetkik kaydı (`RadiologyStudy`) aşağıdaki durum adımlarını izler:
1. `Ordered`: Hekim istemiyle tetkik kaydı ve sentetik Accession Numarası (`DEMO-ACC-YYYYMMDD-XXXXXX`) oluşturulur.
2. `Scheduled`: Tetkik için randevu zamanı belirlenir.
3. `Acquired`: Radyoloji teknisyeni tarafından çekim tamamlanır ve teknisyen protokol notu kaydedilir. İstem kalemi `InAnalysis` durumuna geçer.
4. `ReportDrafted`: Uzman radyolog rapor bulgularını taslak olarak kaydeder.
5. `ReportFinalized`: Uzman radyolog bulgular ve klinik kanaati (`Impression`) onaylayarak kesinleştirir. İstem kalemi `Reported` durumuna geçer.
6. `AddendumAdded`: Kesinleşmiş rapor klinik olarak değiştirilemez (immutability). Hekim veya radyolog açıklaması gerektiğinde zaman damgalı ve hekim kimlikli ek rapor (`Addendum`) iliştirilir.
7. `Cancelled`: Yalnızca kesinleşmemiş çekimler gerekçe belirtilerek iptal edilebilir.

---

## 3. Güvenlik ve Yetkilendirme

- `HospitalPermissions.Diagnostics.RadiologyWorklistView`: Radyoloji teknisyeni, radyolog, hekim ve başhekim iş listesini görüntüler.
- `HospitalPermissions.Diagnostics.RadiologyStudyComplete`: Randevu verme ve çekim tamamlama (teknisyen/başhekim).
- `HospitalPermissions.Diagnostics.RadiologyReportFinalize`: Rapor taslağı oluşturma, kesinleştirme, addendum ekleme ve iptal (radyolog/hekim/başhekim).
- **Hasta IDOR Koruması**: Hasta rolündeki kullanıcılar yalnızca kendi adlarına ait ve durumu `ReportFinalized` veya `AddendumAdded` olan radyoloji raporlarını görüntüleyebilir; taslak veya çekim aşamasındaki kayıtları göremez.

---

## 4. Denetim İzi (Audit Logging)

Aşağıdaki eylemler `Diagnostics` hedef alanı ve `RadiologyStudy` hedef kaynağı altında denetlenir:
- `Diagnostics.RadiologyStudyCreate`
- `Diagnostics.RadiologyStudySchedule`
- `Diagnostics.RadiologyAcquisitionComplete`
- `Diagnostics.RadiologyReportDraft`
- `Diagnostics.RadiologyReportFinalize`
- `Diagnostics.RadiologyReportAddendum`
- `Diagnostics.RadiologyStudyCancel`
