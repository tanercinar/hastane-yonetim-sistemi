# Ubiquitous Language — Hastane Yönetim Sistemi

Bu sözlük, ürün belgeleri, arayüz, API ve kod için kanonik alan dilidir. Türkçe kullanıcı terimi korunur; önerilen İngilizce kod terimi parantez içinde verilir. “Kaçınılacak adlar” eşanlamlı veya aşırı yüklenmiş kullanımları gösterir.

## Kişiler, kimlik ve organizasyon

| Kanonik terim | Tanım | Kaçınılacak adlar |
|---|---|---|
| **Kişi (Person)** | Hasta veya çalışan olabilen gerçek kişiyi temsil eden demografik özne. | User, account |
| **Kullanıcı (User)** | Kimlik doğrulayabilen teknik hesap. | Kişi, hasta, login |
| **Hasta (Patient)** | Hastane hizmetlerinin öznesi olan kişi kaydı. | Müşteri, kullanıcı |
| **Hasta Numarası (Medical Record Number)** | Kurum içinde hastayı gösteren, kullanıcıya görünür sentetik iş kimliği. | Database id, T.C. kimlik no |
| **Sağlık Çalışanı (Healthcare Professional)** | Klinik bakım veya yardımcı sağlık hizmeti sunmaya yetkili personel profiline sahip kişi. | User, tüm personel |
| **Personel Profili (Staff Profile)** | Bir kişi ile hastanedeki mesleki görev ve organizasyon atamalarını bağlayan kayıt. | User role |
| **Rol (Role)** | Bir kullanıcıya verilebilecek izin grubunun adı. | Yetki, scope |
| **İzin (Permission)** | Belirli bir eylemi yapabilme yetkisi. | Role, access |
| **Kaynak Kapsamı (Resource Scope)** | İznin kendi, atanmış hasta, bölüm veya kurum kaynaklarından hangilerinde geçerli olduğu. | Tenant, role |
| **Bakım İlişkisi (Care Relationship)** | Klinik çalışanın hastaya erişimini meşru kılan aktif atama/karşılaşma/servis ilişkisi. | Her doktor erişimi |
| **Hastane (Hospital)** | Sistemdeki en üst demo sağlık kuruluşu. | Tenant |
| **Şube (Facility)** | Hastanenin fiziksel veya operasyonel hizmet yerleşkesi. | Hastane, bina |
| **Bölüm (Department)** | Personel ve klinik işin bağlı olduğu organizasyon birimi. | Uzmanlık, servis |
| **Uzmanlık (Specialty)** | Sağlık çalışanının veya hizmetin mesleki klinik alanı. | Bölüm |
| **Servis (Ward)** | Yatan hastaların bakım gördüğü yataklı klinik birim. | Bölüm, oda |

## Randevu ve klinik bakım

