namespace HospitalManagement.Host.Configuration;

public sealed class RuntimeOptions
{
    public const string SectionName = "HospitalManagement:Runtime";

    public string DataMode { get; set; } = string.Empty;

    public bool EmbeddedArtificialIntelligenceEnabled
    {
        get;
        set;
    }
}

public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "HospitalDatabase";

    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ObjectStorageOptions
{
    public const string SectionName = "HospitalManagement:ObjectStorage";

    public string Endpoint { get; set; } = string.Empty;

    public bool UseTls
    {
        get;
        set;
    }

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "HospitalManagement:Email";

    public string Mode { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port
    {
        get;
        set;
    }

    public string SenderAddress { get; set; } = string.Empty;
}
