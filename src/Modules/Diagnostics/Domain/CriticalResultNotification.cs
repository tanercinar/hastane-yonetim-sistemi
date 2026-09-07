namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class CriticalResultNotification
{
    public Guid Id
    {
        get; private set;
    }
    public Guid LabResultId
    {
        get; private set;
    }
    public Guid DiagnosticOrderId
    {
        get; private set;
    }
    public Guid DiagnosticOrderItemId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public string ParameterCode { get; private set; } = string.Empty;
    public string ParameterName { get; private set; } = string.Empty;
    public decimal? NumericValue
    {
        get; private set;
    }
    public string? StringValue
    {
        get; private set;
    }
    public string? Unit
    {
        get; private set;
    }
    public LabResultInterpretation Flag
    {
        get; private set;
    }
    public CriticalNotificationStatus Status
    {
        get; private set;
    }
    public int EscalationLevel { get; private set; } = 1;
    public Guid? ResponsibleDoctorUserId
    {
        get; private set;
    }
    public Guid? AcknowledgedByUserId
    {
        get; private set;
    }
    public DateTime? AcknowledgedAtUtc
    {
        get; private set;
    }
    public string? AcknowledgmentNotes
    {
        get; private set;
    }
    public DateTime? EscalatedAtUtc
    {
        get; private set;
    }
    public string? EscalationReason
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
    public int Version { get; private set; } = 1;

    private CriticalResultNotification()
    {
    }

    public static CriticalResultNotification Create(
        Guid id,
        Guid labResultId,
        Guid diagnosticOrderId,
        Guid diagnosticOrderItemId,
        Guid patientId,
        string parameterCode,
        string parameterName,
        decimal? numericValue,
        string? stringValue,
        string? unit,
        LabResultInterpretation flag,
        Guid? responsibleDoctorUserId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Bildirim kimliği zorunludur.", nameof(id));
        }

        if (labResultId == Guid.Empty)
        {
            throw new ArgumentException("Laboratuvar sonuç kimliği zorunludur.", nameof(labResultId));
        }

        if (diagnosticOrderId == Guid.Empty)
        {
            throw new ArgumentException("İstem kimliği zorunludur.", nameof(diagnosticOrderId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(parameterCode))
        {
            throw new ArgumentException("Parametre kodu zorunludur.", nameof(parameterCode));
        }

        if (string.IsNullOrWhiteSpace(parameterName))
        {
            throw new ArgumentException("Parametre adı zorunludur.", nameof(parameterName));
        }

        return new CriticalResultNotification
        {
            Id = id,
            LabResultId = labResultId,
            DiagnosticOrderId = diagnosticOrderId,
            DiagnosticOrderItemId = diagnosticOrderItemId,
            PatientId = patientId,
            ParameterCode = parameterCode.Trim().ToUpperInvariant(),
            ParameterName = parameterName.Trim(),
            NumericValue = numericValue,
            StringValue = stringValue?.Trim(),
            Unit = unit?.Trim(),
            Flag = flag,
            Status = CriticalNotificationStatus.Active,
            EscalationLevel = 1,
            ResponsibleDoctorUserId = responsibleDoctorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
            Version = 1,
        };
    }

    public void Acknowledge(Guid actorUserId, string notes, DateTime nowUtc)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Onaylayan kullanıcı kimliği zorunludur.", nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new ArgumentException("Kritik değer teslim teyidi ve alındı notu zorunludur.", nameof(notes));
        }

        if (Status == CriticalNotificationStatus.Acknowledged || Status == CriticalNotificationStatus.Closed)
        {
            throw new InvalidOperationException("Kritik sonuç bildirimi zaten onaylanmış veya kapatılmış.");
        }

        Status = CriticalNotificationStatus.Acknowledged;
        AcknowledgedByUserId = actorUserId;
        AcknowledgedAtUtc = nowUtc;
        AcknowledgmentNotes = notes.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Escalate(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Eskalasyon gerekçesi zorunludur.", nameof(reason));
        }

        if (Status == CriticalNotificationStatus.Acknowledged || Status == CriticalNotificationStatus.Closed)
        {
            throw new InvalidOperationException("Onaylanmış veya kapatılmış bir bildirim eskale edilemez.");
        }

        Status = CriticalNotificationStatus.Escalated;
        EscalationLevel++;
        EscalatedAtUtc = nowUtc;
        EscalationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Close(Guid actorUserId, string? notes, DateTime nowUtc)
    {
        Status = CriticalNotificationStatus.Closed;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
