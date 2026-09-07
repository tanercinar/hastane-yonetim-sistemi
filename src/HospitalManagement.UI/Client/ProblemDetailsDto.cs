using System.Net;
using System.Text.Json.Serialization;

namespace HospitalManagement.UI.Client;

/// <summary>
/// RFC 9457 / RFC 7807 Problem Details representation for API errors.
/// </summary>
public sealed class ProblemDetailsDto
{
    [JsonPropertyName("type")]
    public string? Type
    {
        get; set;
    }

    [JsonPropertyName("title")]
    public string? Title
    {
        get; set;
    }

    [JsonPropertyName("status")]
    public int? Status
    {
        get; set;
    }

    [JsonPropertyName("detail")]
    public string? Detail
    {
        get; set;
    }

    [JsonPropertyName("instance")]
    public string? Instance
    {
        get; set;
    }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors
    {
        get; set;
    }
}

/// <summary>
/// Structured exception thrown when API calls return non-successful HTTP status codes.
/// Extracts safe RFC 9457 Problem Details while preventing internal sensitive data leakage.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode
    {
        get;
    }

    public ProblemDetailsDto? ProblemDetails
    {
        get;
    }

    public ApiException(
        HttpStatusCode statusCode,
        string message,
        ProblemDetailsDto? problemDetails = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }

    public static ApiException FromProblemDetails(
        HttpStatusCode statusCode,
        ProblemDetailsDto? problemDetails)
    {
        var message = problemDetails?.Detail ??
                      problemDetails?.Title ??
                      $"API çağrısı başarısız oldu (HTTP {(int)statusCode}).";

        return new ApiException(statusCode, message, problemDetails);
    }
}
