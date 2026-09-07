namespace HospitalManagement.Modules.Pharmacy.Domain;

public sealed class Prescription
{
    private readonly List<PrescriptionItem> _items = [];

    private Prescription()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string PrescriptionNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public Guid EncounterId
    {
        get; private set;
    }
    public Guid PrescribingDoctorId
    {
        get; private set;
    }
    public Guid DepartmentId
    {
        get; private set;
    }
    public PrescriptionStatus Status
    {
        get; private set;
    }
    public DateTime? ValidUntilUtc
    {
        get; private set;
    }
    public DateTime? SignedAtUtc
    {
        get; private set;
    }
    public Guid? SignedByDoctorId
    {
        get; private set;
    }
    public DateTime? CancelledAtUtc
    {
        get; private set;
    }
    public Guid? CancelledByDoctorId
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }
    public DateTime? EnteredInErrorAtUtc
    {
        get; private set;
    }
    public Guid? EnteredInErrorByDoctorId
    {
        get; private set;
    }
    public string? EnteredInErrorReason
    {
        get; private set;
    }
    public string? DiagnosisSummary
    {
        get; private set;
    }
    public string? GeneralInstructions
    {
        get; private set;
    }
    public int Version
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

    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();

    public static Prescription CreateDraft(
        Guid id,
        string prescriptionNumber,
        Guid patientId,
        Guid encounterId,
        Guid prescribingDoctorId,
        Guid departmentId,
        string? diagnosisSummary,
        string? generalInstructions,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Reçete kimliği boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(prescriptionNumber))
        {
            throw new ArgumentException("Reçete numarası boş olamaz.", nameof(prescriptionNumber));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (prescribingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Reçete yazan hekim kimliği boş olamaz.", nameof(prescribingDoctorId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği boş olamaz.", nameof(departmentId));
        }

        return new Prescription
        {
            Id = id,
            PrescriptionNumber = prescriptionNumber.Trim().ToUpperInvariant(),
            PatientId = patientId,
            EncounterId = encounterId,
            PrescribingDoctorId = prescribingDoctorId,
            DepartmentId = departmentId,
            Status = PrescriptionStatus.Draft,
            DiagnosisSummary = string.IsNullOrWhiteSpace(diagnosisSummary) ? null : diagnosisSummary.Trim(),
            GeneralInstructions = string.IsNullOrWhiteSpace(generalInstructions) ? null : generalInstructions.Trim(),
            Version = 1,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = null,
        };
    }

    public PrescriptionItem AddItem(
        Guid itemId,
        Guid medicationCatalogItemId,
        string medicationCode,
        string brandName,
        string genericName,
        MedicationForm form,
        MedicationRoute route,
        decimal dose,
        string doseUnit,
        string frequency,
        int durationDays,
        int quantity,
        string quantityUnit,
        string? instructions,
        DateTime nowUtc)
    {
        EnsureDraftState("Reçeteye kalem ekleme");

        var item = PrescriptionItem.Create(
            itemId,
            Id,
            medicationCatalogItemId,
            medicationCode,
            brandName,
            genericName,
            form,
            route,
            dose,
            doseUnit,
            frequency,
            durationDays,
            quantity,
            quantityUnit,
            instructions,
            nowUtc);

        _items.Add(item);
        UpdatedAtUtc = nowUtc;
        return item;
    }

    public void RemoveItem(Guid itemId, DateTime nowUtc)
    {
        EnsureDraftState("Reçeteden kalem silme");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            throw new InvalidOperationException($"Reçetede '{itemId}' kimlikli kalem bulunamadı.");
        }

        _items.Remove(item);
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateDetails(
        string? diagnosisSummary,
        string? generalInstructions,
        DateTime nowUtc)
    {
        EnsureDraftState("Reçete bilgilerini güncelleme");

        DiagnosisSummary = string.IsNullOrWhiteSpace(diagnosisSummary) ? null : diagnosisSummary.Trim();
        GeneralInstructions = string.IsNullOrWhiteSpace(generalInstructions) ? null : generalInstructions.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Sign(Guid doctorId, DateTime validUntilUtc, DateTime nowUtc)
    {
        EnsureDraftState("Reçete imzalama");

        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException("İmzalayan hekim kimliği boş olamaz.", nameof(doctorId));
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("İçinde en az bir ilaç kalemi bulunmayan reçete imzalanamaz.");
        }

        if (validUntilUtc <= nowUtc)
        {
            throw new ArgumentException("Reçetenin geçerlilik süresi ileri bir tarih olmalıdır.", nameof(validUntilUtc));
        }

        Status = PrescriptionStatus.Signed;
        SignedAtUtc = nowUtc;
        SignedByDoctorId = doctorId;
        ValidUntilUtc = validUntilUtc;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void RecordDispense(Guid itemId, int quantity, DateTime nowUtc)
    {
        if (Status is not (PrescriptionStatus.Signed or PrescriptionStatus.PartiallyDispensed))
        {
            throw new InvalidOperationException($"Yalnızca imzalı veya kısmen teslim edilmiş reçeteler teslim edilebilir. Mevcut durum: '{Status}'.");
        }

        if (ValidUntilUtc.HasValue && nowUtc > ValidUntilUtc.Value)
        {
            Status = PrescriptionStatus.Expired;
            Version++;
            UpdatedAtUtc = nowUtc;
            throw new InvalidOperationException("Reçetenin son geçerlilik süresi dolmuştur; ilaç teslimi yapılamaz.");
        }

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            throw new InvalidOperationException($"Reçetede '{itemId}' kimlikli ilaç kalemi bulunamadı.");
        }

        item.RecordDispense(quantity);

        var allFullyDispensed = _items.Count > 0 && _items.All(i => i.IsFullyDispensed);
        Status = allFullyDispensed ? PrescriptionStatus.Dispensed : PrescriptionStatus.PartiallyDispensed;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(Guid doctorId, string reason, DateTime nowUtc)
    {
        if (Status is not (PrescriptionStatus.Draft or PrescriptionStatus.Signed))
        {
            throw new InvalidOperationException($"'{Status}' durumundaki reçete iptal edilemez.");
        }

        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException("İptal eden hekim kimliği boş olamaz.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reçete iptal gerekçesi zorunludur.", nameof(reason));
        }

        Status = PrescriptionStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelledByDoctorId = doctorId;
        CancellationReason = reason.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public bool CheckAndMarkExpired(DateTime nowUtc)
    {
        if (Status is (PrescriptionStatus.Signed or PrescriptionStatus.PartiallyDispensed)
            && ValidUntilUtc.HasValue
            && nowUtc > ValidUntilUtc.Value)
        {
            Status = PrescriptionStatus.Expired;
            Version++;
            UpdatedAtUtc = nowUtc;
            return true;
        }

        return false;
    }

    public void MarkEnteredInError(Guid doctorId, string reason, DateTime nowUtc)
    {
        if (Status is PrescriptionStatus.Dispensed
            or PrescriptionStatus.Cancelled
            or PrescriptionStatus.Expired
            or PrescriptionStatus.EnteredInError)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki reçete hatalı giriş olarak işaretlenemez.");
        }

        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException("Hatalı giriş bildiren hekim kimliği boş olamaz.", nameof(doctorId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        Status = PrescriptionStatus.EnteredInError;
        EnteredInErrorAtUtc = nowUtc;
        EnteredInErrorByDoctorId = doctorId;
        EnteredInErrorReason = reason.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    private void EnsureDraftState(string operation)
    {
        if (Status != PrescriptionStatus.Draft)
        {
            throw new InvalidOperationException($"'{operation}' işlemi yalnızca taslak durumundaki reçeteler için geçerlidir. Mevcut durum: '{Status}'.");
        }
    }
}
