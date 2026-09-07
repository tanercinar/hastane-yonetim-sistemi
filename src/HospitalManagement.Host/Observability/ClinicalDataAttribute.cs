using Microsoft.Extensions.Compliance.Classification;

namespace HospitalManagement.Host.Observability;

internal static class HospitalDataClassifications
{
    private const string TaxonomyName = "HospitalManagement";

    internal static DataClassification ClinicalContent =>
        new(TaxonomyName, nameof(ClinicalContent));
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
internal sealed class ClinicalDataAttribute : DataClassificationAttribute
{
    public ClinicalDataAttribute()
        : base(HospitalDataClassifications.ClinicalContent)
    {
    }
}
