using System.Net;
using System.Net.Mail;

using HospitalManagement.Host.Configuration;
using HospitalManagement.Modules.IdentityAccess.Application;

using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Identity;

public sealed class MockSmtpIdentityMessageSender(
    IOptions<EmailDeliveryOptions> options) : IIdentityMessageSender
{
    private readonly EmailDeliveryOptions _options = options.Value;

    public async Task SendAsync(
        IdentityMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var mailMessage = new MailMessage(
            new MailAddress(_options.SenderAddress),
            new MailAddress(message.RecipientAddress))
        {
            Subject = GetSubject(message.Kind),
            Body = $"""
                MOCK / DEMO kimlik bildirimi

                İşlem kodu: {message.ActionCode}
                Son kullanım (UTC): {message.ExpiresAtUtc:O}

                Bu kodu yalnız Hastane Yönetim Sistemi DEMO ekranındaki ilgili forma girin.
                Kod URL içinde taşınmaz ve gerçek bir kuruma gönderilmez.
                """,
            IsBodyHtml = false,
        };
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = false,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = CredentialCache.DefaultNetworkCredentials,
        };

        await client.SendMailAsync(mailMessage, cancellationToken);
    }

    private static string GetSubject(IdentityMessageKind kind) => kind switch
    {
        IdentityMessageKind.PatientEmailConfirmation => "MOCK — DEMO hesap doğrulama kodu",
        IdentityMessageKind.PasswordReset => "MOCK — DEMO parola sıfırlama kodu",
        IdentityMessageKind.StaffInvitation => "MOCK — DEMO personel davet kodu",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
