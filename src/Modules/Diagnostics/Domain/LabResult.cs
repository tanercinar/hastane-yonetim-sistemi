namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class LabResult
{
    private readonly List<LabResultItem> _items = [];

    public Guid Id
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
    public Guid? SpecimenId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public string CatalogCode { get; private set; } = string.Empty;
    public string CatalogItemName { get; private set; } = string.Empty;
    public LabResultStatus Status
    {
        get; private set;
    }
    public Guid? TechnicallyApprovedByUserId
    {
        get; private set;
    }
    public DateTime? TechnicallyApprovedAtUtc
    {
        get; private set;
    }
    public Guid? ClinicallyApprovedByUserId
    {
        get; private set;
    }
    public DateTime? ClinicallyApprovedAtUtc
    {
        get; private set;
    }
    public Guid? PreviousResultId
    {
        get; private set;
    }
    public string? CorrectionReason
    {
        get; private set;
    }
    public string? ClinicalNotes
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
    public int Version
    {
        get; private set;
    }

    public IReadOnlyCollection<LabResultItem> Items => _items.AsReadOnly();

    private LabResult()
    {
    }

    public static LabResult CreateDraft(
        Guid id,
        Guid diagnosticOrderId,
        Guid diagnosticOrderItemId,
        Guid? specimenId,
        Guid patientId,
        string catalogCode,
        string catalogItemName,
        string? clinicalNotes,
        DateTime nowUtc,
        IEnumerable<LabResultItem> items)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Laboratuvar sonuç kimliği zorunludur.", nameof(id));
        }

        if (diagnosticOrderId == Guid.Empty)
        {
            throw new ArgumentException("Tanısal istem kimliği zorunludur.", nameof(diagnosticOrderId));
        }

        if (diagnosticOrderItemId == Guid.Empty)
        {
            throw new ArgumentException("İstem kalemi kimliği zorunludur.", nameof(diagnosticOrderItemId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(catalogCode))
        {
            throw new ArgumentException("Katalog kodu zorunludur.", nameof(catalogCode));
        }

        if (string.IsNullOrWhiteSpace(catalogItemName))
        {
            throw new ArgumentException("Katalog adı zorunludur.", nameof(catalogItemName));
        }

        var result = new LabResult
        {
            Id = id,
            DiagnosticOrderId = diagnosticOrderId,
            DiagnosticOrderItemId = diagnosticOrderItemId,
            SpecimenId = specimenId,
            PatientId = patientId,
            CatalogCode = catalogCode.Trim().ToUpperInvariant(),
            CatalogItemName = catalogItemName.Trim(),
            Status = LabResultStatus.Draft,
            ClinicalNotes = clinicalNotes?.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
            Version = 1,
        };

        if (items != null)
        {
            result._items.AddRange(items);
        }

        return result;
    }

    public void UpdateItems(
        IEnumerable<(string ParameterCode, decimal? NumericValue, string? StringValue, string? Notes)> parameterValues,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        if (Status != LabResultStatus.Draft && Status != LabResultStatus.TechnicallyApproved)
        {
            throw new InvalidOperationException(
                $"Yalnızca taslak veya teknik onaydaki sonuçlar düzenlenebilir. Kesinleşmiş (onaylı) sonuçlar sessizce güncellenemez. Mevcut durum: {Status}");
        }

        var valuesDict = parameterValues.ToDictionary(x => x.ParameterCode.ToUpperInvariant(), x => x);

        foreach (var item in _items)
        {
            if (valuesDict.TryGetValue(item.ParameterCode.ToUpperInvariant(), out var val))
            {
                item.UpdateValue(val.NumericValue, val.StringValue, val.Notes);
            }
        }

        ClinicalNotes = clinicalNotes?.Trim() ?? ClinicalNotes;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ApproveTechnically(
        Guid userId,
        DateTime nowUtc)
    {
        if (Status != LabResultStatus.Draft)
        {
            throw new InvalidOperationException($"Yalnızca taslak durumundaki sonuçlar teknik onaya alınabilir. Mevcut durum: {Status}");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Teknik onayı veren kullanıcı kimliği zorunludur.", nameof(userId));
        }

        Status = LabResultStatus.TechnicallyApproved;
        TechnicallyApprovedByUserId = userId;
        TechnicallyApprovedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ApproveClinically(
        Guid userId,
        DateTime nowUtc)
    {
        if (Status != LabResultStatus.TechnicallyApproved)
        {
            throw new InvalidOperationException($"Yalnızca teknik onayı tamamlanmış sonuçlar klinik olarak onaylanabilir. Mevcut durum: {Status}");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Klinik onayı veren uzman hekim kimliği zorunludur.", nameof(userId));
        }

        Status = LabResultStatus.FinalApproved;
        ClinicallyApprovedByUserId = userId;
        ClinicallyApprovedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public LabResult CreateCorrection(
        Guid newResultId,
        string correctionReason,
        Guid correctedByUserId,
        DateTime nowUtc,
        IEnumerable<(string ParameterCode, decimal? NumericValue, string? StringValue, string? Notes)> correctedValues)
    {
        if (string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new ArgumentException("Sonuç düzeltmesi için zorunlu bir gerekçe belirtilmelidir.", nameof(correctionReason));
        }

        if (Status != LabResultStatus.FinalApproved && Status != LabResultStatus.Corrected)
        {
            throw new InvalidOperationException("Yalnızca kesinleşmiş/onaylı sonuçlar düzeltme akışına alınabilir.");
        }

        var newResult = new LabResult
        {
            Id = newResultId,
            DiagnosticOrderId = DiagnosticOrderId,
            DiagnosticOrderItemId = DiagnosticOrderItemId,
            SpecimenId = SpecimenId,
            PatientId = PatientId,
            CatalogCode = CatalogCode,
            CatalogItemName = CatalogItemName,
            Status = LabResultStatus.Corrected,
            PreviousResultId = Id,
            CorrectionReason = correctionReason.Trim(),
            ClinicallyApprovedByUserId = correctedByUserId,
            ClinicallyApprovedAtUtc = nowUtc,
            ClinicalNotes = ClinicalNotes,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
            Version = 1,
        };

        var valDict = correctedValues.ToDictionary(x => x.ParameterCode.ToUpperInvariant(), x => x);

        foreach (var oldItem in _items)
        {
            decimal? numVal = oldItem.NumericValue;
            string? strVal = oldItem.StringValue;
            string? note = oldItem.Notes;

            if (valDict.TryGetValue(oldItem.ParameterCode.ToUpperInvariant(), out var v))
            {
                numVal = v.NumericValue;
                strVal = v.StringValue;
                note = v.Notes;
            }

            var newItem = LabResultItem.Create(
                Guid.NewGuid(),
                newResultId,
                oldItem.ParameterCode,
                oldItem.ParameterName,
                numVal,
                strVal,
                oldItem.Unit,
                oldItem.ReferenceRangeLow,
                oldItem.ReferenceRangeHigh,
                oldItem.ReferenceRangeText,
                note);

            newResult._items.Add(newItem);
        }

        return newResult;
    }

    public void MarkEnteredInError(DateTime nowUtc)
    {
        Status = LabResultStatus.EnteredInError;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
