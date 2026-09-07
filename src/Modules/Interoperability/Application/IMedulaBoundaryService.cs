using HospitalManagement.Modules.Interoperability.Domain.Medula;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record MedulaOperationResultDto(
    Guid Id,
    string OperationType,
    string Status,
    string StatusDescription,
    string DemoDisclaimer,
    string RequestSummary,
    string ResponseSummary,
    DateTime ProcessedAtUtc);

public interface IMedulaBoundaryService
{
    /// <summary>
    /// Demo MEDULA/SGK işlemi çalıştırır. Gerçek sunucuya bağlanmaz.
    /// </summary>
    Task<MedulaOperationResultDto> ExecuteDemoOperationAsync(
        MedulaOperationType operationType,
        string requestSummary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kapsam dışı finansal işlem için açık red yanıtı döndürür.
    /// </summary>
    Task<MedulaOperationResultDto> RejectOutOfScopeAsync(
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm MEDULA/SGK/İTS/ÜTS işlem türlerinin demo sınır bilgisini döndürür.
    /// </summary>
    Task<List<MedulaOperationResultDto>> GetBoundaryInfoAsync(
        CancellationToken cancellationToken = default);
}
