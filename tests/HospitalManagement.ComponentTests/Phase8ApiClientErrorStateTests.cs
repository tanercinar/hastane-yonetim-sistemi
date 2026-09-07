using System.Net;

using HospitalManagement.Web.Client.Emergency;
using HospitalManagement.Web.Client.Surgery;

namespace HospitalManagement.ComponentTests;

public sealed class Phase8ApiClientErrorStateTests
{
    [Theory]
    [InlineData("emergency")]
    [InlineData("surgery")]
    [InlineData("icu")]
    [InlineData("handoff")]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task ForbiddenClinicalWorklistIsNotRenderedAsAnEmptyState(string clientName)
    {
        using var httpClient = new HttpClient(new ForbiddenHandler())
        {
            BaseAddress = new Uri("https://hospital.invalid/"),
        };

        var exception = clientName switch
        {
            "emergency" => await Assert.ThrowsAsync<HttpRequestException>(
                () => new EmergencyApiClient(httpClient).GetAdmissionsAsync()),
            "surgery" => await Assert.ThrowsAsync<HttpRequestException>(
                () => new SurgeryApiClient(httpClient).GetBookingsAsync()),
            "icu" => await Assert.ThrowsAsync<HttpRequestException>(
                () => new IcuApiClient(httpClient).GetIcuBedsAsync()),
            "handoff" => await Assert.ThrowsAsync<HttpRequestException>(
                () => new ClinicalHandoffApiClient(httpClient).GetPendingHandoffsAsync()),
            _ => throw new ArgumentOutOfRangeException(nameof(clientName)),
        };

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    private sealed class ForbiddenHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                RequestMessage = request,
            });
    }
}
