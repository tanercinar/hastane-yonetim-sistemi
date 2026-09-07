# Mimari testler

F01-G03, [ADR-0001](../adr/ADR-0001-modular-monolith.md) ve [çözüm yapısında](solution-structure.md) tanımlanan bağımlılık yönlerini iki ayrı seviyede otomatik olarak korur.

## Koruma seviyeleri

### Proje grafiği

`ProjectReferenceArchitectureTests`, `src/` altındaki gerçek `.csproj` dosyalarını okur. Dosya adı benzerliğine güvenmeden referansın çözümlenmiş tam yolunu da doğrular.

- Mevcut 21 üretim projesinin tamamı politikada sınıflandırılmalıdır.
- `BuildingBlocks` ve `Contracts` hiçbir üretim projesine bağımlı olamaz.
- `UI` yalnız `Contracts` projesine bağımlı olabilir.
- `Web.Client` yalnız `UI` ve `Contracts` projelerine bağımlı olabilir.
- Her modül yalnız `BuildingBlocks` projesine doğrudan referans verebilir.
- `Host` composition root olarak mevcut üretim projelerini bağlayabilir.
- Üretim projesinden `tests/` altına veya repository dışındaki benzer adlı bir projeye referans verilemez.

Yeni bir üretim projesi eklendiğinde test bilinçli olarak başarısız olur; proje önce açık bağımlılık politikasına eklenmelidir.

### Namespace ve bytecode bağımlılıkları

`NamespaceDependencyArchitectureTests`, ArchUnitNET ile derlenmiş assembly'leri inceler.

- `*.Domain` türleri UI, Web, Host, ASP.NET Core, EF Core, Application, Infrastructure veya Endpoints türlerine bağımlı olamaz.
- `*.Application` türleri UI, Web, Host, ASP.NET Core, EF Core, Infrastructure veya Endpoints türlerine bağımlı olamaz.
- `BuildingBlocks` ve `Contracts` içindeki ortak türler modül türlerine bağımlı olamaz.
- Bir modülün türleri başka modülün `Infrastructure` namespace'ine bağımlı olamaz.

Domain ve Application klasörleri henüz boş olan modüllerde namespace kuralı doğal olarak ihlal üretmez. Türler eklendiğinde aynı kurallar ek yapılandırma gerektirmeden bytecode üzerinde uygulanır; proje grafiği ise boş iskelette de aktiftir.

## Negatif mutasyon kanıtı

Kabul doğrulamasında Patients modülüne geçici olarak `UI` ve Scheduling proje referansları ile `Patients.Domain -> Scheduling.Infrastructure / ASP.NET Core / UI` bağımlılığı eklendi. Test koşusu aşağıdaki üç testi beklenen biçimde kırdı:

- `ProductionProjectReferencesMustFollowAllowedDirection`
- `DomainTypesMustNotDependOnTechnicalOrOuterLayers`
- `ModuleTypesMustNotDependOnForeignInfrastructure`

Geçici probe dosyaları ve referanslar kaldırıldıktan sonra aynı 10 mimari test yeniden geçti. Repository'de kasıtlı ihlal bırakılmadı.

## Yerel çalıştırma

ArchUnitNET derlenmiş assembly'leri incelediği için hedef komut Debug yapılandırmasını kullanır:

```powershell
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj `
  --configuration Debug `
  --filter "Category=Architecture"
```

Solution önceden derlendiyse:

```powershell
dotnet test .\tests\HospitalManagement.ArchitectureTests\HospitalManagement.ArchitectureTests.csproj `
  --no-build `
  --configuration Debug `
  --filter "Category=Architecture"
```

Başarısızlık mesajı ihlal eden kaynak türü/projeyi ve hedef bağımlılığı gösterir. Testi geçici olarak atlamak, kuralı gevşetmek veya namespace'i gizlemek çözüm değildir; bağımlılık açık contract ya da izin verilen yön üzerinden yeniden kurulmalıdır.
