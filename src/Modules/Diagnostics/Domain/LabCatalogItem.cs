namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class LabCatalogItem
{
    private readonly List<LabCatalogParameter> _parameters = [];

    public Guid Id
    {
        get; private set;
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string SpecimenType { get; private set; } = string.Empty;
    public string ContainerType { get; private set; } = string.Empty;
    public bool IsPanel
    {
        get; private set;
    }
    public int TurnaroundMinutes
    {
        get; private set;
    }
    public bool IsActive
    {
        get; private set;
    }
    public string CatalogVersion { get; private set; } = string.Empty;
    public string? Description
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public IReadOnlyCollection<LabCatalogParameter> Parameters => _parameters.AsReadOnly();

    private LabCatalogItem()
    {
    }

    public static LabCatalogItem Create(
        Guid id,
        string code,
        string name,
        string category,
        string specimenType,
        string containerType,
        bool isPanel,
        int turnaroundMinutes,
        string catalogVersion,
        string? description,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Katalog kalem kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Katalog kodu zorunludur.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Katalog test adı zorunludur.", nameof(name));
        }

        return new LabCatalogItem
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Genel" : category.Trim(),
            SpecimenType = string.IsNullOrWhiteSpace(specimenType) ? "Venöz Tam Kan" : specimenType.Trim(),
            ContainerType = string.IsNullOrWhiteSpace(containerType) ? "Mor Kapaklı EDTA Tüp" : containerType.Trim(),
            IsPanel = isPanel,
            TurnaroundMinutes = Math.Max(1, turnaroundMinutes),
            IsActive = true,
            CatalogVersion = string.IsNullOrWhiteSpace(catalogVersion) ? "DEMO-LAB-2026.1" : catalogVersion.Trim(),
            Description = description?.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
        };
    }

    public void AddParameter(LabCatalogParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        _parameters.Add(parameter);
    }

    public void SetActive(bool isActive, DateTime nowUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = nowUtc;
    }
}
