namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class DiagnosisCatalogItem
{
    private DiagnosisCatalogItem()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public string Code { get; private set; } = string.Empty;

    public string NameTurkish { get; private set; } = string.Empty;

    public string NameEnglish { get; private set; } = string.Empty;

    public string Chapter { get; private set; } = string.Empty;

    public string Block { get; private set; } = string.Empty;

    public string CatalogVersion { get; private set; } = "ICD-10-TR-2026.1";

    public bool IsActive
    {
        get; private set;
    }

    public static DiagnosisCatalogItem Create(
        Guid id,
        string code,
        string nameTurkish,
        string nameEnglish,
        string chapter,
        string block,
        string catalogVersion = "ICD-10-TR-2026.1",
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Katalog öğesi kimliği boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Tanı kodu boş olamaz.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameTurkish))
        {
            throw new ArgumentException("Türkçe tanı adı boş olamaz.", nameof(nameTurkish));
        }

        return new DiagnosisCatalogItem
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            NameTurkish = nameTurkish.Trim(),
            NameEnglish = string.IsNullOrWhiteSpace(nameEnglish) ? nameTurkish.Trim() : nameEnglish.Trim(),
            Chapter = string.IsNullOrWhiteSpace(chapter) ? "Genel" : chapter.Trim(),
            Block = string.IsNullOrWhiteSpace(block) ? "Genel" : block.Trim(),
            CatalogVersion = string.IsNullOrWhiteSpace(catalogVersion) ? "ICD-10-TR-2026.1" : catalogVersion.Trim(),
            IsActive = isActive,
        };
    }
}