| Kanonik terim | Tanım | Kaçınılacak adlar |
|---|---|---|
| **Uygunluk Penceresi (Availability Window)** | Bir doktor veya kaynağın randevuya açılabilecek çalışma aralığı. | Appointment |
| **Randevu Slotu (Appointment Slot)** | Belirli zaman ve kaynak için ayrılabilir planlama birimi. | Randevu |
| **Randevu (Appointment)** | Hasta ile hizmet/çalışan arasında planlanan ziyaret taahhüdü. | Muayene, encounter |
| **Check-in** | Hastanın planlanan ziyarete geldiğinin kayıt altına alınması. | Kayıt, kabul |
| **Sıra (Queue Entry)** | Check-in olmuş hastanın günlük hizmet bekleme konumu. | Randevu |
| **Karşılaşma (Encounter)** | Hastaya fiilen sağlık hizmeti sunulan klinik etkileşim. | Randevu, muayene kaydı |
| **Poliklinik Karşılaşması (Outpatient Encounter)** | Yatış gerektirmeyen planlı veya ayaktan klinik karşılaşma. | Appointment |
| **Bakım Epizodu (Episode of Care)** | Aynı sağlık amacı etrafındaki birden çok karşılaşmayı birleştiren üst süreç. | Encounter |
| **Vital Bulgu (Vital Sign Observation)** | Hastanın belirli zamanda ölçülen fizyolojik değeri. | Test result |
| **Alerji/İntolerans (Allergy Intolerance)** | Bir maddeye karşı belgelenmiş olumsuz yanıt veya risk kaydı. | Diagnosis |
| **Problem (Clinical Problem)** | Hastanın izlenen aktif veya geçmiş sağlık durumu. | Tanı |
| **Tanı (Diagnosis)** | Klinik değerlendirme sonucunda encounter'a bağlanan kodlu/serbest klinik sonuç. | Problem, şikâyet |
| **Klinik Not (Clinical Note)** | Şikâyet, öykü, muayene, değerlendirme veya planı belgeleyen kayıt. | Audit log |
| **İmza (Clinical Sign-off)** | Klinik kaydın yazarınca tamamlanıp değişmez sürüm hâline getirilmesi. | Digital certificate |
| **Ek Not (Addendum)** | İmzalı kayda geçmişi bozmadan eklenen yeni açıklama. | Edit |
| **Düzeltme (Correction)** | Hatalı klinik kaydın eski sürümünü koruyarak gerekçeli yeni sürümle düzeltilmesi. | Update, delete |
| **Hatalı Giriş (Entered in Error)** | Klinik olarak hiç gerçekleşmemiş/yanlış kişiye girilmiş kaydın görünür geçmişle geçersiz sayılması. | Hard delete |
| **Konsültasyon (Consultation Request)** | Başka bir klinik uzmanından değerlendirme istenmesi ve yanıt süreci. | Referral |
| **Sevk (Referral/Transfer Out)** | Hastanın kurum dışı başka hizmet sunucusuna yönlendirilmesi. | Konsültasyon, iç transfer |

## İlaç, tanı hizmetleri ve numune

| Kanonik terim | Tanım | Kaçınılacak adlar |
|---|---|---|
| **Klinik İstem (Clinical Order)** | Bir karşılaşma kapsamında ilaç, test, görüntüleme veya başka klinik hizmet talebi. | Result, appointment |
| **Reçete (Prescription)** | Doktorun hastaya yönelik imzalı ilaç kullanım talimatları bütünü. | Medication order (yatan hasta bağlamında) |
| **Reçete Kalemi (Prescription Item)** | Tek ilaç, doz, sıklık, yol ve süre talimatı. | Drug |
| **Teslim (Dispense)** | Eczacının reçeteye dayanarak belirli ürün/lot/miktarı hastaya verdiği kayıt. | Reçete, satış |
| **İlaç Uygulaması (Medication Administration)** | Planlı dozun sağlık çalışanı tarafından hastaya uygulanma kaydı. | Dispense |
| **Stok Hareketi (Stock Movement)** | Ürün miktarını gerekçesiyle artıran veya azaltan değişmez kayıt. | Current stock |
| **Lot (Batch/Lot)** | Aynı üretim grubuna ve son kullanma tarihine sahip stok birimi. | Product |
| **Laboratuvar İstemi (Laboratory Order)** | Bir veya daha fazla test/panel için klinik talep. | Result |
| **Numune (Specimen)** | Laboratuvar incelemesi için hastadan alınan biyolojik materyalin demo kaydı. | Test, sample data |
| **Gözetim Zinciri (Chain of Custody)** | Numunenin toplama, kabul, taşıma ve çalışma hareketlerinin izlenebilir dizisi. | Audit only |
| **Sonuç (Observation Result)** | Test veya ölçümden üretilen, durum ve sürüm taşıyan klinik veri. | Report |
| **Kritik Sonuç (Critical Result)** | İnsan tarafından belirlenmiş demo eşiklere göre acil bildirim gerektiren sonuç. | AI alert |
| **Tanısal Rapor (Diagnostic Report)** | Bir veya daha çok sonucu yorumlayan final laboratuvar/radyoloji/patoloji belgesi. | Result |
| **Görüntüleme Çalışması (Imaging Study)** | Mock PACS içinde seri/instance metadata'sını gruplayan radyoloji çalışması. | Image file |

