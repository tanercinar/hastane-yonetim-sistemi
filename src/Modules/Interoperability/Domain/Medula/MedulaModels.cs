namespace HospitalManagement.Modules.Interoperability.Domain.Medula;

/// <summary>
/// MEDULA/SGK işlem türleri — yalnız demo kapsamında tanımlıdır.
/// Gerçek provizyon, fatura veya mali mutabakat işlemi yapılmaz.
/// </summary>
public enum MedulaOperationType
{
    ProvizyonSorgu = 1,
    ProvizyonTeyit = 2,
    HakSahibiDogrulama = 3,
    ItsTeslimBildirimi = 4,
    UtsDogrulamaSorgusu = 5,
}

/// <summary>
/// MEDULA/SGK işlem sonuç durumları.
/// </summary>
public enum MedulaOperationStatus
{
    NotImplemented = 0,
    DemoSuccess = 1,
    DemoRejected = 2,
    OutOfScope = 3,
}

/// <summary>
/// MEDULA/SGK veya İTS/ÜTS demo işlem yanıtı.
/// Tüm yanıtlar açıkça "DEMO — Uygulanmadı" bilgisi taşır.
/// </summary>
public sealed class MedulaOperationResult
{
    public Guid Id
    {
        get; private set;
    }
    public MedulaOperationType OperationType
    {
        get; private set;
    }
    public MedulaOperationStatus Status
    {
        get; private set;
    }
    public string StatusDescription { get; private set; } = string.Empty;
    public string DemoDisclaimer { get; private set; } = string.Empty;
    public string RequestSummary { get; private set; } = string.Empty;
    public string ResponseSummary { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Oluşturulmuş bir demo "uygulanmadı" / "kapsam dışı" yanıtı döndürür.
    /// Gerçek SGK/MEDULA sunucusuna hiçbir çağrı yapılmaz.
    /// </summary>
    public static MedulaOperationResult CreateDemoResponse(
        MedulaOperationType operationType,
        string requestSummary)
    {
        var (status, description, disclaimer, response) = operationType switch
        {
            MedulaOperationType.ProvizyonSorgu => (
                MedulaOperationStatus.NotImplemented,
                "Provizyon sorgusu demo kapsamındadır — gerçek SGK bağlantısı yoktur.",
                "DEMO — Bu işlem gerçek MEDULA/SGK provizyon sorgusu DEĞİLDİR. Finansal model eklenmemiştir.",
                "{ \"provizyon\": \"DEMO-PROV-00001\", \"durum\": \"Uygulanmadı\", \"aciklama\": \"Demo ortamında provizyon sorgusu simüle edilmiştir.\" }"),

            MedulaOperationType.ProvizyonTeyit => (
                MedulaOperationStatus.OutOfScope,
                "Provizyon teyit işlemi kapsam dışıdır — finansal onay/fatura akışı uygulanmamıştır.",
                "DEMO — Gerçek provizyon teyidi yapılmaz. Sahte başarılı provizyon gerçek işlem gibi sunulmaz.",
                "{ \"teyit\": \"KAPSAM_DISI\", \"aciklama\": \"Provizyon teyit işlemi bu demo kapsamında uygulanmamıştır.\" }"),

            MedulaOperationType.HakSahibiDogrulama => (
                MedulaOperationStatus.DemoSuccess,
                "Hak sahibi doğrulaması demo verisiyle simüle edilmiştir.",
                "DEMO — Gerçek SGK hak sahipliği sorgusu yapılmaz. Sonuç sentetiktir.",
                "{ \"tcKimlik\": \"11111111110\", \"hakSahibi\": true, \"sigortaTuru\": \"DEMO-Genel\", \"aciklama\": \"Demo ortamında tüm sentetik hastalar hak sahibi olarak döner.\" }"),

            MedulaOperationType.ItsTeslimBildirimi => (
                MedulaOperationStatus.NotImplemented,
                "İTS ilaç teslim bildirimi demo kontratıdır — gerçek İTS sunucusuna bağlanılmaz.",
                "DEMO — Gerçek İTS (İlaç Takip Sistemi) entegrasyonu uygulanmamıştır.",
                "{ \"itsKarekod\": \"DEMO-ITS-0001\", \"durum\": \"Uygulanmadı\", \"aciklama\": \"İlaç teslim bildirimi simüle edilmiştir.\" }"),

            MedulaOperationType.UtsDogrulamaSorgusu => (
                MedulaOperationStatus.NotImplemented,
                "ÜTS tıbbi cihaz doğrulaması demo kontratıdır — gerçek ÜTS sunucusuna bağlanılmaz.",
                "DEMO — Gerçek ÜTS (Ürün Takip Sistemi) entegrasyonu uygulanmamıştır.",
                "{ \"utsBarcode\": \"DEMO-UTS-0001\", \"durum\": \"Uygulanmadı\", \"aciklama\": \"Tıbbi cihaz doğrulaması simüle edilmiştir.\" }"),

            _ => (
                MedulaOperationStatus.OutOfScope,
                "Bilinmeyen işlem türü — kapsam dışı.",
                "DEMO — Bu işlem kapsam dışıdır.",
                "{ \"durum\": \"KAPSAM_DISI\" }"),
        };

        return new MedulaOperationResult
        {
            Id = Guid.NewGuid(),
            OperationType = operationType,
            Status = status,
            StatusDescription = description,
            DemoDisclaimer = disclaimer,
            RequestSummary = string.IsNullOrWhiteSpace(requestSummary) ? "{}" : requestSummary.Trim(),
            ResponseSummary = response,
            ProcessedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Kapsam dışı finansal bir işlem için açık red yanıtı üretir.
    /// </summary>
    public static MedulaOperationResult CreateOutOfScopeRejection(string operationName)
    {
        return new MedulaOperationResult
        {
            Id = Guid.NewGuid(),
            OperationType = MedulaOperationType.ProvizyonTeyit,
            Status = MedulaOperationStatus.OutOfScope,
            StatusDescription = $"'{operationName}' işlemi kapsam dışıdır — finansal model eklenmemiştir.",
            DemoDisclaimer = "DEMO — Bu hastane yönetim sistemi sertifikalı HBYS veya fatura/provizyon sistemi DEĞİLDİR. Finans, satın alma, bordro ve tam İK özellikleri kapsam dışıdır.",
            RequestSummary = $"{{ \"islem\": \"{operationName}\", \"durum\": \"RED\" }}",
            ResponseSummary = $"{{ \"sonuc\": \"KAPSAM_DISI\", \"aciklama\": \"'{operationName}' demo kapsamında uygulanmamıştır.\" }}",
            ProcessedAtUtc = DateTime.UtcNow,
        };
    }
}
