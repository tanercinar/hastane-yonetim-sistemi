namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class OperatingRoom
{
    private OperatingRoom()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string RoomCode { get; private set; } = string.Empty;
    public string RoomName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int Capacity { get; private set; } = 1;
    public Guid? SpecialtyDepartmentId
    {
        get; private set;
    }
    public uint Version
    {
        get; internal set;
    }

    public static OperatingRoom Create(
        Guid id,
        string roomCode,
        string roomName,
        int capacity = 1,
        Guid? specialtyDepartmentId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Ameliyathane ID boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(roomCode))
        {
            throw new ArgumentException("Ameliyathane salon kodu boş olamaz.", nameof(roomCode));
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            throw new ArgumentException("Ameliyathane salon adı boş olamaz.", nameof(roomName));
        }

        return new OperatingRoom
        {
            Id = id,
            RoomCode = roomCode.Trim().ToUpperInvariant(),
            RoomName = roomName.Trim(),
            Capacity = capacity > 0 ? capacity : 1,
            SpecialtyDepartmentId = specialtyDepartmentId,
            IsActive = true,
        };
    }

    public void UpdateStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
