using System.Collections.Concurrent;

using HospitalManagement.Modules.IdentityAccess.Application;

namespace HospitalManagement.IntegrationTests.Infrastructure;

internal sealed class InMemoryIdentityMessageSender : IIdentityMessageSender
{
    private readonly ConcurrentQueue<IdentityMessage> _messages = new();

    internal IReadOnlyCollection<IdentityMessage> Messages => _messages.ToArray();

    public Task SendAsync(
        IdentityMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    internal IdentityMessage Latest(IdentityMessageKind kind, string recipientAddress) =>
        _messages.Last(message =>
            message.Kind == kind
            && string.Equals(
                message.RecipientAddress,
                recipientAddress,
                StringComparison.OrdinalIgnoreCase));
}
