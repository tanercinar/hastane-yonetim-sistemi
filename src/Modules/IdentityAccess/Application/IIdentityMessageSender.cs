namespace HospitalManagement.Modules.IdentityAccess.Application;

public enum IdentityMessageKind
{
    PatientEmailConfirmation = 1,
    PasswordReset = 2,
    StaffInvitation = 3,
}

public sealed record IdentityMessage(
    IdentityMessageKind Kind,
    string RecipientAddress,
    string ActionCode,
    DateTime ExpiresAtUtc);

public interface IIdentityMessageSender
{
    Task SendAsync(
        IdentityMessage message,
        CancellationToken cancellationToken = default);
}
