namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record MarkAttachmentEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı yüklenen dosya";
}
