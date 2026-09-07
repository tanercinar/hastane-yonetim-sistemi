# Çözüm yapısı ve proje sınırları

Bu belge, Faz 1 başlangıcındaki derlenebilir çözüm iskeletini ve izin verilen proje bağımlılıklarını tanımlar. Kaynak projeler `src/`, doğrulama projeleri `tests/` altında bulunur. Mimari yaklaşımın gerekçesi [ADR-0001](../adr/ADR-0001-modular-monolith.md) ve istemci ayrımı [ADR-0002](../adr/ADR-0002-api-first-blazor-clients.md) içindedir.

## Üretim projeleri

| Proje | Sorumluluk | İzin verilen doğrudan proje bağımlılıkları |
| --- | --- | --- |
| `HospitalManagement.BuildingBlocks` | Modüllerin paylaşabildiği teknik ve alan-bağımsız yapı taşları | Yok |
| `HospitalManagement.Contracts` | İstemcilerle API arasında taşınan sözleşmeler | Yok |
| `HospitalManagement.UI` | Web ve ileride MAUI tarafından paylaşılabilen host-bağımsız Razor bileşenleri | `Contracts` |
| `HospitalManagement.Web.Client` | Blazor WebAssembly istemci kompozisyonu | `UI`, `Contracts` |
| `HospitalManagement.Host` | ASP.NET Core composition root; API, kimlik, SignalR ve modül uçlarının bağlanacağı süreç | `Web.Client`, `UI`, `Contracts`, `BuildingBlocks`, tüm modül assembly'leri |

## Modül assembly'leri

Her modül ayrı assembly'dir ve başlangıç iskeletinde yalnız `HospitalManagement.BuildingBlocks` projesine referans verir:

- `IdentityAccess`, `Organization`, `Patients`, `Scheduling`
- `ClinicalRecords`, `Pharmacy`, `Diagnostics`, `Inpatient`, `Emergency`
- `SurgeryCriticalCare`, `SpecialtyCare`, `Inventory`
- `Notifications`, `Reporting`, `Interoperability`, `AuditPrivacy`

Bir modül başka bir modülün projesine, veritabanı bağlamına veya altyapı ayrıntısına doğrudan referans veremez. Modüller arası iş birliği ilerleyen görevlerde açık sözleşmeler, uygulama servisleri veya olaylar üzerinden kurulacaktır.

## Test projeleri

| Proje | Test sınırı |
| --- | --- |
| `HospitalManagement.UnitTests` | BuildingBlocks ve modüllerin hızlı davranış testleri |
| `HospitalManagement.IntegrationTests` | Host üzerinden entegrasyon ve API sözleşmesi testleri |
| `HospitalManagement.ArchitectureTests` | Tüm üretim assembly'lerinin bağımlılık sınırları |
| `HospitalManagement.ComponentTests` | UI ve Web.Client Razor bileşenleri |
| `HospitalManagement.EndToEndTests` | Host üzerinden tarayıcı düzeyindeki kritik yolculuklar |

Mimari sınır testleri F01-G03 kapsamında etkinleştirilmiştir; kurallar ve yerel komutlar [mimari testler belgesinde](architecture-tests.md) açıklanır. Diğer test araçlarının fixture ve örnek testleri F01-G10'da kurulacaktır.

## Bağımlılık yönü

```text
Host ───────────────► Modules ─────────► BuildingBlocks
  │
  ├────────────────► Web.Client ──────► UI ──────► Contracts
  ├────────────────► UI
  ├────────────────► Contracts
  └────────────────► BuildingBlocks
```

Aşağıdaki referanslar yasaktır:

- `BuildingBlocks` veya `Contracts` katmanından herhangi bir modüle, UI'a ya da Host'a referans.
- `UI` ya da `Web.Client` katmanından modül implementation assembly'lerine referans.
- Bir modülden başka bir modüle doğrudan proje referansı.
- Herhangi bir üretim projesinden `tests/` altındaki projelere referans.

Bu kurallar F01-G03'te eklenen proje grafiği ve derlenmiş bytecode testleriyle korunur.

## Yerel doğrulama

```powershell
dotnet restore .\HospitalManagement.slnx
dotnet build .\HospitalManagement.slnx --no-restore --configuration Release
dotnet test .\HospitalManagement.slnx --no-build --configuration Release
```

Başarılı G02 doğrulamasında solution içindeki 26 proje derlenir ve üretim grafiğinde yukarıdaki yönlerin dışında proje referansı bulunmaz.
