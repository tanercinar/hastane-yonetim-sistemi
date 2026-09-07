# Repository Politikası Doğrulaması

Bu rehber `F01-G01` kapsamında oluşturulan SDK, restore, derleme, analyzer ve ignore politikasının manuel olarak nasıl kontrol edileceğini gösterir.

## Beklenen yapılandırma

- SDK: `.NET SDK 10.0.201`, exact pin (`rollForward: disable`)
- Target framework varsayılanı: `net10.0`
- C#: `14.0`
- Nullable ve implicit usings: etkin
- Compiler/analyzer warning: build hatası
- Analyzer seviyesi: `10.0-recommended`
- NuGet: merkezi sürüm yönetimi, lock file ve moderate+ güvenlik audit'i
- CI: locked restore ve deterministic build

## SDK kontrolü

Repository kökünde:

```powershell
dotnet --version
dotnet --info
```

İlk komut `10.0.201` dönmeli; `dotnet --info` içindeki `global.json file` bu repository'deki `global.json` olmalıdır. SDK yoksa farklı sürüme sessizce geçilmez; aynı SDK kurulmalıdır.

## Restore ve build kontrolü

`F01-G02` ile solution oluşturulduktan sonra kanonik kontrol:

```powershell
dotnet restore
dotnet build --no-restore --configuration Release
```

Lock dosyaları oluşturulup commit edildikten sonra CI eşdeğeri:

```powershell
$env:CI = 'true'
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
Remove-Item Env:CI
```

İkinci restore, lock dosyasıyla proje bağımlılıkları uyuşmuyorsa başarısız olmalıdır. Build sıfır warning ve sıfır error ile tamamlanmalıdır.

## MSBuild politika kontrolü

Bir proje eklendikten sonra etkin değerler şöyle sorgulanabilir:

```powershell
dotnet msbuild <PROJE.csproj> `
  -getProperty:TargetFramework `
  -getProperty:LangVersion `
  -getProperty:Nullable `
  -getProperty:ImplicitUsings `
  -getProperty:TreatWarningsAsErrors `
  -getProperty:AnalysisLevel `
  -getProperty:Deterministic `
  -getProperty:ManagePackageVersionsCentrally `
  -getProperty:RestorePackagesWithLockFile
```

Beklenen değerler sırasıyla `net10.0`, `14.0`, `enable`, `enable`, `true`, `10.0-recommended`, `true`, `true`, `true`dir.

## Ignore politikası kontrolü

```powershell
git check-ignore -v --no-index -- .env
git check-ignore -v --no-index -- appsettings.Local.json
git check-ignore -v --no-index -- secrets.json
git check-ignore -v --no-index -- local.sqlite
git check-ignore -v --no-index -- certificate.pfx
```

Her komut `.gitignore` içindeki eşleşen kuralı göstermelidir. Aşağıdaki dosyalar ignore edilmemelidir:

```powershell
git check-ignore --no-index -- global.json
git check-ignore --no-index -- Directory.Build.props
git check-ignore --no-index -- Directory.Packages.props
git check-ignore --no-index -- NuGet.Config
git check-ignore --no-index -- .env.example
```

Bu komutların çıktı vermemesi ve exit code `1` dönmesi, dosyanın izlenebilir olduğu anlamına gelir.

## F01-G01 doğrulama kanıtı

21 Ağustos 2026'da geçici, Git tarafından ignore edilen `net10.0` console probe üzerinde:

- normal restore ve locked restore başarılı,
- iki temiz Release build sıfır warning/error ile başarılı,
- iki build'in DLL ve PDB SHA-256 değerleri aynı,
- bilinçli `IDE1006` adlandırma ihlali build hatasına dönüştü,
- dokuz secret/yerel çıktı örneği ignore edildi,
- izlenmesi gereken altı repository dosyası ignore edilmedi.

Probe doğrulama sonunda kaldırılmıştır; kalıcı uygulama/solution iskeleti `F01-G02` kapsamındadır.
