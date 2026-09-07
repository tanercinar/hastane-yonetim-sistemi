namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class DiagnosticOrderItem
{
    public Guid Id
    {
        get; private set;
    }
    public Guid DiagnosticOrderId
    {
        get; private set;
    }
    public string CatalogCode { get; private set; } = string.Empty;
    public string CatalogItemName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public DiagnosticOrderItemStatus Status
    {
        get; private set;
    }
    public string? SpecialInstructions
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

    private DiagnosticOrderItem()
    {
    }

    public static DiagnosticOrderItem Create(
        Guid id,
        Guid diagnosticOrderId,
        string catalogCode,
        string catalogItemName,
        string category,
        string? specialInstructions,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Kalem kimliği zorunludur.", nameof(id));
        }

        if (diagnosticOrderId == Guid.Empty)
        {
            throw new ArgumentException("İstem kimliği zorunludur.", nameof(diagnosticOrderId));
        }

        if (string.IsNullOrWhiteSpace(catalogCode))
        {
            throw new ArgumentException("Katalog kodu zorunludur.", nameof(catalogCode));
        }

        if (string.IsNullOrWhiteSpace(catalogItemName))
        {
            throw new ArgumentException("Katalog kalem adı zorunludur.", nameof(catalogItemName));
        }

        return new DiagnosticOrderItem
        {
            Id = id,
            DiagnosticOrderId = diagnosticOrderId,
            CatalogCode = catalogCode.Trim(),
            CatalogItemName = catalogItemName.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            Status = DiagnosticOrderItemStatus.Pending,
            SpecialInstructions = specialInstructions?.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
        };
    }

    public void UpdateStatus(DiagnosticOrderItemStatus newStatus, DateTime nowUtc)
    {
        Status = newStatus;
        UpdatedAtUtc = nowUtc;
    }
}
