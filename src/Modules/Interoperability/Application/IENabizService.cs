using HospitalManagement.Modules.Interoperability.Domain.ENabiz;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record ENabizTransmissionDto(
    Guid Id,
    string SysTakipNo,
    int PackageTypeCode,
    string PackageTypeName,
    Guid PatientId,
    string PatientNationalId,
    bool HasPatientConsent,
    string Status,
    string PayloadSummary,
    string ResponseCode,
    string ResponseMessage,
    int RetryCount,
    DateTime QueuedAtUtc,
    DateTime? SentAtUtc,
    DateTime? LastAttemptAtUtc);

public sealed record EnqueueENabizPackageDto(
    ENabizPackageType PackageType,
    Guid PatientId,
    string PatientNationalId,
    bool HasPatientConsent,
    string PayloadSummary);

public interface IENabizService
{
    Task<ENabizTransmissionDto> EnqueuePackageAsync(
        EnqueueENabizPackageDto request,
        CancellationToken cancellationToken = default);

    Task<ENabizTransmissionDto> SendTransmissionAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default);

    Task<ENabizTransmissionDto> RetryTransmissionAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default);

    Task<List<ENabizTransmissionDto>> QueryTransmissionQueueAsync(
        ENabizTransmissionStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<ENabizTransmissionDto?> GetTransmissionByIdAsync(
        Guid transmissionId,
        CancellationToken cancellationToken = default);
}
