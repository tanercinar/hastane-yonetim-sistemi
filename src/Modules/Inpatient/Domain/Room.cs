namespace HospitalManagement.Modules.Inpatient.Domain;

public sealed class Room
{
    private readonly List<Bed> _beds = [];

    public Guid Id
    {
        get; private set;
    }
    public Guid WardId
    {
        get; private set;
    }
    public string RoomNumber { get; private set; } = string.Empty;
    public BedPlacementGender GenderConstraint
    {
        get; private set;
    }
    public IsolationType IsolationType
    {
        get; private set;
    }
    public bool IsNegativePressure
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

    public IReadOnlyCollection<Bed> Beds => _beds.AsReadOnly();

    private Room()
    {
    }

    public static Room Create(
        Guid id,
        Guid wardId,
        string roomNumber,
        BedPlacementGender genderConstraint,
        IsolationType isolationType,
        bool isNegativePressure,
        DateTime nowUtc,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Oda kimliği zorunludur.", nameof(id));
        }

        if (wardId == Guid.Empty)
        {
            throw new ArgumentException("Servis kimliği zorunludur.", nameof(wardId));
        }

        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            throw new ArgumentException("Oda numarası zorunludur.", nameof(roomNumber));
        }

        return new Room
        {
            Id = id,
            WardId = wardId,
            RoomNumber = roomNumber.Trim(),
            GenderConstraint = genderConstraint,
            IsolationType = isolationType,
            IsNegativePressure = isNegativePressure,
            IsActive = isActive,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
        };
    }

    public void UpdateRoomDetails(
        BedPlacementGender genderConstraint,
        IsolationType isolationType,
        bool isNegativePressure,
        bool isActive,
        DateTime nowUtc)
    {
        GenderConstraint = genderConstraint;
        IsolationType = isolationType;
        IsNegativePressure = isNegativePressure;
        IsActive = isActive;
        UpdatedAtUtc = nowUtc;
    }
}
