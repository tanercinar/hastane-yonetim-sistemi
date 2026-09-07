namespace HospitalManagement.Contracts.Pharmacy;

public sealed record MedicationCatalogItemImportRequest
{
    public string Code { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string Form { get; init; } = string.Empty;
    public decimal StrengthValue
    {
        get; init;
    }
    public string StrengthUnit { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? AtcCode
    {
        get; init;
    }
    public string? Description
    {
        get; init;
    }
}

public sealed record ImportMedicationCatalogRequest
{
    public string CatalogVersion { get; init; } = string.Empty;
    public List<MedicationCatalogItemImportRequest> Items { get; init; } = [];
}
