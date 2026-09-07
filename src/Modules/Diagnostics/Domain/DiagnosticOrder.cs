namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class DiagnosticOrder
{
    private readonly List<DiagnosticOrderItem> _items = [];

    public Guid Id
    {
        get; private set;
    }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public Guid EncounterId
    {
        get; private set;
    }
    public Guid PlacingDoctorId
    {
        get; private set;
    }
    public Guid DepartmentId
    {
        get; private set;
    }
    public DiagnosticOrderType OrderType
    {
        get; private set;
    }
    public DiagnosticOrderPriority Priority
    {
        get; private set;
    }
    public DiagnosticOrderStatus Status
    {
        get; private set;
    }
    public string? ClinicalIndication
    {
        get; private set;
    }
    public string? OrderNotes
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }
    public string? EnteredInErrorReason
    {
        get; private set;
    }
    public DateTime? PlacedAtUtc
    {
        get; private set;
    }
    public DateTime? CompletedAtUtc
    {
        get; private set;
    }
    public DateTime? CancelledAtUtc
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
    public uint Version
    {
        get; private set;
    }

    public IReadOnlyCollection<DiagnosticOrderItem> Items => _items.AsReadOnly();

    private DiagnosticOrder()
    {
    }

    public static DiagnosticOrder CreateDraft(
        Guid id,
        string orderNumber,
        Guid patientId,
        Guid encounterId,
        Guid placingDoctorId,
        Guid departmentId,
        DiagnosticOrderType orderType,
        DiagnosticOrderPriority priority,
        string? clinicalIndication,
        string? orderNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("İstem kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new ArgumentException("İstem numarası zorunludur.", nameof(orderNumber));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği zorunludur.", nameof(encounterId));
        }

        if (placingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("İsteyen hekim kimliği zorunludur.", nameof(placingDoctorId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği zorunludur.", nameof(departmentId));
        }

        return new DiagnosticOrder
        {
            Id = id,
            OrderNumber = orderNumber.Trim(),
            PatientId = patientId,
            EncounterId = encounterId,
            PlacingDoctorId = placingDoctorId,
            DepartmentId = departmentId,
            OrderType = orderType,
            Priority = priority,
            Status = DiagnosticOrderStatus.Draft,
            ClinicalIndication = clinicalIndication?.Trim(),
            OrderNotes = orderNotes?.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
        };
    }

    public void AddItem(
        Guid itemId,
        string catalogCode,
        string catalogItemName,
        string category,
        string? specialInstructions,
        DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Yalnızca taslak durumundaki istemlere kalem eklenebilir. Mevcut durum: {Status}");
        }

        if (_items.Any(i => i.CatalogCode.Equals(catalogCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"İstemde zaten bu test veya tetkik kalemi ({catalogCode}) bulunmaktadır.");
        }

        var item = DiagnosticOrderItem.Create(
            itemId,
            Id,
            catalogCode,
            catalogItemName,
            category,
            specialInstructions,
            nowUtc);

        _items.Add(item);
        UpdatedAtUtc = nowUtc;
    }

    public void RemoveItem(Guid itemId, DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Yalnızca taslak durumundaki istemlerden kalem silinebilir. Mevcut durum: {Status}");
        }

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
        {
            _items.Remove(item);
            UpdatedAtUtc = nowUtc;
        }
    }

    public void Place(Guid doctorId, DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Yalnızca taslak durumundaki istemler onaylanıp işleme alınabilir. Mevcut durum: {Status}");
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("En az bir test veya tetkik kalemi olmadan istem verilemez.");
        }

        Status = DiagnosticOrderStatus.Placed;
        PlacedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void StartProcessing(DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Placed)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'İletildi/Onaylandı' (Placed) durumundaki istemler işleme alınabilir. Mevcut durum: {Status}");
        }

        Status = DiagnosticOrderStatus.InProgress;
        UpdatedAtUtc = nowUtc;
    }

    public void Complete(DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Placed && Status != DiagnosticOrderStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Yalnızca aktif işlemdeki istemler tamamlanabilir. Mevcut durum: {Status}");
        }

        Status = DiagnosticOrderStatus.Completed;
        CompletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(Guid cancellingUserId, string reason, DateTime nowUtc)
    {
        if (Status != DiagnosticOrderStatus.Draft && Status != DiagnosticOrderStatus.Placed)
        {
            throw new InvalidOperationException(
                $"İşleme başlanmış veya tamamlanmış istemler iptal edilemez. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi belirtilmelidir.", nameof(reason));
        }

        Status = DiagnosticOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        foreach (var item in _items)
        {
            item.UpdateStatus(DiagnosticOrderItemStatus.Cancelled, nowUtc);
        }
    }

    public void MarkEnteredInError(Guid actorUserId, string reason, DateTime nowUtc)
    {
        if (Status == DiagnosticOrderStatus.EnteredInError)
        {
            throw new InvalidOperationException("İstem zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        Status = DiagnosticOrderStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;

        foreach (var item in _items)
        {
            item.UpdateStatus(DiagnosticOrderItemStatus.Cancelled, nowUtc);
        }
    }
}
