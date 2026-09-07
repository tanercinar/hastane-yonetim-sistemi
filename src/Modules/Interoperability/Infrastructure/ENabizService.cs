using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.ENabiz;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class ENabizService : IENabizService
{
    private readonly InteroperabilityDbContext _dbContext;
    private readonly IIntegrationMockEngine _mockEngine;
    private readonly TimeProvider _timeProvider;

    public ENabizService(
        InteroperabilityDbContext dbContext,
        IIntegrationMockEngine mockEngine,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ENabizTransmissionDto> EnqueuePackageAsync(
        EnqueueENabizPackageDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = new ENabizTransmissionRecord(
            request.PackageType,
            request.PatientId,
            request.PatientNationalId,
            request.HasPatientConsent,
            request.PayloadSummary);

        _dbContext.ENabizTransmissions.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(record);
    }

    public async Task<ENabizTransmissionDto> SendTransmissionAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ENabizTransmissions
            .FirstOrDefaultAsync(t => t.Id == transmissionId, cancellationToken);

        if (record is null)
        {
            throw new KeyNotFoundException($"e-Nabız gönderim kaydı bulunamadı: {transmissionId}");
        }

        if (!record.HasPatientConsent)
        {
            throw new InvalidOperationException("Hasta e-Nabız veri aktarımına rıza göstermediğinden gönderim yapılamaz.");
        }

        record.MarkTransmitting();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.ENabiz,
            $"SendPackage_{(int)record.PackageType}",
            () => Task.FromResult(true),
            payloadSummary: record.PayloadSummary,
            correlationId: $"CORR-ENABIZ-{record.SysTakipNo}",
            cancellationToken: cancellationToken);

        if (result.IsSuccess)
        {
            record.MarkSuccess($"SYS_200_{(int)record.PackageType}", "e-Nabız paketi başarıyla iletildi.");
        }
        else
        {
            record.MarkFailed("ERR_ENABIZ_GATEWAY", result.ErrorMessage ?? "Paket e-Nabız sunucusuna iletilemedi.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(record);
    }

    public async Task<ENabizTransmissionDto> RetryTransmissionAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default)
    {
        return await SendTransmissionAsync(transmissionId, cancellationToken);
    }

    public async Task<List<ENabizTransmissionDto>> QueryTransmissionQueueAsync(
        ENabizTransmissionStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ENabizTransmissions.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(t => t.PatientId == patientId.Value);
        }

        var list = await query.OrderByDescending(t => t.QueuedAtUtc).ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ENabizTransmissionDto?> GetTransmissionByIdAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ENabizTransmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transmissionId, cancellationToken);

        return record is not null ? MapToDto(record) : null;
    }

    private static ENabizTransmissionDto MapToDto(ENabizTransmissionRecord t) =>
        new(
            t.Id,
            t.SysTakipNo,
            (int)t.PackageType,
            t.PackageType.ToString(),
            t.PatientId,
            t.PatientNationalId,
            t.HasPatientConsent,
            t.Status.ToString(),
            t.PayloadSummary,
            t.ResponseCode,
            t.ResponseMessage,
            t.RetryCount,
            t.QueuedAtUtc,
            t.SentAtUtc,
            t.LastAttemptAtUtc);
}
