using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Medula;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class MedulaBoundaryService : IMedulaBoundaryService
{
    private readonly IIntegrationMockEngine _mockEngine;

    public MedulaBoundaryService(IIntegrationMockEngine mockEngine)
    {
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
    }

    public async Task<MedulaOperationResultDto> ExecuteDemoOperationAsync(
        MedulaOperationType operationType,
        string requestSummary,
        CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Medula,
            $"DemoOperation_{operationType}",
            () => Task.FromResult(MedulaOperationResult.CreateDemoResponse(operationType, requestSummary)),
            payloadSummary: $"MEDULA demo: {operationType}",
            correlationId: $"CORR-MEDULA-{Guid.NewGuid():N}"[..28],
            cancellationToken: cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return MapToDto(result.Value);
        }

        // Mock engine returned a failure — still return a valid demo response
        var fallback = MedulaOperationResult.CreateDemoResponse(operationType, requestSummary);
        return MapToDto(fallback);
    }

    public Task<MedulaOperationResultDto> RejectOutOfScopeAsync(
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var rejection = MedulaOperationResult.CreateOutOfScopeRejection(operationName);
        return Task.FromResult(MapToDto(rejection));
    }

    public Task<List<MedulaOperationResultDto>> GetBoundaryInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<MedulaOperationResultDto>();

        foreach (var opType in Enum.GetValues<MedulaOperationType>())
        {
            var info = MedulaOperationResult.CreateDemoResponse(opType, "{}");
            results.Add(MapToDto(info));
        }

        return Task.FromResult(results);
    }

    private static MedulaOperationResultDto MapToDto(MedulaOperationResult r) =>
        new(
            r.Id,
            r.OperationType.ToString(),
            r.Status.ToString(),
            r.StatusDescription,
            r.DemoDisclaimer,
            r.RequestSummary,
            r.ResponseSummary,
            r.ProcessedAtUtc);
}
