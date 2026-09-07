namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class LabCatalogParameter
{
    public Guid Id
    {
        get; private set;
    }
    public Guid LabCatalogItemId
    {
        get; private set;
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal? ReferenceRangeLow
    {
        get; private set;
    }
    public decimal? ReferenceRangeHigh
    {
        get; private set;
    }
    public decimal? CriticalLow
    {
        get; private set;
    }
    public decimal? CriticalHigh
    {
        get; private set;
    }
    public string ValueType { get; private set; } = "Numeric";
    public int SortOrder
    {
        get; private set;
    }

    private LabCatalogParameter()
    {
    }

    public static LabCatalogParameter Create(
        Guid id,
        Guid labCatalogItemId,
        string code,
        string name,
        string unit,
        decimal? referenceRangeLow,
        decimal? referenceRangeHigh,
        decimal? criticalLow,
        decimal? criticalHigh,
        string valueType = "Numeric",
        int sortOrder = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Parametre kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Parametre kodu zorunludur.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parametre adı zorunludur.", nameof(name));
        }

        return new LabCatalogParameter
        {
            Id = id,
            LabCatalogItemId = labCatalogItemId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Unit = unit?.Trim() ?? string.Empty,
            ReferenceRangeLow = referenceRangeLow,
            ReferenceRangeHigh = referenceRangeHigh,
            CriticalLow = criticalLow,
            CriticalHigh = criticalHigh,
            ValueType = string.IsNullOrWhiteSpace(valueType) ? "Numeric" : valueType.Trim(),
            SortOrder = sortOrder,
        };
    }
}
