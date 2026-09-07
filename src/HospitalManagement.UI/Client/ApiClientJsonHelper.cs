using System.Net.Http.Json;
using System.Text.Json;

namespace HospitalManagement.UI.Client;

/// <summary>
/// Helper for typed HTTP responses and RFC 9457 Problem Details error parsing.
/// </summary>
public static class ApiClientJsonHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Ensures success status code or extracts RFC 9457 Problem Details into an ApiException.
    /// </summary>
    public static async Task EnsureSuccessOrThrowProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetailsDto? problemDetails = null;
        try
        {
            if (response.Content.Headers.ContentLength > 0 ||
                response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(
                    JsonOptions,
                    cancellationToken);
            }
        }
        catch
        {
            // Fallback if content cannot be parsed as JSON ProblemDetails
        }

        throw ApiException.FromProblemDetails(response.StatusCode, problemDetails);
    }

    /// <summary>
    /// Reads JSON payload or parses Problem Details on error.
    /// </summary>
    public static async Task<T> ReadContentOrThrowAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        await EnsureSuccessOrThrowProblemDetailsAsync(response, cancellationToken);

        var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (content is null)
        {
            throw new ApiException(
                response.StatusCode,
                "Sunucu beklenmeyen boş bir yanıt döndürdü.");
        }

        return content;
    }
}
