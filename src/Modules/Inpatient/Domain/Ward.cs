namespace HospitalManagement.Modules.Inpatient.Domain;

public sealed class Ward
{
    private readonly List<Room> _rooms = [];

    public Guid Id
    {
        get; private set;
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid DepartmentId
    {
        get; private set;
    }
    public string Building { get; private set; } = string.Empty;
    public string Floor { get; private set; } = string.Empty;
    public WardType WardType
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
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private Ward()
    {
    }

    public static Ward Create(
        Guid id,
        string code,
        string name,
        Guid departmentId,
        string building,
        string floor,
        WardType wardType,
        DateTime nowUtc,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Servis kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Servis kodu zorunludur.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Servis adı zorunludur.", nameof(name));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği zorunludur.", nameof(departmentId));
        }

        return new Ward
        {
            Id = id,
            Code = code.Trim(),
            Name = name.Trim(),
            DepartmentId = departmentId,
            Building = string.IsNullOrWhiteSpace(building) ? "Ana Bina" : building.Trim(),
            Floor = string.IsNullOrWhiteSpace(floor) ? "1. Kat" : floor.Trim(),
            WardType = wardType,
            IsActive = isActive,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
        };
    }

    public void UpdateDetails(string name, string building, string floor, WardType wardType, bool isActive, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Servis adı zorunludur.", nameof(name));
        }

        Name = name.Trim();
        Building = string.IsNullOrWhiteSpace(building) ? Building : building.Trim();
        Floor = string.IsNullOrWhiteSpace(floor) ? Floor : floor.Trim();
        WardType = wardType;
        IsActive = isActive;
        UpdatedAtUtc = nowUtc;
    }
}
