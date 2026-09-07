# HL7 FHIR R4 Demo API ve Dışa Aktarma (FHIR R4 Demo API & Export)

## 1. Amaç ve Kapsam

Bu belge, **Faz 10 — Mock entegrasyonlar ve birlikte çalışabilirlik** kapsamında `F10-G02 — FHIR demo API` görevinin teknik mimarisini, desteklenen kaynak modellerini (Patient, Practitioner, Appointment, Encounter, Observation, DiagnosticReport, MedicationRequest), JSON serileştirme kurallarını ve `$export` paketleme sözleşmesini açıklar.

FHIR katmanı, uluslararası HL7 FHIR R4 standardına uyumlu veri yapıları üretir ve açık kaynaklı sağlık entegrasyon testlerini simüle eder.

> [!IMPORTANT]
> **Kapsam ve Uyarı:** Bu modül sertifikalı tam kapsamlı bir FHIR sunucusu (full certified FHIR server) iddiası taşımaz; eğitim ve portföy amaçlı sentetik okuma ve export sağlar. Tüm kaynak kimlikleri `DEMO-*` öneklidir.

---

## 2. Desteklenen FHIR R4 Kaynakları

| Kaynak Türü | Model Sınıfı | Tanım ve Standart Kodlamalar |
|---|---|---|
| **CapabilityStatement** | `FhirCapabilityStatement` | Sunucu yetenek bildirimi (`4.0.1`, JSON formatı) |
| **Patient** | `FhirPatient` | Sentetik hasta demografisi (İsim, cinsiyet, doğum tarihi, iletişim) |
| **Practitioner** | `FhirPractitioner` | Yetkili hekim/personel kimlik ve uzmanlık bilgisi |
| **Appointment** | `FhirAppointment` | Randevu durumu, zaman aralığı ve katılımcı referansları |
| **Encounter** | `FhirEncounter` | Klinik temas/başvuru kaydı, sınıflandırma (`AMB`, `IMP`, `EMER`) |
| **Observation** | `FhirObservation` | Vital bulgular ve laboratuvar parametreleri (LOINC, UCUM birimleri) |
| **DiagnosticReport** | `FhirDiagnosticReport` | Laboratuvar ve radyoloji sonuç raporları |
| **MedicationRequest** | `FhirMedicationRequest` | Reçete ve ilaç uygulama talimatı (ATC kodları, dozaj) |
| **Bundle** | `FhirBundle` | `$export` ve arama sonuçları için kaynak koleksiyon paketi (`type: "collection"`) |

---

## 3. Güvenlik, İzin ve Veri Minimizasyonu

- **Erişim Kontrolü:** FHIR uç noktaları genel API yetkilendirme standartlarına tabidir. Kimliksiz istekler `401 Unauthorized` ile reddedilir. Hasta düzeyi `$export`, ayrıca `interoperability.fhir.export` izni ister; varsayılan allow yalnız `ChiefMedicalOfficer`, hasta ve `SystemAdministrator` deny'dır.
- **Demo veri sınırı:** Bu aşamadaki kaynak üreticileri gerçek klinik depolardan veri okumaz; verilen GUID için yalnız sentetik FHIR örneği üretir. Bu nedenle normal kaynak GET'leri gerçek hasta portföyü veya bakım ilişkisi kanıtı sayılmaz. Gerçek depoya bağlanmadan önce her hasta kaynağı ayrıca Resource Scope + Care Relationship ile korunmalıdır.
- **Mock Motoru Entegrasyonu:** Tüm FHIR çağrıları `IIntegrationMockEngine` üzerinden geçer; yapılandırılmış gecikme (latency), hata enjeksiyonu ve devre kesici korumasına tabidir.

---

## 4. API Uç Noktaları

| Metot | Yol | Açıklama |
|---|---|---|
| `GET` | `/api/v1/interoperability/fhir/r4/metadata` | FHIR CapabilityStatement meta verisini getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/Patient/{id}` | Belirtilen hastanın FHIR Patient kaynağını getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/Practitioner/{id}` | Belirtilen personelin FHIR Practitioner kaynağını getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/Observation/{id}` | Belirtilen gözlem/vital FHIR Observation kaynağını getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/DiagnosticReport/{id}` | Belirtilen tanısal raporun FHIR DiagnosticReport kaynağını getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/MedicationRequest/{id}` | Belirtilen reçetenin FHIR MedicationRequest kaynağını getirir |
| `GET` | `/api/v1/interoperability/fhir/r4/Patient/{id}/$export` | Hastanın tüm klinik kaynaklarını içeren FHIR Bundle (`collection`) döndürür |

---

## 5. Doğrulama ve Testler

- **Birim Testleri (`FhirR4DomainTests`):** FHIR R4 JSON serileştirme standartları, `Bundle` paketleme doğrulaması, LOINC kodlu `Observation` değer atamaları.
- **Entegrasyon Testleri (`FhirDemoIntegrationTests`):** PostgreSQL üzerinde metadata/örnek kaynaklar; `$export` için anonim `401`, hasta `403`, yetkili başhekim `200`; FHIR MOCK çevrimdışıyken exception ayrıntısı sızdırmayan `503 Problem Details` doğrulaması.
