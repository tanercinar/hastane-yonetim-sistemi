namespace HospitalManagement.Host.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "HospitalManagement:Observability";

    public string ServiceName { get; init; } = string.Empty;

    public bool ConsoleExporterEnabled
    {
        get; init;
    }
}
