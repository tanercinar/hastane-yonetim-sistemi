# Organizasyon modeli

Bu belge `F02-G01` ile kurulan organizasyon modelinin sınırlarını ve kalıcılık kurallarını tanımlar. Model yalnız sentetik `DEMO` veri içindir; bir insan kaynakları veya bordro sistemi değildir.

## Modül sahipliği

`Organization` modülü aşağıdaki kavramların tek yazma sahibidir:

| Kavram | Amaç | Temel kapsam |
|---|---|---|
| `Hospital` | En üst organizasyon ve kaynak kapsamı | Kod, görünen ad, aktiflik |
| `Facility` | Hastaneye bağlı fiziksel/operasyonel şube | Hastane, kod, görünen ad |
| `Department` | Şube içindeki hiyerarşik klinik, tanı veya operasyon birimi | Hastane, şube, üst bölüm, kategori |
| `Specialty` | Klinik uzmanlık kataloğu | Hastane, kod, görünen ad |
| `StaffProfile` | Bir kişinin hastane içindeki klinik çalışma profili | Opak kişi kimliği, personel numarası, meslek, birincil uzmanlık |
| `StaffDepartmentAssignment` | Klinik personelin zaman aralıklı bölüm görevi | Hastane, personel, bölüm, birincillik, başlangıç/bitiş |

Diğer modüller bu modülün tablolarına veya `OrganizationDbContext` sınıfına doğrudan erişmez. Gerektiğinde kimlikleri kendi kayıtlarında opak referans olarak taşır ve ileriki görevlerde tanımlanacak uygulama sözleşmesi/API üzerinden doğrular.

`StaffProfile.PersonId` de bir çapraz modül veritabanı foreign key'i değildir. İlgili kişi kaydının yaşam döngüsü kendi modülünde kalır.

## Bilinçli kapsam dışı alanlar

`StaffProfile` bir tam İK çalışan kaydı değildir. Aşağıdaki alan ve süreçler Organization şemasına eklenmez:

- maaş, bordro, vergi, banka ve yan haklar;
- izin, puantaj, vardiya bordrosu ve performans değerlendirmesi;
- iş sözleşmesi, özlük dosyası ve işe alım/işten ayrılma belgeleri;
- ev adresi, kişisel iletişim veya kimlik belgesi gibi kişi verilerinin kopyaları.

Klinik erişim yetkileri de personel profilinde saklanmaz. İzinler ve kaynak kapsamı `IdentityAccess` görevlerinde; gerçekleşen erişim kayıtları `AuditPrivacy` görevinde ele alınır.

## Kalıcılık ve bütünlük

Organization tabloları PostgreSQL `organization` şemasında, modüle özel `__ef_migrations_history` tablosuyla tutulur. İlk migration `InitialOrganization` adını taşır.

Veritabanı aşağıdaki kuralları uygulama koduna ek olarak zorunlu kılar:

- hastane kodu sistem genelinde; şube, uzmanlık ve personel numarası hastane içinde benzersizdir;
- bölüm kodu hastane ve şube içinde benzersizdir;
- bölümün şubesi ve üst bölümü aynı hastane kapsamındadır; üst bölüm ayrıca aynı şubededir;
- personelin birincil uzmanlığı kendi hastanesindedir;
- bölüm atamasındaki personel ve bölüm aynı hastanededir;
- bir personelin aynı bölümde yalnız bir aktif ataması ve tüm bölümler arasında yalnız bir aktif birincil ataması olabilir;
- atama bitişi başlangıçtan sonra olmalıdır;
- kayıtlar `Version` ile optimistic concurrency denetimine tabidir; eski sürümle yazma reddedilir.

Tüm kalıcı zamanlar UTC'dir. Kodlar baştaki/sondaki boşluklardan arındırılır ve büyük harfe normalize edilir. Silme zincirleri yerine ilişkiler `Restrict` davranışındadır; ileriki iş akışları geçmişi koruyacak biçimde pasifleştirme veya atamayı sonlandırma kullanmalıdır.

## Deterministik DEMO seed ağacı

İlk migration aşağıdaki sentetik kataloğu üretir:

```text
DEMO Eğitim Hastanesi
└── DEMO Merkez Şube
    ├── DEMO Klinik Hizmetler
    │   ├── DEMO İç Hastalıkları Bölümü
    │   └── DEMO Kardiyoloji Bölümü
    └── DEMO Tanı Hizmetleri
        ├── DEMO Laboratuvar Bölümü
        └── DEMO Radyoloji Bölümü
```

Uzmanlık seed'i İç Hastalıkları, Kardiyoloji, Tıbbi Biyokimya ve Radyoloji kayıtlarını içerir. Kişi veya personel seed edilmez; böylece örnek organizasyon ağacı gerçek kişisel veri ya da gerçeğe benzeyen çalışan özlük verisi içermez.

## Migration ve test komutları

Yerel veritabanına platform ve Organization migration'larını birlikte uygulamak için:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\apply-local-database-foundation.ps1
```

Yeni Organization migration'ı yalnız model bilinçli olarak değiştirildiğinde oluşturulur:

```powershell
dotnet ef migrations add MigrationAdi `
  --project .\src\Modules\Organization\HospitalManagement.Modules.Organization.csproj `
  --startup-project .\src\HospitalManagement.Host\HospitalManagement.Host.csproj `
  --context OrganizationDbContext `
  --output-dir Infrastructure\Persistence\Migrations
```

Hedef testler:

```powershell
dotnet test .\tests\HospitalManagement.UnitTests\HospitalManagement.UnitTests.csproj `
  -c Release --filter "Roadmap=F02-G01"

dotnet test .\tests\HospitalManagement.IntegrationTests\HospitalManagement.IntegrationTests.csproj `
  -c Release --filter "Roadmap=F02-G01"
```

Integration testleri gerçek, geçici PostgreSQL Testcontainer kullanır ve Docker Desktop gerektirir.
