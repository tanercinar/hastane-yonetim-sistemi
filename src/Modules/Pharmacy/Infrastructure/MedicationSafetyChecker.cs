using System.Globalization;

using HospitalManagement.BuildingBlocks.Clinical;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public sealed class MedicationSafetyChecker : IMedicationSafetyChecker
{
    private const string RegulatoryDisclaimer =
        "DİKKAT: Bu güvenlik uyarıları sentetik DEMO verilerle kural tabanlı deterministik olarak üretilmiştir. " +
        "Sertifikalı bir klinik karar destek sistemi (CDSS) veya yapay zeka niteliğinde değildir.";

    private readonly PharmacyDbContext _dbContext;
    private readonly IPatientAllergyLookup? _allergyLookup;

    public MedicationSafetyChecker(
        PharmacyDbContext dbContext,
        IPatientAllergyLookup? allergyLookup = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _allergyLookup = allergyLookup;
    }

    public async Task<MedicationSafetyCheckResult> CheckSafetyAsync(
        Guid patientId,
        IReadOnlyList<PrescriptionItemSafetyCandidate> items,
        Guid? currentPrescriptionId = null,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<MedicationSafetyWarning>();

        if (items is null || items.Count == 0)
        {
            return new MedicationSafetyCheckResult(
                HasWarnings: false,
                HasCriticalWarnings: false,
                HasModerateWarnings: false,
                Warnings: warnings,
                Disclaimer: RegulatoryDisclaimer);
        }

        var catalogItemIds = items.Select(i => i.MedicationCatalogItemId).Distinct().ToList();
        var catalogMap = await _dbContext.MedicationCatalogItems
            .AsNoTracking()
            .Where(m => catalogItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var candidateDetails = items
            .Where(i => catalogMap.ContainsKey(i.MedicationCatalogItemId))
            .Select(i => (Candidate: i, Catalog: catalogMap[i.MedicationCatalogItemId]))
            .ToList();

        // 1. Kural: Demo Alerji Çapraz Kontrolü
        if (_allergyLookup is not null && patientId != Guid.Empty)
        {
            try
            {
                var patientAllergies = await _allergyLookup.GetActiveAllergiesAsync(patientId, cancellationToken);
                if (patientAllergies is { Count: > 0 })
                {
                    foreach (var (_, cat) in candidateDetails)
                    {
                        foreach (var allergy in patientAllergies)
                        {
                            if (IsAllergyMatch(allergy.Allergen, cat.GenericName, cat.BrandName, cat.AtcCode))
                            {
                                warnings.Add(new MedicationSafetyWarning(
                                    WarningCode: $"DEMO-WARN-ALLERGY-{cat.Code}",
                                    Type: SafetyWarningType.AllergyCrossReaction,
                                    Severity: SafetyWarningSeverity.Critical,
                                    Title: $"Alerji Çapraz Reaksiyon Uyarısı: {allergy.Allergen}",
                                    Message: $"Hastanın aktif '{allergy.Allergen}' alerji kaydı bulunmaktadır. Reçete edilmek istenen '{cat.BrandName}' ({cat.GenericName}) ilacı bu alerjenle çapraz reaksiyon veya doğrudan etkileşim riski taşır.",
                                    OffendingMedicationName: cat.BrandName,
                                    ConflictingItemName: allergy.Allergen,
                                    RequiresOverrideReason: true));
                            }
                        }
                    }
                }
            }
            catch
            {
                warnings.Add(new MedicationSafetyWarning(
                    WarningCode: "DEMO-WARN-ALLERGY-CHECK-UNAVAILABLE",
                    Type: SafetyWarningType.AllergyCrossReaction,
                    Severity: SafetyWarningSeverity.Critical,
                    Title: "Alerji kontrolü tamamlanamadı",
                    Message: "Aktif alerji kayıtları doğrulanamadı. İmzalamadan önce kayıtları yeniden kontrol ediniz.",
                    OffendingMedicationName: null,
                    ConflictingItemName: null,
                    RequiresOverrideReason: true));
            }
        }

        // 2. Kural: Yinelenen Etken Madde (Duplicate Therapy)
        // 2a. Reçete içi duplikasyon
        var genericGroups = candidateDetails
            .GroupBy(c => NormalizeText(c.Catalog.GenericName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in genericGroups)
        {
            var first = group.First();
            warnings.Add(new MedicationSafetyWarning(
                WarningCode: "DEMO-WARN-DUP-INTRA",
                Type: SafetyWarningType.DuplicateTherapy,
                Severity: SafetyWarningSeverity.Critical,
                Title: $"Mükerrer Etken Madde Uyarısı: {first.Catalog.GenericName}",
                Message: $"Aynı reçetede '{first.Catalog.GenericName}' etken maddesini içeren birden fazla ({group.Count()} adet) ilaç bulunmaktadır. Toksisite ve aşırı doz riskine karşı kontrol ediniz.",
                OffendingMedicationName: first.Catalog.BrandName,
                ConflictingItemName: string.Join(", ", group.Select(g => g.Catalog.BrandName)),
                RequiresOverrideReason: true));
        }

        // 2b. Hastanın diğer aktif imzalı reçeteleriyle duplikasyon
        if (patientId != Guid.Empty)
        {
            var activePrescriptions = await _dbContext.Prescriptions
                .Include(p => p.Items)
                .AsNoTracking()
                .Where(p => p.PatientId == patientId &&
                            p.Id != currentPrescriptionId &&
                            (p.Status == PrescriptionStatus.Signed || p.Status == PrescriptionStatus.PartiallyDispensed))
                .ToListAsync(cancellationToken);

            var activePrescriptionCatalogIds = activePrescriptions
                .SelectMany(p => p.Items)
                .Select(i => i.MedicationCatalogItemId)
                .Distinct()
                .ToList();

            if (activePrescriptionCatalogIds.Count > 0)
            {
                var activeCatalogMap = await _dbContext.MedicationCatalogItems
                    .AsNoTracking()
                    .Where(m => activePrescriptionCatalogIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, cancellationToken);

                foreach (var (_, cat) in candidateDetails)
                {
                    var catNorm = NormalizeText(cat.GenericName);
                    foreach (var activeRx in activePrescriptions)
                    {
                        foreach (var item in activeRx.Items)
                        {
                            if (activeCatalogMap.TryGetValue(item.MedicationCatalogItemId, out var activeCat))
                            {
                                var activeCatNorm = NormalizeText(activeCat.GenericName);
                                if (catNorm == activeCatNorm)
                                {
                                    warnings.Add(new MedicationSafetyWarning(
                                        WarningCode: $"DEMO-WARN-DUP-ACTIVE-{cat.Code}",
                                        Type: SafetyWarningType.DuplicateTherapy,
                                        Severity: SafetyWarningSeverity.Moderate,
                                        Title: $"Devam Eden Tedavide Mükerrer Etken Madde: {cat.GenericName}",
                                        Message: $"Hastanın aktif reçetesinde ({activeRx.PrescriptionNumber}) zaten '{activeCat.BrandName}' ({activeCat.GenericName}) bulunmaktadır.",
                                        OffendingMedicationName: cat.BrandName,
                                        ConflictingItemName: $"{activeCat.BrandName} ({activeRx.PrescriptionNumber})",
                                        RequiresOverrideReason: true));
                                }
                            }
                        }
                    }
                }
            }
        }

        // 3. Kural: Basit İlaç-İlaç Etkileşim Matrisi (Drug Interaction Matrix)
        for (var i = 0; i < candidateDetails.Count; i++)
        {
            for (var j = i + 1; j < candidateDetails.Count; j++)
            {
                var medA = candidateDetails[i].Catalog;
                var medB = candidateDetails[j].Catalog;

                var interaction = CheckInteraction(medA.GenericName, medB.GenericName);
                if (interaction is not null)
                {
                    warnings.Add(new MedicationSafetyWarning(
                        WarningCode: interaction.WarningCode,
                        Type: SafetyWarningType.DrugInteraction,
                        Severity: interaction.Severity,
                        Title: $"İlaç Etkileşimi: {medA.BrandName} + {medB.BrandName}",
                        Message: interaction.Message,
                        OffendingMedicationName: medA.BrandName,
                        ConflictingItemName: medB.BrandName,
                        RequiresOverrideReason: interaction.Severity >= SafetyWarningSeverity.Moderate));
                }
            }
        }

        var hasCritical = warnings.Any(w => w.Severity == SafetyWarningSeverity.Critical);
        var hasModerate = warnings.Any(w => w.Severity == SafetyWarningSeverity.Moderate);

        return new MedicationSafetyCheckResult(
            HasWarnings: warnings.Count > 0,
            HasCriticalWarnings: hasCritical,
            HasModerateWarnings: hasModerate,
            Warnings: warnings,
            Disclaimer: RegulatoryDisclaimer);
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Trim()
            .Replace('İ', 'i')
            .Replace('I', 'i')
            .Replace('ı', 'i')
            .Replace('ş', 's')
            .Replace('Ş', 's')
            .Replace('ğ', 'g')
            .Replace('Ğ', 'g')
            .Replace('ü', 'u')
            .Replace('Ü', 'u')
            .Replace('ö', 'o')
            .Replace('Ö', 'o')
            .ToLowerInvariant();
    }

    private static bool IsAllergyMatch(string allergen, string genericName, string brandName, string? atcCode)
    {
        var a = NormalizeText(allergen);
        var g = NormalizeText(genericName);
        var b = NormalizeText(brandName);

        // Penisilin / Amoksisilin grubu
        if ((a.Contains("penisilin", StringComparison.Ordinal) || a.Contains("penicillin", StringComparison.Ordinal) || a.Contains("beta-laktam", StringComparison.Ordinal)) &&
            (g.Contains("amoksisilin", StringComparison.Ordinal) || g.Contains("ampisilin", StringComparison.Ordinal) || b.Contains("amoksilin", StringComparison.Ordinal)))
        {
            return true;
        }

        // NSAİİ / Aspirin / İbuprofen grubu
        if ((a.Contains("aspirin", StringComparison.Ordinal) || a.Contains("nsai", StringComparison.Ordinal) || a.Contains("nsaid", StringComparison.Ordinal) || a.Contains("ibuprofen", StringComparison.Ordinal)) &&
            (g.Contains("aspirin", StringComparison.Ordinal) || g.Contains("asetilsalisilik", StringComparison.Ordinal) || g.Contains("ibuprofen", StringComparison.Ordinal) || g.Contains("diklofenak", StringComparison.Ordinal)))
        {
            return true;
        }

        // Kinolon / Siprofloksasin grubu
        if ((a.Contains("kinolon", StringComparison.Ordinal) || a.Contains("quinolone", StringComparison.Ordinal) || a.Contains("siprofloksasin", StringComparison.Ordinal)) &&
            (g.Contains("siprofloksasin", StringComparison.Ordinal) || g.Contains("levofloksasin", StringComparison.Ordinal)))
        {
            return true;
        }

        // Parasetamol
        if ((a.Contains("parasetamol", StringComparison.Ordinal) || a.Contains("asetaminofen", StringComparison.Ordinal)) &&
            (g.Contains("parasetamol", StringComparison.Ordinal) || b.Contains("parasetamol", StringComparison.Ordinal)))
        {
            return true;
        }

        // Genel ad eşleşmesi
        return g.Contains(a, StringComparison.Ordinal) || a.Contains(g, StringComparison.Ordinal);
    }

    private static SyntheticInteractionDef? CheckInteraction(string genericA, string genericB)
    {
        var g1 = NormalizeText(genericA);
        var g2 = NormalizeText(genericB);

        var isG1Asp = g1.Contains("aspirin", StringComparison.Ordinal) || g1.Contains("asetilsalisilik", StringComparison.Ordinal);
        var isG2Asp = g2.Contains("aspirin", StringComparison.Ordinal) || g2.Contains("asetilsalisilik", StringComparison.Ordinal);
        var isG1Ibu = g1.Contains("ibuprofen", StringComparison.Ordinal);
        var isG2Ibu = g2.Contains("ibuprofen", StringComparison.Ordinal);

        // Aspirin + İbuprofen (veya 2 NSAİİ)
        if ((isG1Asp && isG2Ibu) || (isG2Asp && isG1Ibu))
        {
            return new SyntheticInteractionDef(
                "DEMO-INT-ASP-IBU",
                SafetyWarningSeverity.Critical,
                "Eşzamanlı iki farklı NSAİİ (Aspirin ve İbuprofen) kullanımı gastrointestinal ülserasyon, kanama ve nefrotoksisite riskini katlar.");
        }

        // Amoksisilin + Metotreksat
        if (g1.Contains("amoksisilin", StringComparison.Ordinal) && g2.Contains("metotreksat", StringComparison.Ordinal) ||
            g2.Contains("amoksisilin", StringComparison.Ordinal) && g1.Contains("metotreksat", StringComparison.Ordinal))
        {
            return new SyntheticInteractionDef(
                "DEMO-INT-AMX-MTX",
                SafetyWarningSeverity.Moderate,
                "Penisilin türevi antibiyotikler metotreksatın böbrek klirensini azaltarak kemik iliği baskılanması ve toksisite riskini artırabilir.");
        }

        // Siprofloksasin + Teofilin
        if (g1.Contains("siprofloksasin", StringComparison.Ordinal) && g2.Contains("teofilin", StringComparison.Ordinal) ||
            g2.Contains("siprofloksasin", StringComparison.Ordinal) && g1.Contains("teofilin", StringComparison.Ordinal))
        {
            return new SyntheticInteractionDef(
                "DEMO-INT-CIP-THEO",
                SafetyWarningSeverity.Critical,
                "Siprofloksasin teofilin metabolizmasını inhibe ederek serum teofilin düzeyini ve kardiyak/nörolojik toksisite riskini belirgin artırır.");
        }

        // Klaritromisin + Atorvastatin
        if (g1.Contains("klaritromisin", StringComparison.Ordinal) && g2.Contains("atorvastatin", StringComparison.Ordinal) ||
            g2.Contains("klaritromisin", StringComparison.Ordinal) && g1.Contains("atorvastatin", StringComparison.Ordinal))
        {
            return new SyntheticInteractionDef(
                "DEMO-INT-CLA-ATOR",
                SafetyWarningSeverity.Critical,
                "Klaritromisin CYP3A4 enzimini güçlü bir şekilde inhibe ederek atorvastatin plazma konsantrasyonunu ve rabdomiyoliz riskini artırır.");
        }

        // Enalapril + Spironolakton
        if (g1.Contains("enalapril", StringComparison.Ordinal) && g2.Contains("spironolakton", StringComparison.Ordinal) ||
            g2.Contains("enalapril", StringComparison.Ordinal) && g1.Contains("spironolakton", StringComparison.Ordinal))
        {
            return new SyntheticInteractionDef(
                "DEMO-INT-ENA-SPRO",
                SafetyWarningSeverity.Moderate,
                "ACE inhibitörü ve potasyum tutucu diüretik kombinasyonu ciddi hiperkalemi riski doğurur. Serum potasyum seviyesi izlenmelidir.");
        }

        return null;
    }

    private sealed record SyntheticInteractionDef(string WarningCode, SafetyWarningSeverity Severity, string Message);
}
