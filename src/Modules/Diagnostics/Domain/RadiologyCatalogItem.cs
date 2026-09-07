namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class RadiologyCatalogItem
{
    public Guid Id
    {
        get; private set;
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public RadiologyModality Modality
    {
        get; private set;
    }
    public string BodySite { get; private set; } = string.Empty;
    public string? Description
    {
        get; private set;
    }
    public string? PreparationInstructions
    {
        get; private set;
    }
    public bool ContrastRequired
    {
        get; private set;
    }
    public int EstimatedDurationMinutes
    {
        get; private set;
    }
    public bool IsActive
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    private RadiologyCatalogItem()
    {
    }

    public static RadiologyCatalogItem Create(
        Guid id,
        string code,
        string name,
        RadiologyModality modality,
        string bodySite,
        string? description,
        string? preparationInstructions,
        bool contrastRequired,
        int estimatedDurationMinutes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Katalog öğesi kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Katalog kodu zorunludur.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("İşlem adı zorunludur.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(bodySite))
        {
            throw new ArgumentException("Vücut bölgesi zorunludur.", nameof(bodySite));
        }

        return new RadiologyCatalogItem
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Modality = modality,
            BodySite = bodySite.Trim(),
            Description = description?.Trim(),
            PreparationInstructions = preparationInstructions?.Trim(),
            ContrastRequired = contrastRequired,
            EstimatedDurationMinutes = estimatedDurationMinutes > 0 ? estimatedDurationMinutes : 15,
            IsActive = true,
            CreatedAtUtc = nowUtc,
        };
    }
}
