using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public sealed class MedicationCatalogDataSeeder : IMedicationCatalogDataSeeder
{
    public const string DefaultCatalogVersion = "DEMO-MED-2026.1";
    private static readonly Guid PharmacyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000007");

    private readonly PharmacyDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public MedicationCatalogDataSeeder(
        PharmacyDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await _dbContext.MedicationCatalogItems
            .CountAsync(m => m.CatalogVersion == DefaultCatalogVersion, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (existingCount == 0)
        {
            var items = GetDefaultDemoMedications(nowUtc);
            _dbContext.MedicationCatalogItems.AddRange(items);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var stockCount = await _dbContext.MedicationStockItems.CountAsync(cancellationToken);
        if (stockCount == 0)
        {
            var allMeds = await _dbContext.MedicationCatalogItems.ToListAsync(cancellationToken);
            var (stockItems, transactions) = GetDefaultDemoStockItemsAndTransactions(allMeds, nowUtc);
            _dbContext.MedicationStockItems.AddRange(stockItems);
            _dbContext.MedicationStockTransactions.AddRange(transactions);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static (IReadOnlyList<MedicationStockItem> Items, IReadOnlyList<MedicationStockTransaction> Transactions) GetDefaultDemoStockItemsAndTransactions(
        IReadOnlyList<MedicationCatalogItem> meds,
        DateTime nowUtc)
    {
        var items = new List<MedicationStockItem>();
        var transactions = new List<MedicationStockTransaction>();

        foreach (var med in meds)
        {
            // Lot 1 - Yakın miat (FEFO öncelikli)
            var id1 = Guid.NewGuid();
            var lot1 = MedicationStockItem.Create(
                id1,
                PharmacyDepartmentId,
                "Merkez Eczane Deposu",
                med.Id,
                $"LOT-2026-{med.Code.Replace("DEMO-MED-", "")}-A",
                nowUtc.AddMonths(6),
                initialQuantity: 50,
                reorderLevel: 10,
                nowUtc);
            items.Add(lot1);

            transactions.Add(MedicationStockTransaction.Create(
                Guid.NewGuid(),
                id1,
                StockTransactionType.InitialReceipt,
                quantity: 50,
                previousQuantityOnHand: 0,
                newQuantityOnHand: 50,
                referenceId: "DEMO-INIT-2026.1",
                notes: "İlk demo stok devri",
                performedByUserId: null,
                performedAtUtc: nowUtc));

            // Lot 2 - Uzak miat
            var id2 = Guid.NewGuid();
            var lot2 = MedicationStockItem.Create(
                id2,
                PharmacyDepartmentId,
                "Merkez Eczane Deposu",
                med.Id,
                $"LOT-2026-{med.Code.Replace("DEMO-MED-", "")}-B",
                nowUtc.AddMonths(18),
                initialQuantity: 100,
                reorderLevel: 10,
                nowUtc);
            items.Add(lot2);

            transactions.Add(MedicationStockTransaction.Create(
                Guid.NewGuid(),
                id2,
                StockTransactionType.InitialReceipt,
                quantity: 100,
                previousQuantityOnHand: 0,
                newQuantityOnHand: 100,
                referenceId: "DEMO-INIT-2026.1",
                notes: "İlk demo stok devri",
                performedByUserId: null,
                performedAtUtc: nowUtc));
        }

        return (items, transactions);
    }

    public static IReadOnlyList<MedicationCatalogItem> GetDefaultDemoMedications(DateTime nowUtc)
    {
        return
        [
            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000601"),
                "DEMO-MED-AMX500",
                "DEMO-Amoksilin 500mg Kapsül",
                "Amoksisilin",
                MedicationForm.Capsule,
                500m,
                "mg",
                MedicationRoute.Oral,
                "J01CA04",
                "Geniş spektrumlu penisilin grubu antibiyotik.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000602"),
                "DEMO-MED-CIP500",
                "DEMO-Siprofloksasin 500mg Film Tablet",
                "Siprofloksasin",
                MedicationForm.Tablet,
                500m,
                "mg",
                MedicationRoute.Oral,
                "J01MA02",
                "Florokinolon grubu geniş spektrumlu antibakteriyel.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000603"),
                "DEMO-MED-AZI500",
                "DEMO-Azitromisin 500mg Tablet",
                "Azitromisin",
                MedicationForm.Tablet,
                500m,
                "mg",
                MedicationRoute.Oral,
                "J01FA10",
                "Makrolid grubu solunum ve doku antibiyotiği.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000604"),
                "DEMO-MED-PAR500",
                "DEMO-Parasetamol 500mg Tablet",
                "Parasetamol",
                MedicationForm.Tablet,
                500m,
                "mg",
                MedicationRoute.Oral,
                "N02BE01",
                "Hafif ve orta şiddetli ağrılarda kullanılan analjezik ve antipiretik.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000605"),
                "DEMO-MED-IBU400",
                "DEMO-İbuprofen 400mg Draje",
                "İbuprofen",
                MedicationForm.Tablet,
                400m,
                "mg",
                MedicationRoute.Oral,
                "M01AE01",
                "Non-steroid antiinflamatuvar ve analjezik.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000606"),
                "DEMO-MED-ASA100",
                "DEMO-Asetilsalisilik Asit 100mg Enterik Tablet",
                "Asetilsalisilik Asit",
                MedicationForm.Tablet,
                100m,
                "mg",
                MedicationRoute.Oral,
                "B01AC06",
                "Antiagregan ve kardiyovasküler koruma ilacı.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000607"),
                "DEMO-MED-RAM05",
                "DEMO-Ramipril 5mg Tablet",
                "Ramipril",
                MedicationForm.Tablet,
                5m,
                "mg",
                MedicationRoute.Oral,
                "C09AA05",
                "ACE inhibitörü antihipertansif ve kalp yetmezliği ilacı.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000608"),
                "DEMO-MED-AML05",
                "DEMO-Amlodipin 5mg Tablet",
                "Amlodipin",
                MedicationForm.Tablet,
                5m,
                "mg",
                MedicationRoute.Oral,
                "C08CA01",
                "Kalsiyum kanal blokeri antihipertansif.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000609"),
                "DEMO-MED-MET50",
                "DEMO-Metoprolol Suksinat 50mg Kontrollü Salım Tableti",
                "Metoprolol",
                MedicationForm.Tablet,
                50m,
                "mg",
                MedicationRoute.Oral,
                "C07AB02",
                "Kardiyoselektif beta-bloker antihipertansif ve antiaritmik.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000610"),
                "DEMO-MED-ATO20",
                "DEMO-Atorvastatin 20mg Film Tablet",
                "Atorvastatin",
                MedicationForm.Tablet,
                20m,
                "mg",
                MedicationRoute.Oral,
                "C10AA05",
                "HMG-CoA redüktaz inhibitörü kolesterol düşürücü statin.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000611"),
                "DEMO-MED-SAL100",
                "DEMO-Salbutamol 100mcg İnhalasyon Aerosolü",
                "Salbutamol",
                MedicationForm.Inhaler,
                100m,
                "mcg",
                MedicationRoute.Inhalation,
                "R03AC02",
                "Hızlı etkili beta-2 agonist bronkodilatör.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000612"),
                "DEMO-MED-PAN40",
                "DEMO-Pantoprazol 40mg Enterik Tablet",
                "Pantoprazol",
                MedicationForm.Tablet,
                40m,
                "mg",
                MedicationRoute.Oral,
                "A02BC02",
                "Proton pompası inhibitörü mide koruyucu.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000613"),
                "DEMO-MED-MET850",
                "DEMO-Metformin Hidroklorür 850mg Film Tablet",
                "Metformin",
                MedicationForm.Tablet,
                850m,
                "mg",
                MedicationRoute.Oral,
                "A10BA02",
                "Tip 2 diyabet tedavisinde kullanılan biguanid grubu antidiyabetik.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000614"),
                "DEMO-MED-INS100",
                "DEMO-İnsülin Glargin 100 IU/ml Enjeksiyonluk Çözelti",
                "İnsülin Glargin",
                MedicationForm.Injection,
                100m,
                "IU",
                MedicationRoute.Subcutaneous,
                "A10AE04",
                "Uzun etkili rekombinant DNA kökenli insan insülini analoğu.",
                DefaultCatalogVersion,
                nowUtc),

            MedicationCatalogItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000615"),
                "DEMO-MED-CET10",
                "DEMO-Setirizin 10mg Film Tablet",
                "Setirizin",
                MedicationForm.Tablet,
                10m,
                "mg",
                MedicationRoute.Oral,
                "R06AE07",
                "İkinci kuşak H1 reseptör antagonisti antialerjik antihistaminik.",
                DefaultCatalogVersion,
                nowUtc),
        ];
    }
}
