using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class RadiologyCatalogDataSeeder : IRadiologyCatalogDataSeeder
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RadiologyCatalogDataSeeder(
        DiagnosticsDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await _dbContext.RadiologyCatalogItems.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var items = new List<RadiologyCatalogItem>
        {
            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000001"),
                "DEMO-RAD-CHEST-XR",
                "Akciğer Grafisi (PA / Göğüs Radyografisi)",
                RadiologyModality.XR,
                "Toraks / Göğüs",
                "Standart posteroanterior akciğer ve mediastinum görüntülemesi.",
                "Üst giysiler ve metal takılar çıkarılmalıdır.",
                false,
                10,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000002"),
                "DEMO-RAD-KNEE-XR",
                "Diz Grafisi (İki Yönlü AP/Lateral)",
                RadiologyModality.XR,
                "Alt Ekstremite / Diz",
                "Diz eklemi kemik ve eklem aralığı değerlendirmesi.",
                null,
                false,
                10,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000003"),
                "DEMO-RAD-BRAIN-MRI",
                "Beyin Manyetik Rezonans Görüntüleme (Kranial MRG)",
                RadiologyModality.MR,
                "Baş-Boyun / Kranial",
                "Beyin parankimi, ventriküller ve vasküler yapılar.",
                "Metalik implant ve kalp pili sorgulanmalıdır. Klostrofobi varlığı belirtilmelidir.",
                false,
                30,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000004"),
                "DEMO-RAD-LUMBAR-MRI",
                "Lomber Spinal Manyetik Rezonans Görüntüleme",
                RadiologyModality.MR,
                "Omurga / Lomber",
                "Lomber disk herniasyonu ve spinal kanal değerlendirmesi.",
                "Tüm metal eşyalar soyunma kabininde bırakılmalıdır.",
                false,
                25,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000005"),
                "DEMO-RAD-ABDOMEN-CT",
                "Tüm Batın ve Pelvis Bilgisayarlı Tomografi (Kontrastlı)",
                RadiologyModality.CT,
                "Abdomen ve Pelvis",
                "Karaciğer, dalak, pankreas, böbrekler ve gastrointestinal traktus.",
                "6 saat açlık gereklidir. Serum kreatinin değeri ve kontrast madde alerji öyküsü kontrol edilmelidir.",
                true,
                20,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000006"),
                "DEMO-RAD-THORAX-CT",
                "Toraks Yüksek Çözünürlüklü Bilgisayarlı Tomografi (HRCT)",
                RadiologyModality.CT,
                "Toraks / Akciğer Parankimi",
                "İnterstisyel akciğer hastalıkları ve nodül takibi.",
                "Nefes tutma talimatlarına uyulmalıdır.",
                false,
                15,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000007"),
                "DEMO-RAD-THYROID-US",
                "Tiroid ve Boyun Ultrasonografisi",
                RadiologyModality.US,
                "Baş-Boyun / Tiroid",
                "Tiroid bezi parankimi, nodül ve servikal lenf nodu haritalaması.",
                "Ön hazırlık gerekmez.",
                false,
                15,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000008"),
                "DEMO-RAD-ABDOMEN-US",
                "Tüm Batın Ultrasonografisi",
                RadiologyModality.US,
                "Abdomen",
                "Safra kesesi, karaciğer, böbrek ve mesane değerlendirmesi.",
                "Sabah açlığı (en az 8 saat) ve dolu mesane gereklidir.",
                false,
                20,
                nowUtc),

            RadiologyCatalogItem.Create(
                Guid.Parse("60000000-0000-0000-0000-000000000009"),
                "DEMO-RAD-MAMMO-MG",
                "Bilateral Dijital Mamografi (CC/MLO)",
                RadiologyModality.MG,
                "Meme",
                "Meme kanseri tarama ve lezyon karakterizasyonu.",
                "Deodorant, pudra ve vücut kremi kullanılmamalıdır.",
                false,
                20,
                nowUtc),
        };

        _dbContext.RadiologyCatalogItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