## Yatış ve kritik bakım

| Kanonik terim | Tanım | Kaçınılacak adlar |
|---|---|---|
| **Yatış (Admission)** | Hastanın belirli süreyle yataklı bakım sorumluluğuna alınması. | Encounter, check-in |
| **Oda (Room)** | Servis içindeki bir veya daha çok yatağı barındıran fiziksel alan. | Ward |
| **Yatak (Bed)** | Aynı anda en fazla bir aktif yatışa atanabilen bakım kaynağı. | Room |
| **Yatak Ataması (Bed Assignment)** | Bir yatış ile yatağın belirli zaman aralığındaki ilişkisi. | Admission |
| **İç Transfer (Internal Transfer)** | Aktif yatışın servis/oda/yatak veya sorumlu ekip değişikliği. | Sevk |
| **Taburculuk (Discharge)** | Hastanenin aktif yatış sorumluluğunu klinik özetle sonlandırması. | Cancel admission |
| **Bakım Planı (Care Plan)** | Hastanın hemşirelik problemleri, hedefleri ve görevleri bütünü. | Clinical note |
| **İlaç Uygulama Kaydı (eMAR)** | Planlanan dozların uygulama durumlarını gösteren kayıt dizisi. | Prescription |
| **Triyaj (Triage Assessment)** | Acil başvurunun insan tarafından demo öncelik seviyesine sınıflandırılması. | Diagnosis, AI decision |
| **Devir Teslim (Clinical Handoff)** | Bakım sorumluluğu değişirken özet, sahiplik ve açık görevlerin kabulü. | Transfer only |

## Güvenlik, gizlilik ve entegrasyon

| Kanonik terim | Tanım | Kaçınılacak adlar |
|---|---|---|
| **Sentetik Veri (Synthetic Data)** | Gerçek bir kişiden türetilmemiş ve `DEMO` olarak işaretlenmiş kurgu veri. | Anonim gerçek veri |
| **Özel Nitelikli Veri (Special Category Data)** | Sağlık, biyometrik veya genetik gibi daha sıkı koruma gerektiren kişisel veri sınıfı. | PHI (Türk hukuku terimi yerine) |
| **Denetim Olayı (Audit Event)** | Kim, ne zaman, hangi hedefte, hangi eylemi, hangi sonuçla yaptığına dair değişmez güvenlik kaydı. | Clinical note, application log |
| **Uygulama Logu (Application Log)** | Sistemin teknik davranışını açıklayan ve klinik içerik taşımaması gereken kayıt. | Audit event |
| **Saklama Politikası (Retention Policy)** | Veri kategorisinin hangi koşul ve süreyle tutulup nasıl imha/anonimleştirileceğini belirleyen kural. | Backup policy |
| **Anonimleştirme (Anonymization)** | Verinin başka verilerle eşleştirilse dahi kişiye bağlanamaz hâle getirilmesi. | Masking, pseudonymization |
| **Maskeleme (Masking)** | Yetkisiz veya gereksiz alanların ekranda kısmen gizlenmesi. | Anonymization |
| **Mock Entegrasyon (Mock Integration)** | Gerçek dış sisteme bağlanmadan onun sözleşme ve hata davranışını simüle eden adaptör. | Sandbox/production integration |

## Temel ilişkiler

