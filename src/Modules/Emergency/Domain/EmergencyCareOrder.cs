namespace HospitalManagement.Modules.Emergency.Domain;

public sealed class EmergencyCareOrder
{
    private EmergencyCareOrder()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid AdmissionId
    {
        get; private set;
    }
    public EmergencyOrderType OrderType
    {
        get; private set;
    }
    public string OrderCatalogCode { get; private set; } = string.Empty;
    public string OrderCatalogName { get; private set; } = string.Empty;
    public EmergencyOrderPriority Priority
    {
        get; private set;
    }
    public Guid OrderedByDoctorId
    {
        get; private set;
    }
    public DateTime OrderedAtUtc
    {
        get; private set;
    }
    public EmergencyOrderStatus Status
    {
        get; private set;
    }
    public string? ClinicalInstructions
    {
        get; private set;
    }
    public string? ResultSummary
    {
        get; private set;
    }
    public DateTime? CompletedAtUtc
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }
    public uint Version
    {
        get; internal set;
    }

    public static EmergencyCareOrder Create(
        Guid id,
        Guid admissionId,
        EmergencyOrderType orderType,
        string orderCatalogCode,
        string orderCatalogName,
        EmergencyOrderPriority priority,
        Guid orderedByDoctorId,
        string? clinicalInstructions,
        DateTime orderedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("İstem ID boş olamaz.", nameof(id));
        }

        if (admissionId == Guid.Empty)
        {
            throw new ArgumentException("Acil başvuru ID boş olamaz.", nameof(admissionId));
        }

        if (string.IsNullOrWhiteSpace(orderCatalogCode))
        {
            throw new ArgumentException("İstem katalog kodu boş olamaz.", nameof(orderCatalogCode));
        }

        if (string.IsNullOrWhiteSpace(orderCatalogName))
        {
            throw new ArgumentException("İstem katalog adı boş olamaz.", nameof(orderCatalogName));
        }

        if (orderedByDoctorId == Guid.Empty)
        {
            throw new ArgumentException("İstemi yapan hekim ID boş olamaz.", nameof(orderedByDoctorId));
        }

        return new EmergencyCareOrder
        {
            Id = id,
            AdmissionId = admissionId,
            OrderType = orderType,
            OrderCatalogCode = orderCatalogCode.Trim(),
            OrderCatalogName = orderCatalogName.Trim(),
            Priority = priority,
            OrderedByDoctorId = orderedByDoctorId,
            OrderedAtUtc = orderedAtUtc,
            Status = EmergencyOrderStatus.Ordered,
            ClinicalInstructions = string.IsNullOrWhiteSpace(clinicalInstructions) ? null : clinicalInstructions.Trim(),
        };
    }

    public void MarkInProgress(DateTime nowUtc)
    {
        if (Status != EmergencyOrderStatus.Ordered)
        {
            throw new InvalidOperationException($"Yalnızca 'Ordered' durumundaki istem işleme alınabilir. Mevcut durum: {Status}");
        }

        Status = EmergencyOrderStatus.InProgress;
    }

    public void Complete(string? resultSummary, DateTime completedAtUtc)
    {
        if (Status == EmergencyOrderStatus.Completed || Status == EmergencyOrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Sonuçlanmış veya iptal edilmiş istem tamamlanamaz. Mevcut durum: {Status}");
        }

        Status = EmergencyOrderStatus.Completed;
        ResultSummary = string.IsNullOrWhiteSpace(resultSummary) ? null : resultSummary.Trim();
        CompletedAtUtc = completedAtUtc;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == EmergencyOrderStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış istem iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(reason));
        }

        Status = EmergencyOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CompletedAtUtc = nowUtc;
    }
}
