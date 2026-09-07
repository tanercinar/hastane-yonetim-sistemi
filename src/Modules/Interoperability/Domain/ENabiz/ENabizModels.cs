namespace HospitalManagement.Modules.Interoperability.Domain.ENabiz;

public enum ENabizPackageType
{
    Package101HastaKayit = 101,
    Package102HizmetIstem = 102,
    Package103LaboratuvarSonuc = 103,
    Package104RadyolojiSonuc = 104,
    Package105Recete = 105,
    Package106Epikriz = 106,
}

public enum ENabizTransmissionStatus
{
    Queued = 1,
    Transmitting = 2,
    Successful = 3,
    Failed = 4,
    ConsentDenied = 5,
}

public sealed class ENabizTransmissionRecord
{
    private ENabizTransmissionRecord()
    {
    }

    public ENabizTransmissionRecord(
        ENabizPackageType packageType,
        Guid patientId,
        string patientNationalId,
        bool hasPatientConsent,
        string payloadSummary)
    {
        Id = Guid.NewGuid();
        SysTakipNo = $"SYS-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        PackageType = packageType;
        PatientId = patientId;
        PatientNationalId = string.IsNullOrWhiteSpace(patientNationalId) ? "11111111110" : patientNationalId.Trim();
        HasPatientConsent = hasPatientConsent;
        PayloadSummary = string.IsNullOrWhiteSpace(payloadSummary) ? "{}" : payloadSummary.Trim();
        RetryCount = 0;
        QueuedAtUtc = DateTime.UtcNow;

        if (!hasPatientConsent)
        {
            Status = ENabizTransmissionStatus.ConsentDenied;
            ResponseCode = "ERR_CONSENT_DENIED";
            ResponseMessage = "Hasta e-Nabız veri aktarımına rıza göstermediğinden paket iletilemedi.";
        }
        else
        {
            Status = ENabizTransmissionStatus.Queued;
            ResponseCode = "QUEUED";
            ResponseMessage = "Paket gönderim kuyruğuna eklendi.";
        }
    }

    public Guid Id
    {
        get; private set;
    }
    public string SysTakipNo { get; private set; } = string.Empty;
    public ENabizPackageType PackageType
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public string PatientNationalId { get; private set; } = string.Empty;
    public bool HasPatientConsent
    {
        get; private set;
    }
    public ENabizTransmissionStatus Status
    {
        get; private set;
    }
    public string PayloadSummary { get; private set; } = string.Empty;
    public string ResponseCode { get; private set; } = string.Empty;
    public string ResponseMessage { get; private set; } = string.Empty;
    public int RetryCount
    {
        get; private set;
    }
    public DateTime QueuedAtUtc
    {
        get; private set;
    }
    public DateTime? SentAtUtc
    {
        get; private set;
    }
    public DateTime? LastAttemptAtUtc
    {
        get; private set;
    }

    public void MarkTransmitting()
    {
        Status = ENabizTransmissionStatus.Transmitting;
        LastAttemptAtUtc = DateTime.UtcNow;
    }

    public void MarkSuccess(string responseCode, string responseMessage)
    {
        Status = ENabizTransmissionStatus.Successful;
        ResponseCode = string.IsNullOrWhiteSpace(responseCode) ? "SYS_SUCCESS_200" : responseCode.Trim();
        ResponseMessage = string.IsNullOrWhiteSpace(responseMessage) ? "Paket e-Nabız sistemine başarıyla iletildi." : responseMessage.Trim();
        SentAtUtc = DateTime.UtcNow;
        LastAttemptAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorCode, string errorMessage)
    {
        Status = ENabizTransmissionStatus.Failed;
        ResponseCode = string.IsNullOrWhiteSpace(errorCode) ? "ERR_SYS_GATEWAY" : errorCode.Trim();
        ResponseMessage = string.IsNullOrWhiteSpace(errorMessage) ? "e-Nabız sunucusuna paket aktarılamadı." : errorMessage.Trim();
        RetryCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
    }
}
