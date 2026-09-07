namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class CrossmatchRequest
{
    private CrossmatchRequest()
    {
    }

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
    public Guid PatientId
    {
        get; private set;
    }
    public BloodGroup PatientBloodGroup
    {
        get; private set;
    }
    public BloodProductType RequestedProductType
    {
        get; private set;
    }
    public int UnitsRequested
    {
        get; private set;
    }
    public DateTime? RequiredByUtc
    {
        get; private set;
    }
    public CrossmatchStatus Status
    {
        get; private set;
    }
    public BloodCompatibilityStatus CompatibilityResult
    {
        get; private set;
    }

    public string? TechnicianNotes
    {
        get; private set;
    }
    public DateTime? TestedAtUtc
    {
        get; private set;
    }
    public Guid? TestedByUserId
    {
        get; private set;
    }
    public Guid? AllocatedBloodUnitId
    {
        get; private set;
    }

    public string? CancellationReason
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
    public int Version { get; set; } = 1;

    public static CrossmatchRequest Create(
        Guid id,
        Guid diagnosticOrderId,
        Guid diagnosticOrderItemId,
        Guid patientId,
        BloodGroup patientBloodGroup,
        BloodProductType requestedProductType,
        int unitsRequested,
        DateTime? requiredByUtc,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id zorunludur.", nameof(id));
        if (diagnosticOrderId == Guid.Empty)
            throw new ArgumentException("DiagnosticOrderId zorunludur.", nameof(diagnosticOrderId));
        if (diagnosticOrderItemId == Guid.Empty)
            throw new ArgumentException("DiagnosticOrderItemId zorunludur.", nameof(diagnosticOrderItemId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId zorunludur.", nameof(patientId));
        if (unitsRequested <= 0)
            throw new ArgumentException("Talep edilen ünite adedi 0'dan büyük olmalıdır.", nameof(unitsRequested));

        return new CrossmatchRequest
        {
            Id = id,
            DiagnosticOrderId = diagnosticOrderId,
            DiagnosticOrderItemId = diagnosticOrderItemId,
            PatientId = patientId,
            PatientBloodGroup = patientBloodGroup,
            RequestedProductType = requestedProductType,
            UnitsRequested = unitsRequested,
            RequiredByUtc = requiredByUtc,
            Status = CrossmatchStatus.Requested,
            CompatibilityResult = BloodCompatibilityStatus.PendingTesting,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void RecordTestResult(
        Guid testedByUserId,
        BloodCompatibilityStatus result,
        Guid? allocatedBloodUnitId,
        string? notes,
        DateTime nowUtc)
    {
        if (Status == CrossmatchStatus.Cancelled)
        {
            throw new InvalidOperationException("İptal edilmiş bir istem üzerinde crossmatch testi yapılamaz.");
        }

        if (result == BloodCompatibilityStatus.Compatible && allocatedBloodUnitId is null)
        {
            throw new ArgumentException("Uygun (Compatible) bulunan crossmatch testi için tahsis edilen kan ünitesi zorunludur.", nameof(allocatedBloodUnitId));
        }

        TestedByUserId = testedByUserId;
        CompatibilityResult = result;
        AllocatedBloodUnitId = allocatedBloodUnitId;
        TechnicianNotes = notes?.Trim();
        TestedAtUtc = nowUtc;
        Status = CrossmatchStatus.Completed;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == CrossmatchStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış bir crossmatch testi iptal edilemez.");
        }

        Status = CrossmatchStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
