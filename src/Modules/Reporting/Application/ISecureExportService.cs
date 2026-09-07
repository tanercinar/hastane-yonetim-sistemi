namespace HospitalManagement.Modules.Reporting.Application;

public sealed record SecureExportRequestDto(
    string ReportType,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    Guid? DepartmentId = null);

public sealed record SecureExportResultDto(
    string FileName,
    string ContentType,
    byte[] Content,
    int RowCount);

public interface ISecureExportService
{
    Task<SecureExportResultDto> ExportCsvAsync(
        SecureExportRequestDto request,
        Guid? actorUserId,
        Guid? actorPersonId,
        string? actorRole,
        string correlationId,
        CancellationToken cancellationToken = default);
}
