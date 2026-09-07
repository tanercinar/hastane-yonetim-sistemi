using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface ICriticalResultNotificationService
{
    Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> AcknowledgeAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        string acknowledgmentNotes,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> EscalateAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        string escalationReason,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<CriticalResultNotificationDto>>> GetActiveNotificationsAsync(
        ClaimsPrincipal actor,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<CriticalResultNotificationDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task ProcessAndNotifyCriticalResultsAsync(
        LabResult labResult,
        CancellationToken cancellationToken = default);
}
