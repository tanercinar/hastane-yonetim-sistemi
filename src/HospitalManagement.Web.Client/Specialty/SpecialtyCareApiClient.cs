using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;

namespace HospitalManagement.Web.Client.Specialty;

public sealed class SpecialtyCareApiClient : ISpecialtyCareApiClient
{
    private readonly HttpClient _httpClient;

    public SpecialtyCareApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<PregnancyEpisodeResponse>> GetActivePregnancyEpisodesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<PregnancyEpisodeResponse>>(
            "api/v1/specialty/pregnancy-episodes/active",
            cancellationToken);
        return result ?? [];
    }

    public async Task<List<PregnancyEpisodeResponse>> GetPregnancyEpisodesByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<PregnancyEpisodeResponse>>(
            $"api/v1/specialty/pregnancy-episodes/patient/{patientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<PregnancyEpisodeResponse?> GetPregnancyEpisodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<PregnancyEpisodeResponse>(
            $"api/v1/specialty/pregnancy-episodes/{id}",
            cancellationToken);
    }

    public async Task<PregnancyEpisodeResponse> CreatePregnancyEpisodeAsync(CreatePregnancyEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/specialty/pregnancy-episodes")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Gebelik takibi oluşturulamadı.");
    }

    public async Task<AntenatalVisitResponse> RecordAntenatalVisitAsync(Guid episodeId, RecordAntenatalVisitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/pregnancy-episodes/{episodeId}/antenatal-visits")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<AntenatalVisitResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Antenatal vizit kaydedilemedi.");
    }

    public async Task<PregnancyEpisodeResponse> UpdatePregnancyRiskCategoryAsync(Guid episodeId, UpdatePregnancyRiskCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/pregnancy-episodes/{episodeId}/risk-category")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Risk kategorisi güncellenemedi.");
    }

    public async Task<PregnancyEpisodeResponse> CompletePregnancyEpisodeAsync(Guid episodeId, CompletePregnancyEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/pregnancy-episodes/{episodeId}/complete")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Gebelik takibi sonlandırılamadı.");
    }

    public async Task<DeliveryRecordResponse> CreateDeliveryRecordAsync(CreateDeliveryRecordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/specialty/deliveries")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<DeliveryRecordResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Doğum kaydı oluşturulamadı.");
    }

    public async Task<DeliveryRecordResponse?> GetDeliveryRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<DeliveryRecordResponse>(
            $"api/v1/specialty/deliveries/{id}",
            cancellationToken);
    }

    public async Task<List<DeliveryRecordResponse>> GetDeliveryRecordsByMotherPatientIdAsync(Guid motherPatientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<DeliveryRecordResponse>>(
            $"api/v1/specialty/deliveries/mother/{motherPatientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<NewbornResponse> AddNewbornAsync(Guid deliveryId, AddNewbornRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/deliveries/{deliveryId}/newborns")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<NewbornResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Yenidoğan kaydı eklenemedi.");
    }

    public async Task<List<ToothConditionResponse>> GetLatestOdontogramAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ToothConditionResponse>>(
            $"api/v1/specialty/dental/odontogram/{patientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<ToothConditionResponse> RecordToothConditionAsync(Guid patientId, RecordToothConditionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/dental/odontogram/{patientId}/tooth")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var recorded = await response.Content.ReadFromJsonAsync<ToothConditionResponse>(cancellationToken: cancellationToken);
        return recorded ?? throw new InvalidOperationException("Diş durumu kaydedilemedi.");
    }

    public async Task<List<ToothConditionResponse>> GetToothHistoryAsync(Guid patientId, int toothNumber, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ToothConditionResponse>>(
            $"api/v1/specialty/dental/odontogram/{patientId}/tooth/{toothNumber}/history",
            cancellationToken);
        return result ?? [];
    }

    public async Task<List<DentalProcedureResponse>> GetDentalProceduresAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<DentalProcedureResponse>>(
            $"api/v1/specialty/dental/procedures/patient/{patientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<DentalProcedureResponse> PlanDentalProcedureAsync(PlanDentalProcedureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/specialty/dental/procedures")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<DentalProcedureResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Diş tedavisi planlanamadı.");
    }

    public async Task<DentalProcedureResponse> CompleteDentalProcedureAsync(Guid procedureId, CompleteDentalProcedureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/dental/procedures/{procedureId}/complete")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completed = await response.Content.ReadFromJsonAsync<DentalProcedureResponse>(cancellationToken: cancellationToken);
        return completed ?? throw new InvalidOperationException("Diş tedavisi tamamlanamadı.");
    }

    public async Task<List<DentalExaminationResponse>> GetDentalExaminationsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<DentalExaminationResponse>>(
            $"api/v1/specialty/dental/examinations/patient/{patientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<DentalExaminationResponse> CreateDentalExaminationAsync(CreateDentalExaminationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/specialty/dental/examinations")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<DentalExaminationResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Diş muayenesi oluşturulamadı.");
    }

    public async Task<HomeHealthVisitResponse> RequestHomeHealthVisitAsync(RequestHomeHealthVisitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/specialty/home-health/visits")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<HomeHealthVisitResponse>(cancellationToken: cancellationToken);
        return created ?? throw new InvalidOperationException("Evde sağlık ziyareti talep edilemedi.");
    }

    public async Task<List<HomeHealthVisitResponse>> GetActiveHomeHealthVisitsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<HomeHealthVisitResponse>>(
            "api/v1/specialty/home-health/visits/active",
            cancellationToken);
        return result ?? [];
    }

    public async Task<HomeHealthVisitResponse?> GetHomeHealthVisitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<HomeHealthVisitResponse>(
            $"api/v1/specialty/home-health/visits/{id}",
            cancellationToken);
    }

    public async Task<List<HomeHealthVisitResponse>> GetHomeHealthVisitsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<HomeHealthVisitResponse>>(
            $"api/v1/specialty/home-health/visits/patient/{patientId}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<HomeHealthVisitResponse> AssignHomeHealthTeamAsync(Guid id, AssignHomeHealthTeamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/home-health/visits/{id}/assign")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<HomeHealthVisitResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Ziyaret ekibi görevlendirilemedi.");
    }

    public async Task<HomeHealthVisitResponse> StartHomeHealthVisitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/home-health/visits/{id}/start");

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<HomeHealthVisitResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Ziyaret başlatılamadı.");
    }

    public async Task<HomeHealthVisitResponse> CompleteHomeHealthVisitAsync(Guid id, CompleteHomeHealthVisitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/home-health/visits/{id}/complete")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<HomeHealthVisitResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Ziyaret tamamlanamadı.");
    }

    public async Task<HomeHealthVisitResponse> CancelHomeHealthVisitAsync(Guid id, CancelHomeHealthVisitRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/specialty/home-health/visits/{id}/cancel")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<HomeHealthVisitResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Ziyaret iptal edilemedi.");
    }

    public async Task<SpecialtyOperationalSummaryResponse> GetSpecialtyOperationalSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/specialty/reports/operational-summary";
        var queryParams = new List<string>();
        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");
        }

        if (queryParams.Count > 0)
        {
            url = $"{url}?{string.Join("&", queryParams)}";
        }

        var result = await _httpClient.GetFromJsonAsync<SpecialtyOperationalSummaryResponse>(url, cancellationToken);
        return result ?? new SpecialtyOperationalSummaryResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow);
    }
}
