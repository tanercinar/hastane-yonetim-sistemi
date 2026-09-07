using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HospitalManagement.Host.Observability;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace HospitalManagement.IntegrationTests;

public sealed class ApiFoundationTests(ApiFoundationFixture fixture)
    : IClassFixture<ApiFoundationFixture>
{
    private const string CorrelationHeaderName = "X-Correlation-ID";

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task StatusEndpointReturnsVersionedDemoContractAndCorrelationId()
    {
        const string correlationId = "DEMO-api-contract-001";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/status");
        request.Headers.Add(CorrelationHeaderName, correlationId);

        using var response = await CreateClient().SendAsync(request);
        var payload = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, GetSingleHeaderValue(response, CorrelationHeaderName));
        Assert.Equal("HospitalManagement.Api", payload.GetProperty("service").GetString());
        Assert.Equal("v1", payload.GetProperty("apiVersion").GetString());
        Assert.Equal("DEMO", payload.GetProperty("dataMode").GetString());
        Assert.Equal(correlationId, payload.GetProperty("correlationId").GetString());
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task InvalidCorrelationIdIsReplacedWithSafeServerGeneratedValue()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/status");
        request.Headers.TryAddWithoutValidation(
            CorrelationHeaderName,
            "invalid correlation id containing spaces and /unsafe/path");

        using var response = await CreateClient().SendAsync(request);
        var payload = await ReadJsonAsync(response);
        var actualCorrelationId = GetSingleHeaderValue(response, CorrelationHeaderName);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches("^[a-f0-9]{32}$", actualCorrelationId);
        Assert.Equal(actualCorrelationId, payload.GetProperty("correlationId").GetString());
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task OpenApiDocumentPublishesOnlySupportedVersionedEndpoint()
    {
        using var response = await CreateClient().GetAsync("/openapi/v1.json");
        var payload = await ReadJsonAsync(response);
        var paths = payload.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("3.", payload.GetProperty("openapi").GetString(), StringComparison.Ordinal);
        Assert.True(paths.TryGetProperty("/api/v1/platform/status", out _));
        Assert.False(paths.TryGetProperty("/api/v1/platform/validation-probe", out _));
        Assert.False(paths.TryGetProperty("/api/v1/platform/failure-probe", out _));
        Assert.False(paths.TryGetProperty(
            "/api/v1/platform/telemetry-probe/{resourceId}",
            out _));
        Assert.False(paths.TryGetProperty("/health/live", out _));
        Assert.False(paths.TryGetProperty("/health/ready", out _));
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F10-KAPI")]
    public async Task OpenApiPublishesPhase10MockContractsWithoutRealInstitutionEndpoints()
    {
        using var response = await CreateClient().GetAsync("/openapi/v1.json");
        var payload = await ReadJsonAsync(response);
        var paths = payload.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string[] requiredPaths =
        [
            "/api/v1/interoperability/mock-engine/configs",
            "/api/v1/interoperability/fhir/r4/Patient/{id}/$export",
            "/api/v1/interoperability/hl7/inbound",
            "/api/v1/interoperability/dicom/worklist",
            "/api/v1/interoperability/mhrs/appointments",
            "/api/v1/interoperability/enabiz/queue",
            "/api/v1/interoperability/medula/boundaries",
            "/api/v1/notifications/preferences",
        ];

        Assert.All(requiredPaths, path => Assert.True(paths.TryGetProperty(path, out _), path));
        var contract = payload.GetRawText();
        Assert.DoesNotContain("saglik.gov.tr", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sgk.gov.tr", contract, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G07")]
    public async Task HealthEndpointsReturnMinimalLiveAndPostgreSqlReadinessContracts()
    {
        using var liveResponse = await CreateClient().GetAsync("/health/live");
        var livePayload = await ReadJsonAsync(liveResponse);

        using var readyResponse = await CreateClient().GetAsync("/health/ready");
        var readyPayload = await ReadJsonAsync(readyResponse);
        var serializedReadyPayload = readyPayload.GetRawText();

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal("Healthy", livePayload.GetProperty("status").GetString());
        Assert.Empty(livePayload.GetProperty("checks").EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal("Healthy", readyPayload.GetProperty("status").GetString());
        var readinessCheck = Assert.Single(readyPayload.GetProperty("checks").EnumerateArray());
        Assert.Equal("postgresql", readinessCheck.GetProperty("name").GetString());
        Assert.Equal("Healthy", readinessCheck.GetProperty("status").GetString());
        Assert.DoesNotContain("exception", serializedReadyPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("description", serializedReadyPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("no-store, no-cache", readyResponse.Headers.CacheControl?.ToString());
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task MissingApiRouteReturnsStableProblemDetailsContract()
    {
        using var response = await CreateClient().GetAsync("/api/v1/missing");
        var problem = await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "not_found");

        Assert.Equal("Kaynak bulunamadı", problem.GetProperty("title").GetString());
        AssertClientSafe(problem.GetRawText());
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task InvalidRequestReturnsFieldBasedValidationProblemDetails()
    {
        using var response = await CreateClient().PostAsJsonAsync(
            "/api/v1/platform/validation-probe",
            new
            {
                ClientName = string.Empty,
            });
        var problem = await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation_failed");

        var errors = problem.GetProperty("errors");
        var clientNameErrors = errors.GetProperty("clientName").EnumerateArray()
            .Select(error => error.GetString())
            .ToArray();

        Assert.Contains("required", clientNameErrors);
        Assert.DoesNotContain(clientNameErrors, error => string.IsNullOrWhiteSpace(error));
        AssertClientSafe(problem.GetRawText());
    }

    [Fact]
    [Trait("Category", "Contract")]
    [Trait("Roadmap", "F01-G07")]
    public async Task UnexpectedExceptionReturnsGenericProblemWithoutCanaryOrStackTrace()
    {
        const string correlationId = "DEMO-api-failure-001";
        using var loggerProvider = new CapturingLoggerProvider();
        using var factory = fixture.CreateObservabilityFactory(loggerProvider);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/failure-probe");
        request.Headers.Add(CorrelationHeaderName, correlationId);
        using var response = await client.SendAsync(request);
        var problem = await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "server_error");
        var serializedProblem = problem.GetRawText();

        Assert.DoesNotContain("DEMO API failure probe", serializedProblem, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", serializedProblem, StringComparison.Ordinal);
        AssertClientSafe(serializedProblem);

        var exceptionLog = Assert.Single(loggerProvider.Entries, entry => entry.EventId.Id == 2200);
        Assert.Equal(LogLevel.Error, exceptionLog.Level);
        Assert.Equal("System.InvalidOperationException", GetPropertyString(exceptionLog.Properties, "ExceptionType"));
        Assert.Equal(correlationId, GetPropertyString(exceptionLog.Properties, "CorrelationId"));
        Assert.DoesNotContain("DEMO API failure probe", exceptionLog.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F01-G07")]
    public async Task RateLimitRejectionReturnsRetryHintAndProblemDetailsContract()
    {
        using var rateLimitedFactory = fixture.CreateRateLimitedFactory(permitLimit: 1);
        using var client = rateLimitedFactory.CreateClient();

        using var acceptedResponse = await client.GetAsync("/api/v1/platform/status");
        using var rejectedResponse = await client.GetAsync("/api/v1/platform/status");
        var problem = await AssertProblemDetailsAsync(
            rejectedResponse,
            HttpStatusCode.TooManyRequests,
            "rate_limit_exceeded");

        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        Assert.True(rejectedResponse.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.TryParse(Assert.Single(retryAfter), out var retryAfterSeconds));
        Assert.InRange(retryAfterSeconds, 1, 60);
        AssertClientSafe(problem.GetRawText());
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("Roadmap", "F01-G09")]
    public async Task RequestTelemetryCorrelatesSafeLogTraceAndMetricWithoutPayloadLeakage()
    {
        const string correlationId = "DEMO-observability-001";
        const string sensitiveCanary = "DEMO-SENSITIVE-CANARY-X";
        const string expectedRoute = "/api/v1/platform/telemetry-probe/{resourceId}";

        var activities = new ConcurrentQueue<ActivitySnapshot>();
        var measurements = new ConcurrentQueue<MeasurementSnapshot>();
        using var loggerProvider = new CapturingLoggerProvider();
        using var activityListener = CreateActivityListener(activities);
        using var meterListener = CreateMeterListener(measurements);
        using var factory = fixture.CreateObservabilityFactory(loggerProvider);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/platform/telemetry-probe/{sensitiveCanary}?clinicalNote={sensitiveCanary}")
        {
            Content = JsonContent.Create(new
            {
                ClientName = sensitiveCanary,
            }),
        };
        request.Headers.Add(CorrelationHeaderName, correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var jsonConsoleOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JsonConsoleFormatterOptions>>()
            .Get(ConsoleFormatterNames.Json);
        Assert.False(jsonConsoleOptions.IncludeScopes);

        var requestLog = Assert.Single(loggerProvider.Entries, entry =>
            entry.EventId.Id == 2100
            && GetPropertyString(entry.Properties, "CorrelationId") == correlationId);
        Assert.Equal(LogLevel.Information, requestLog.Level);
        Assert.Equal("POST", GetPropertyString(requestLog.Properties, "RequestMethod"));
        Assert.Equal(expectedRoute, GetPropertyString(requestLog.Properties, "RouteTemplate"));
        Assert.Equal(204, GetPropertyInt32(requestLog.Properties, "StatusCode"));
        Assert.True(GetPropertyDouble(requestLog.Properties, "ElapsedMilliseconds") >= 0);

        var traceId = GetPropertyString(requestLog.Properties, "TraceId");
        Assert.Matches("^[a-f0-9]{32}$", traceId);

        var requestActivity = Assert.Single(activities, activity =>
            GetPropertyString(activity.Tags, "hospital.correlation_id") == correlationId);
        Assert.Equal(ObservabilityTelemetry.RequestActivityName, requestActivity.Name);
        Assert.True(requestActivity.Recorded);
        Assert.Equal(traceId, requestActivity.TraceId);
        Assert.Equal("POST", GetPropertyString(requestActivity.Tags, "http.request.method"));
        Assert.Equal(expectedRoute, GetPropertyString(requestActivity.Tags, "http.route"));
        Assert.Equal(204, GetPropertyInt32(requestActivity.Tags, "http.response.status_code"));

        var requestMeasurement = Assert.Single(measurements, measurement =>
            measurement.InstrumentName == ObservabilityTelemetry.RequestDurationInstrumentName
            && GetPropertyString(measurement.Tags, "http.route") == expectedRoute);
        Assert.True(requestMeasurement.Value >= 0);
        Assert.Equal("POST", GetPropertyString(requestMeasurement.Tags, "http.request.method"));
        Assert.Equal(204, GetPropertyInt32(requestMeasurement.Tags, "http.response.status_code"));
        Assert.DoesNotContain("hospital.correlation_id", requestMeasurement.Tags.Keys);

        var redactionLog = Assert.Single(loggerProvider.Entries, entry =>
            entry.EventId.Id == 2199);
        Assert.DoesNotContain(sensitiveCanary, redactionLog.Message, StringComparison.Ordinal);

        var capturedTelemetry = string.Join(
            Environment.NewLine,
            loggerProvider.Entries.Select(entry => entry.Message)
                .Concat(activities.Select(activity => SerializeTags(activity.Tags)))
                .Concat(measurements.Select(measurement => SerializeTags(measurement.Tags))));
        Assert.DoesNotContain(sensitiveCanary, capturedTelemetry, StringComparison.Ordinal);
        Assert.DoesNotContain("clinicalNote", capturedTelemetry, StringComparison.Ordinal);
    }

    private HttpClient CreateClient()
    {
        return fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    private static async Task<JsonElement> AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        var problem = await ReadJsonAsync(response);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.EndsWith($"/{expectedCode}", problem.GetProperty("type").GetString(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("correlationId").GetString()));
        Assert.StartsWith(
            "urn:hospital-management:request:",
            problem.GetProperty("instance").GetString(),
            StringComparison.Ordinal);
        Assert.Equal(
            problem.GetProperty("correlationId").GetString(),
            GetSingleHeaderValue(response, CorrelationHeaderName));

        return problem;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);
        return document.RootElement.Clone();
    }

    private static string GetSingleHeaderValue(HttpResponseMessage response, string headerName)
    {
        Assert.True(response.Headers.TryGetValues(headerName, out var headerValues));
        return Assert.Single(headerValues);
    }

    private static void AssertClientSafe(string responseBody)
    {
        Assert.DoesNotContain("stack", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at ", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\\\", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/src/", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    private static ActivityListener CreateActivityListener(
        ConcurrentQueue<ActivitySnapshot> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.PropagationData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.PropagationData,
            ActivityStopped = activity => activities.Enqueue(new ActivitySnapshot(
                activity.DisplayName,
                activity.TraceId.ToHexString(),
                activity.Recorded,
                activity.TagObjects.ToDictionary(
                    tag => tag.Key,
                    tag => tag.Value,
                    StringComparer.Ordinal))),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static MeterListener CreateMeterListener(
        ConcurrentQueue<MeasurementSnapshot> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ObservabilityTelemetry.MeterName
                && instrument.Name == ObservabilityTelemetry.RequestDurationInstrumentName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            var capturedTags = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                capturedTags[tag.Key] = tag.Value;
            }

            measurements.Enqueue(new MeasurementSnapshot(
                instrument.Name,
                measurement,
                capturedTags));
        });
        listener.Start();
        return listener;
    }

    private static string GetPropertyString(
        IReadOnlyDictionary<string, object?> properties,
        string key)
    {
        Assert.True(properties.TryGetValue(key, out var value), $"Missing telemetry property '{key}'.");
        return Assert.IsType<string>(value);
    }

    private static int GetPropertyInt32(
        IReadOnlyDictionary<string, object?> properties,
        string key)
    {
        Assert.True(properties.TryGetValue(key, out var value), $"Missing telemetry property '{key}'.");
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static double GetPropertyDouble(
        IReadOnlyDictionary<string, object?> properties,
        string key)
    {
        Assert.True(properties.TryGetValue(key, out var value), $"Missing telemetry property '{key}'.");
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static string SerializeTags(IReadOnlyDictionary<string, object?> tags)
    {
        return string.Join(
            ";",
            tags.OrderBy(tag => tag.Key, StringComparer.Ordinal)
                .Select(tag => $"{tag.Key}={tag.Value}"));
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<CapturedLogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) =>
            new CapturingLogger(categoryName, Entries);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(
        string categoryName,
        ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> structuredState
                ? structuredState
                    .Where(property => property.Key != "{OriginalFormat}")
                    .ToDictionary(
                        property => property.Key,
                        property => property.Value,
                        StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);

            entries.Enqueue(new CapturedLogEntry(
                categoryName,
                logLevel,
                eventId,
                formatter(state, exception),
                properties));
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed record ActivitySnapshot(
        string Name,
        string TraceId,
        bool Recorded,
        IReadOnlyDictionary<string, object?> Tags);

    private sealed record MeasurementSnapshot(
        string InstrumentName,
        double Value,
        IReadOnlyDictionary<string, object?> Tags);
}
