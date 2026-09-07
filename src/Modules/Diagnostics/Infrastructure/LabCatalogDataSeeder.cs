using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class LabCatalogDataSeeder : ILabCatalogDataSeeder
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public const string CatalogVersion = "DEMO-LAB-2026.1";

    public LabCatalogDataSeeder(
        DiagnosticsDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.LabCatalogItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var items = GetDefaultDemoLabItems(nowUtc);

        _dbContext.LabCatalogItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public static List<LabCatalogItem> GetDefaultDemoLabItems(DateTime nowUtc)
    {
        var list = new List<LabCatalogItem>();

        // 1. Tam Kan Sayımı (Hemogram)
        var cbc = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            "DEMO-LAB-CBC",
            "Tam Kan Sayımı (Hemogram 18 Parametre)",
            "Hematoloji",
            "Venöz Tam Kan",
            "Mor Kapaklı EDTA Tüp",
            isPanel: true,
            turnaroundMinutes: 30,
            CatalogVersion,
            "Lökosit, eritrosit, hemoglobin, hematokrit ve trombosit parametrelerini içeren tam kan sayımı.",
            nowUtc);
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "WBC", "Lökosit (Beyaz Kan Hücresi)", "10^3/µL", 4.0m, 10.5m, 1.5m, 30.0m, "Numeric", 1));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "RBC", "Eritrosit (Kırmızı Kan Hücresi)", "10^6/µL", 4.2m, 5.8m, 2.0m, 7.0m, "Numeric", 2));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "HGB", "Hemoglobin", "g/dL", 12.0m, 17.5m, 7.0m, 20.0m, "Numeric", 3));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "HCT", "Hematokrit", "%", 36.0m, 52.0m, 20.0m, 60.0m, "Numeric", 4));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "PLT", "Trombosit (Platellet)", "10^3/µL", 150m, 450m, 30m, 1000m, "Numeric", 5));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "NEU%", "Nötrofil Yüzdesi", "%", 40.0m, 75.0m, null, null, "Numeric", 6));
        cbc.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), cbc.Id, "LYM%", "Lenfosit Yüzdesi", "%", 20.0m, 45.0m, null, null, "Numeric", 7));
        list.Add(cbc);

        // 2. Açlık Kan Şekeri
        var glu = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000002"),
            "DEMO-LAB-GLU",
            "Açlık Kan Şekeri (Glukoz)",
            "Klinik Biyokimya",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: false,
            turnaroundMinutes: 45,
            CatalogVersion,
            "En az 8 saatlik açlık sonrası plazma/serum glukoz konsantrasyonu.",
            nowUtc);
        glu.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), glu.Id, "GLU", "Açlık Kan Şekeri", "mg/dL", 70.0m, 100.0m, 40.0m, 450.0m, "Numeric", 1));
        list.Add(glu);

        // 3. Lipid Paneli
        var lipid = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000003"),
            "DEMO-LAB-LIPID",
            "Lipid Profili Paneli",
            "Klinik Biyokimya",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: true,
            turnaroundMinutes: 60,
            CatalogVersion,
            "Total Kolesterol, HDL, LDL ve Trigliserit düzeyleri.",
            nowUtc);
        lipid.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lipid.Id, "CHOL", "Total Kolesterol", "mg/dL", 0m, 200m, null, 400m, "Numeric", 1));
        lipid.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lipid.Id, "HDL", "HDL Kolesterol", "mg/dL", 40m, 60m, null, null, "Numeric", 2));
        lipid.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lipid.Id, "LDL", "LDL Kolesterol", "mg/dL", 0m, 130m, null, 250m, "Numeric", 3));
        lipid.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lipid.Id, "TRIG", "Trigliserit", "mg/dL", 0m, 150m, null, 500m, "Numeric", 4));
        list.Add(lipid);

        // 4. Karaciğer Fonksiyon Testleri
        var lft = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000004"),
            "DEMO-LAB-LFT",
            "Karaciğer Fonksiyon Paneli (LFT)",
            "Klinik Biyokimya",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: true,
            turnaroundMinutes: 60,
            CatalogVersion,
            "ALT, AST, ALP, GGT ve Total Bilirubin düzeyleri.",
            nowUtc);
        lft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lft.Id, "ALT", "Alanin Aminotransferaz (ALT)", "U/L", 0m, 45m, null, 500m, "Numeric", 1));
        lft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lft.Id, "AST", "Aspartat Aminotransferaz (AST)", "U/L", 0m, 40m, null, 500m, "Numeric", 2));
        lft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lft.Id, "ALP", "Alkalen Fosfataz (ALP)", "U/L", 40m, 130m, null, null, "Numeric", 3));
        lft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lft.Id, "GGT", "Gama Glutamil Transferaz (GGT)", "U/L", 0m, 55m, null, null, "Numeric", 4));
        lft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), lft.Id, "TBIL", "Total Bilirubin", "mg/dL", 0.2m, 1.2m, null, 15.0m, "Numeric", 5));
        list.Add(lft);

        // 5. Böbrek Fonksiyon Testleri
        var rft = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000005"),
            "DEMO-LAB-RFT",
            "Böbrek Fonksiyon Paneli (RFT & Elektrolit)",
            "Klinik Biyokimya",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: true,
            turnaroundMinutes: 45,
            CatalogVersion,
            "Üre, Kreatinin, eGFR, Sodyum ve Potasyum düzeyleri.",
            nowUtc);
        rft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), rft.Id, "UREA", "Üre", "mg/dL", 17m, 43m, null, 150m, "Numeric", 1));
        rft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), rft.Id, "CREA", "Kreatinin", "mg/dL", 0.6m, 1.2m, null, 5.0m, "Numeric", 2));
        rft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), rft.Id, "EGFR", "Tahmini GFR (eGFR)", "mL/dk/1.73m2", 90m, 150m, 15m, null, "Numeric", 3));
        rft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), rft.Id, "NA", "Sodyum (Na)", "mmol/L", 135m, 145m, 120m, 160m, "Numeric", 4));
        rft.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), rft.Id, "K", "Potasyum (K)", "mmol/L", 3.5m, 5.1m, 2.5m, 6.5m, "Numeric", 5));
        list.Add(rft);

        // 6. Tam İdrar Tahlili
        var urine = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000006"),
            "DEMO-LAB-URINE",
            "Tam İdrar Tahlili (TİT)",
            "Mikrobiyoloji & İdrar",
            "Spot İdrar",
            "Steril İdrar Kabı",
            isPanel: true,
            turnaroundMinutes: 30,
            CatalogVersion,
            "İdrar strip ve mikroskopi analizi.",
            nowUtc);
        urine.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), urine.Id, "UPH", "İdrar pH", "pH", 5.0m, 7.5m, null, null, "Numeric", 1));
        urine.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), urine.Id, "UDENS", "İdrar Dansite", "g/mL", 1.010m, 1.030m, null, null, "Numeric", 2));
        urine.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), urine.Id, "UPROT", "İdrar Protein", "", null, null, null, null, "Negative", 3));
        urine.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), urine.Id, "ULEUK", "İdrar Lökosit", "HPF", 0m, 5m, null, null, "Numeric", 4));
        urine.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), urine.Id, "UERYT", "İdrar Eritrosit", "HPF", 0m, 3m, null, null, "Numeric", 5));
        list.Add(urine);

        // 7. TSH Paneli
        var tsh = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000007"),
            "DEMO-LAB-TSH",
            "Tiroid Fonksiyon Paneli",
            "Hormon & İmmünoloji",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: true,
            turnaroundMinutes: 90,
            CatalogVersion,
            "TSH, Serbest T3 ve Serbest T4.",
            nowUtc);
        tsh.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), tsh.Id, "TSH", "Tiroid Uyarıcı Hormon (TSH)", "µIU/mL", 0.4m, 4.2m, 0.05m, 20.0m, "Numeric", 1));
        tsh.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), tsh.Id, "FT4", "Serbest T4", "ng/dL", 0.8m, 1.8m, null, null, "Numeric", 2));
        list.Add(tsh);

        // 8. C-Reaktif Protein
        var crp = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000008"),
            "DEMO-LAB-CRP",
            "C-Reaktif Protein (Kantitatif CRP)",
            "Klinik Biyokimya",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: false,
            turnaroundMinutes: 30,
            CatalogVersion,
            "Akut faz reaktanı kantitatif CRP ölçümü.",
            nowUtc);
        crp.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), crp.Id, "CRP", "C-Reaktif Protein", "mg/L", 0m, 5.0m, null, 100.0m, "Numeric", 1));
        list.Add(crp);

        // 9. Koagülasyon Paneli
        var coag = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000009"),
            "DEMO-LAB-COAG",
            "Koagülasyon Paneli (PT, aPTT, INR)",
            "Koagülasyon",
            "Plazma",
            "Mavi Kapaklı Sitratlı Tüp",
            isPanel: true,
            turnaroundMinutes: 45,
            CatalogVersion,
            "Protrombin zamanı, aktive parsiyel tromboplastin zamanı ve INR.",
            nowUtc);
        coag.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), coag.Id, "PT", "Protrombin Zamanı (PT)", "sn", 11.0m, 15.0m, null, 30.0m, "Numeric", 1));
        coag.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), coag.Id, "INR", "Uluslararası Düzeltilmiş Oran (INR)", "", 0.8m, 1.2m, null, 4.5m, "Numeric", 2));
        coag.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), coag.Id, "APTT", "aPTT", "sn", 25.0m, 38.0m, null, 70.0m, "Numeric", 3));
        list.Add(coag);

        // 10. Vitamin & Demir
        var vits = LabCatalogItem.Create(
            Guid.Parse("70000000-0000-0000-0000-000000000010"),
            "DEMO-LAB-VITS",
            "Vitamin B12 & Ferritin Paneli",
            "Hormon & İmmünoloji",
            "Serum",
            "Sarı Kapaklı Jelli Biyokimya Tüpü",
            isPanel: true,
            turnaroundMinutes: 90,
            CatalogVersion,
            "Vitamin B12, Folat ve Serum Ferritin düzeyleri.",
            nowUtc);
        vits.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), vits.Id, "B12", "Vitamin B12", "pg/mL", 200m, 900m, 100m, null, "Numeric", 1));
        vits.AddParameter(LabCatalogParameter.Create(Guid.NewGuid(), vits.Id, "FERR", "Ferritin", "ng/mL", 20m, 250m, 5m, 1000m, "Numeric", 2));
        list.Add(vits);

        return list;
    }
}
