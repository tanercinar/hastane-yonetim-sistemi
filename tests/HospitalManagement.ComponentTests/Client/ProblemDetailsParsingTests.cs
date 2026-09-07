using System.Net;
using System.Text;
using HospitalManagement.UI.Client;

using Xunit;

namespace HospitalManagement.ComponentTests.Client;

public sealed class ProblemDetailsParsingTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    public async Task ReadContentOrThrowAsyncShouldParseRfc9457ProblemDetailsOn400()
    {
        const string problemJson = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            "title": "One or more validation errors occurred.",
            "status": 400,
            "detail": "Kullanıcı girdisi doğrulanamadı.",
            "errors": {
                "Email": ["Geçerli bir e-posta adresi giriniz."]
            }
        }
        """;

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
        };

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            ApiClientJsonHelper.ReadContentOrThrowAsync<string>(response));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Kullanıcı girdisi doğrulanamadı.", exception.Message);
        Assert.NotNull(exception.ProblemDetails);
        Assert.Equal(400, exception.ProblemDetails.Status);
        Assert.True(exception.ProblemDetails.Errors?.ContainsKey("Email"));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    public async Task ReadContentOrThrowAsyncShouldFallbackGracefullyOnNonJsonError()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Raw server error", Encoding.UTF8, "text/plain")
        };

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            ApiClientJsonHelper.ReadContentOrThrowAsync<string>(response));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal("API çağrısı başarısız oldu (HTTP 500).", exception.Message);
        Assert.Null(exception.ProblemDetails);
    }
}
