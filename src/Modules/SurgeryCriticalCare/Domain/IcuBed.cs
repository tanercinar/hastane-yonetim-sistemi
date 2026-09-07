namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class IcuBed
{
    private IcuBed()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string BedCode { get; private set; } = string.Empty;
    public string BedName { get; private set; } = string.Empty;
    public string UnitName { get; private set; } = "Genel Yoğun Bakım Ünitesi";
    public bool IsActive
    {
        get; private set;
    }
    public uint Version
    {
        get; internal set;
    }

    public static IcuBed Create(
        Guid id,
        string bedCode,
        string bedName,
        string unitName = "Genel Yoğun Bakım Ünitesi")
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Yatak ID boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(bedCode))
        {
            throw new ArgumentException("Yatak kodu boş olamaz.", nameof(bedCode));
        }

        if (string.IsNullOrWhiteSpace(bedName))
        {
            throw new ArgumentException("Yatak adı boş olamaz.", nameof(bedName));
        }

        return new IcuBed
        {
            Id = id,
            BedCode = bedCode.Trim().ToUpperInvariant(),
            BedName = bedName.Trim(),
            UnitName = unitName.Trim(),
            IsActive = true,
        };
    }
}
