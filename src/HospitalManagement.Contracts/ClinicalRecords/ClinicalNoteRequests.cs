namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateClinicalNoteRequest
{
    public Guid EncounterId
    {
        get; init;
    }

    public Guid PatientId
    {
        get; init;
    }

    public string NoteType { get; init; } = "GeneralSoap";

    public string Title { get; init; } = string.Empty;

    public string? ChiefComplaint
    {
        get; init;
    }

    public string? HistoryOfPresentIllness
    {
        get; init;
    }

    public string? PhysicalExamination
    {
        get; init;
    }

    public string? Assessment
    {
        get; init;
    }

    public string? Plan
    {
        get; init;
    }

    public string? Content
    {
        get; init;
    }
}

public sealed record UpdateClinicalNoteDraftRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Title { get; init; } = string.Empty;

    public string? ChiefComplaint
    {
        get; init;
    }

    public string? HistoryOfPresentIllness
    {
        get; init;
    }

    public string? PhysicalExamination
    {
        get; init;
    }

    public string? Assessment
    {
        get; init;
    }

    public string? Plan
    {
        get; init;
    }

    public string? Content
    {
        get; init;
    }
}

public sealed record SignClinicalNoteRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string? SignatureNote
    {
        get; init;
    }
}

public sealed record AddClinicalNoteAddendumRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string AddendumContent { get; init; } = string.Empty;

    public string Reason { get; init; } = "Ek klinik bilgi / düzeltme";
}

public sealed record MarkClinicalNoteEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı giriş";
}
