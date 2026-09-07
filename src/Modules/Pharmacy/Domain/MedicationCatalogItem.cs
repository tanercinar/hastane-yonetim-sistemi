namespace HospitalManagement.Modules.Pharmacy.Domain;

public sealed class MedicationCatalogItem
{
    private MedicationCatalogItem()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string Code { get; private set; } = string.Empty;
    public string BrandName { get; private set; } = string.Empty;
    public string GenericName { get; private set; } = string.Empty;
    public MedicationForm Form
    {
        get; private set;
    }
    public decimal StrengthValue
    {
        get; private set;
    }
    public string StrengthUnit { get; private set; } = string.Empty;
    public MedicationRoute Route
    {
        get; private set;
    }
    public string? AtcCode
    {
        get; private set;
    }
    public string? Description
    {
        get; private set;
    }
    public string CatalogVersion { get; private set; } = string.Empty;
    public bool IsActive
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

    public static MedicationCatalogItem Create(
        Guid id,
        string code,
        string brandName,
        string genericName,
        MedicationForm form,
        decimal strengthValue,
        string strengthUnit,
        MedicationRoute route,
        string? atcCode,
        string? description,
        string catalogVersion,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("İlaç kimliği boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("İlaç kodu boş olamaz.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(brandName))
        {
            throw new ArgumentException("İlaç ticari adı boş olamaz.", nameof(brandName));
        }

        if (string.IsNullOrWhiteSpace(genericName))
        {
            throw new ArgumentException("İlaç etken madde adı boş olamaz.", nameof(genericName));
        }

        if (strengthValue <= 0)
        {
            throw new ArgumentException("İlaç etken madde dozu / gücü 0'dan büyük olmalıdır.", nameof(strengthValue));
        }

        if (string.IsNullOrWhiteSpace(strengthUnit))
        {
            throw new ArgumentException("Doz birimi boş olamaz.", nameof(strengthUnit));
        }

        if (string.IsNullOrWhiteSpace(catalogVersion))
        {
            throw new ArgumentException("Katalog sürümü boş olamaz.", nameof(catalogVersion));
        }

        return new MedicationCatalogItem
        {
            Id = id,
            Code = code.Trim(),
            BrandName = brandName.Trim(),
            GenericName = genericName.Trim(),
            Form = form,
            StrengthValue = strengthValue,
            StrengthUnit = strengthUnit.Trim(),
            Route = route,
            AtcCode = string.IsNullOrWhiteSpace(atcCode) ? null : atcCode.Trim().ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CatalogVersion = catalogVersion.Trim(),
            IsActive = true,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = null,
        };
    }

    public void Update(
        string brandName,
        string genericName,
        MedicationForm form,
        decimal strengthValue,
        string strengthUnit,
        MedicationRoute route,
        string? atcCode,
        string? description,
        string catalogVersion,
        DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            throw new ArgumentException("İlaç ticari adı boş olamaz.", nameof(brandName));
        }

        if (string.IsNullOrWhiteSpace(genericName))
        {
            throw new ArgumentException("İlaç etken madde adı boş olamaz.", nameof(genericName));
        }

        if (strengthValue <= 0)
        {
            throw new ArgumentException("İlaç etken madde dozu / gücü 0'dan büyük olmalıdır.", nameof(strengthValue));
        }

        if (string.IsNullOrWhiteSpace(strengthUnit))
        {
            throw new ArgumentException("Doz birimi boş olamaz.", nameof(strengthUnit));
        }

        if (string.IsNullOrWhiteSpace(catalogVersion))
        {
            throw new ArgumentException("Katalog sürümü boş olamaz.", nameof(catalogVersion));
        }

        BrandName = brandName.Trim();
        GenericName = genericName.Trim();
        Form = form;
        StrengthValue = strengthValue;
        StrengthUnit = strengthUnit.Trim();
        Route = route;
        AtcCode = string.IsNullOrWhiteSpace(atcCode) ? null : atcCode.Trim().ToUpperInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CatalogVersion = catalogVersion.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTime updatedAtUtc)
    {
        IsActive = true;
        UpdatedAtUtc = updatedAtUtc;
    }
}
