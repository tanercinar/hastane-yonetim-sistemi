namespace HospitalManagement.Modules.Scheduling.Domain;

public sealed class DoctorLeaveBlock
{
    private DoctorLeaveBlock()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid DoctorId
    {
        get; private set;
    }

    public DateTime StartUtc
    {
        get; private set;
    }

    public DateTime EndUtc
    {
        get; private set;
    }

    public string Reason { get; private set; } = string.Empty;

    public bool IsActive
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static DoctorLeaveBlock Create(
        Guid id,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        string reason,
        DateTime createdAtUtc)
    {
        if (startUtc >= endUtc)
        {
            throw new ArgumentException("İzin/blok başlangıç zamanı bitiş zamanından önce olmalıdır.", nameof(startUtc));
        }

        return new DoctorLeaveBlock
        {
            Id = id,
            DoctorId = doctorId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Reason = string.IsNullOrWhiteSpace(reason) ? "İzin" : reason.Trim(),
            IsActive = true,
            CreatedAtUtc = createdAtUtc,
        };
    }

    public void Cancel(DateTime updatedAtUtc)
    {
        IsActive = false;
    }
}
