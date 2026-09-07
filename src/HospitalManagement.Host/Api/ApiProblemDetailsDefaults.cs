using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Api;

public static class ApiProblemDetailsDefaults
{
    private const string ProblemTypeBaseUri = "https://hospital-management.invalid/problems/";

    public static void Customize(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Apply(context.HttpContext, context.ProblemDetails);
    }

    public static void Apply(HttpContext httpContext, ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problemDetails);

        var status = problemDetails.Status ?? httpContext.Response.StatusCode;
        if (status < StatusCodes.Status400BadRequest)
        {
            status = StatusCodes.Status500InternalServerError;
        }

        var code = problemDetails.Extensions.TryGetValue("code", out var configuredCode)
            && configuredCode is string configuredCodeValue
            && !string.IsNullOrWhiteSpace(configuredCodeValue)
                ? configuredCodeValue
                : GetCode(problemDetails, status);
        var correlationId = httpContext.TraceIdentifier;

        problemDetails.Status = status;
        NormalizeValidationFieldNames(problemDetails);
        problemDetails.Type = $"{ProblemTypeBaseUri}{code}";
        problemDetails.Title = GetTitle(code);
        problemDetails.Detail = GetSafeDetail(code);
        problemDetails.Instance = $"urn:hospital-management:request:{correlationId}";
        problemDetails.Extensions.Remove("traceId");
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["correlationId"] = correlationId;
    }

    private static void NormalizeValidationFieldNames(ProblemDetails problemDetails)
    {
        if (problemDetails is not HttpValidationProblemDetails validationProblem)
        {
            return;
        }

        var normalizedErrors = validationProblem.Errors
            .GroupBy(
                error => JsonNamingPolicy.CamelCase.ConvertName(error.Key),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(error => error.Value)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        validationProblem.Errors.Clear();
        foreach (var error in normalizedErrors)
        {
            validationProblem.Errors[error.Key] = error.Value;
        }
    }

    private static string GetCode(ProblemDetails problemDetails, int status)
    {
        if (problemDetails is HttpValidationProblemDetails)
        {
            return "validation_failed";
        }

        return status switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status401Unauthorized => "authentication_required",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status413PayloadTooLarge => "payload_too_large",
            StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
            StatusCodes.Status422UnprocessableEntity => "unprocessable_entity",
            StatusCodes.Status429TooManyRequests => "rate_limit_exceeded",
            >= StatusCodes.Status500InternalServerError => "server_error",
            _ => "http_error",
        };
    }

    private static string GetTitle(string code)
    {
        return code switch
        {
            "validation_failed" => "Doğrulama başarısız",
            "bad_request" => "Geçersiz istek",
            "authentication_required" => "Kimlik doğrulama gerekli",
            "forbidden" => "Erişim reddedildi",
            "not_found" => "Kaynak bulunamadı",
            "method_not_allowed" => "HTTP metodu desteklenmiyor",
            "conflict" => "İstek mevcut durumla çakışıyor",
            "payload_too_large" => "İstek gövdesi çok büyük",
            "unsupported_media_type" => "İçerik türü desteklenmiyor",
            "unprocessable_entity" => "İstek işlenemedi",
            "rate_limit_exceeded" => "İstek sınırı aşıldı",
            "server_error" => "Sunucu hatası",
            _ => "HTTP isteği tamamlanamadı",
        };
    }

    private static string GetSafeDetail(string code)
    {
        return code switch
        {
            "validation_failed" => "Bir veya daha fazla alan geçersiz.",
            "authentication_required" => "Bu işlem için kimlik doğrulama gereklidir.",
            "forbidden" => "Bu kaynağa erişim izniniz yoktur.",
            "not_found" => "İstenen kaynak bulunamadı.",
            "method_not_allowed" => "Bu kaynak belirtilen HTTP metodunu kabul etmiyor.",
            "conflict" => "Kaynağın güncel durumunu alıp isteği yeniden değerlendirin.",
            "rate_limit_exceeded" => "Daha sonra yeniden deneyin.",
            "server_error" => "İstek işlenirken beklenmeyen bir hata oluştu.",
            _ => "İstek güvenli biçimde işlenemedi.",
        };
    }
}