- Bir **Kişi**, sıfır veya bir **Hasta** kaydına ve sıfır veya daha çok **Personel Profili** atamasına sahip olabilir.
- Bir **Kullanıcı**, bir **Kişi** ile ilişkilidir; hesap ile alan kişisi aynı kavram değildir.
- Bir **Randevu** sıfır veya bir **Karşılaşma** başlatır; randevu hizmet planı, karşılaşma sunulan hizmettir.
- Bir **Karşılaşma**, bir hastaya ve bir veya daha çok katılımcı sağlık çalışanına aittir.
- Bir **Klinik İstem**, bir **Karşılaşma** içinde oluşturulur ve sıfır veya daha çok **Sonuç** üretebilir.
- Bir **Reçete**, bir veya daha çok **Reçete Kalemi** içerir; bir kalem sıfır veya daha çok **Teslim** üretebilir.
- Bir **Yatış**, bir veya daha çok ardışık **Yatak Ataması** içerir; aynı yatakta zamanları çakışan iki aktif atama olamaz.
- Bir **Klinik Not** imzalandıktan sonra değiştirilmez; **Ek Not**, **Düzeltme** veya **Hatalı Giriş** kaydıyla takip edilir.
- Bir **Denetim Olayı** klinik içeriğin kopyası değildir ve normal kullanıcılarca değiştirilemez.

## Örnek diyalog

> **Geliştirici:** “Hasta randevuya geldiğinde randevuyu muayene durumuna mı geçiriyoruz?”
>
> **Alan uzmanı:** “Hayır. **Randevu** planlama kaydıdır; önce **Check-in** yapılır, ardından gerçek bakım için ayrı bir **Karşılaşma** başlatılır.”
>
> **Geliştirici:** “Doktor imzalı **Klinik Not** içinde hata görürse güncelleyebilir mi?”
>
> **Alan uzmanı:** “Sessiz güncelleme yapamaz. Gerekçeli bir **Düzeltme** veya **Ek Not** oluşturur; önceki sürüm ve **Denetim Olayı** korunur.”
>
> **Geliştirici:** “Eczacının encounter notlarını görmesi gerekir mi?”
>
> **Alan uzmanı:** “Hayır. **Eczacı** yalnızca **Reçete**, gerekli güvenlik özeti ve **Teslim** için gereken minimum veriyi görür.”

## İşaretlenen belirsizlikler ve kanonik kararlar

- **“Kayıt”** hem hasta ana kaydı, hem check-in, hem de herhangi bir veri satırı için kullanılabilir. Cümlede mutlaka **Hasta Kaydı**, **Check-in**, **Klinik Not** veya **Denetim Olayı** denmelidir.
- **“Muayene”** randevu veya encounter anlamında kullanılmamalıdır. Plan için **Randevu**, fiilî bakım için **Poliklinik Karşılaşması** kullanılır.
- **“Kabul”** hem check-in hem yatış anlamında belirsizdir. Ayaktan gelişte **Check-in**, yataklı bakımda **Yatış Kabulü** denir.
- **“Transfer”** kurum içi yatak/bölüm değişimi ile kurum dışı yönlendirmeyi karıştırır. İlki **İç Transfer**, ikincisi **Sevk**tir.
- **“Order/İstem”** laboratuvar sonucu veya reçete değildir. Üst kavram **Klinik İstem**; uzmanlaşmış kayıtlar **Laboratuvar İstemi**, **Görüntüleme İstemi** ve **Reçete**dir.
- **“Silme”** imzalı klinik kayda uygulanacak sıradan CRUD işlemi değildir. Klinik hata **Hatalı Giriş/Düzeltme**, kişisel veri yaşam döngüsü ise **İmha/Anonimleştirme** olarak adlandırılır.
- **“Yetki”** rol ile eş anlamlı değildir. **Rol** izin grubudur; gerçek karar **İzin + Kaynak Kapsamı + Bakım İlişkisi** ile verilir.
- **“Anonim veri”** ile maskelenmiş veya takma ad verilmiş veri aynı değildir. Projede seed için **Sentetik Veri**, UI için **Maskeleme**, geri bağlanamaz dönüşüm için **Anonimleştirme** denir.
