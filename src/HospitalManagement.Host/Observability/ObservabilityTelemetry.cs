using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HospitalManagement.Host.Observability;

public static class ObservabilityTelemetry
{
    public const string ActivitySourceName = "HospitalManagement.Host.Requests";
    public const string MeterName = "HospitalManagement.Host.Requests";
    public const string RequestActivityName = "hospital.http.request";
    public const string RequestDurationInstrumentName = "hospital.http.server.request.duration";

    internal static readonly ActivitySource RequestActivitySource = new(ActivitySourceName);

    internal static readonly Meter RequestMeter = new(MeterName);

    internal static readonly Histogram<double> RequestDuration = RequestMeter.CreateHistogram<double>(
        RequestDurationInstrumentName,
        unit: "ms",
        description: "HTTP request duration measured at the safe application boundary.");
}
