namespace HospitalManagement.Modules.Interoperability.Domain.Mhrs;

public enum MhrsAppointmentStatus
{
    Booked = 1,
    Confirmed = 2,
    CancelledByPatient = 3,
    CancelledByDoctor = 4,
    Completed = 5,
}

public sealed record MhrsSlot(
    string SlotId,
    Guid DoctorId,
    string DoctorName,
    string ClinicCode,
    string ClinicName,
    string HospitalCode,
    DateTime SlotDateTimeUtc,
    int DurationMinutes,
    bool IsAvailable);

public sealed class MhrsAppointmentRecord
{
    private MhrsAppointmentRecord()
    {
    }

    public MhrsAppointmentRecord(
        string slotId,
        string patientNationalId,
        string patientFullName,
        Guid doctorId,
        string doctorName,
        string clinicName,
        DateTime appointmentDateTimeUtc,
        string idempotencyKey)
    {
        Id = Guid.NewGuid();
        MhrsAppointmentId = $"MHRS-APT-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        SlotId = string.IsNullOrWhiteSpace(slotId) ? $"SLOT-{Guid.NewGuid():N}"[..12] : slotId.Trim();
        PatientNationalId = string.IsNullOrWhiteSpace(patientNationalId) ? "11111111110" : patientNationalId.Trim();
        PatientFullName = string.IsNullOrWhiteSpace(patientFullName) ? "DEMO HASTA" : patientFullName.Trim();
        DoctorId = doctorId;
        DoctorName = string.IsNullOrWhiteSpace(doctorName) ? "Dr. Tabip" : doctorName.Trim();
        ClinicName = string.IsNullOrWhiteSpace(clinicName) ? "Genel Poliklinik" : clinicName.Trim();
        AppointmentDateTimeUtc = appointmentDateTimeUtc;
        Status = MhrsAppointmentStatus.Booked;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? $"IDEMP-{Guid.NewGuid():N}" : idempotencyKey.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id
    {
        get; private set;
    }
    public string MhrsAppointmentId { get; private set; } = string.Empty;
    public string SlotId { get; private set; } = string.Empty;
    public string PatientNationalId { get; private set; } = string.Empty;
    public string PatientFullName { get; private set; } = string.Empty;
    public Guid DoctorId
    {
        get; private set;
    }
    public string DoctorName { get; private set; } = string.Empty;
    public string ClinicName { get; private set; } = string.Empty;
    public DateTime AppointmentDateTimeUtc
    {
        get; private set;
    }
    public MhrsAppointmentStatus Status
    {
        get; private set;
    }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? CancellationReason
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    public void Cancel(string reason, bool isDoctor = false)
    {
        Status = isDoctor ? MhrsAppointmentStatus.CancelledByDoctor : MhrsAppointmentStatus.CancelledByPatient;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Belirtilmedi" : reason.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Confirm()
    {
        Status = MhrsAppointmentStatus.Confirmed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = MhrsAppointmentStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
