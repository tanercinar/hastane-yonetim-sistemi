using HospitalManagement.Host.Emergency;
using HospitalManagement.Host.Realtime;

using Microsoft.AspNetCore.SignalR;

using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class EmergencyRealtimePrivacyTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task EmergencyRealtimeEventsContainOnlyRefreshTimestamp()
    {
        var proxy = new CapturingClientProxy();
        var notifier = new EmergencyRealtimeNotifier(new CapturingHubContext(proxy));
        var canaryId = Guid.NewGuid();
        const string canary = "DEMO-CLINICAL-CANARY-DO-NOT-PUBLISH";

        await notifier.NotifyAdmissionCreatedAsync(canaryId, canary, canary);
        await notifier.NotifyTriageRecordedAsync(canaryId, canary, canary);
        await notifier.NotifyDoctorAssignedAsync(canaryId, canary, canaryId, canary);
        await notifier.NotifyStatusChangedAsync(canaryId, canary, canary, canary);
        await notifier.NotifyDashboardUpdatedAsync();

        Assert.Equal(9, proxy.Calls.Count);
        Assert.All(proxy.Calls, call =>
        {
            var payload = Assert.Single(call.Arguments);
            Assert.NotNull(payload);
            var property = Assert.Single(payload.GetType().GetProperties());
            Assert.Equal("ChangedAtUtc", property.Name);
            Assert.IsType<DateTime>(property.GetValue(payload));
        });
    }

    private sealed class CapturingHubContext(CapturingClientProxy proxy) : IHubContext<HospitalHub>
    {
        public IHubClients Clients { get; } = new CapturingHubClients(proxy);

        public IGroupManager Groups { get; } = new NoOpGroupManager();
    }

    private sealed class CapturingHubClients(CapturingClientProxy proxy) : IHubClients
    {
        public IClientProxy All => proxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Client(string connectionId) => proxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;

        public IClientProxy Group(string groupName) => proxy;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;

        public IClientProxy User(string userId) => proxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
    }

    private sealed class CapturingClientProxy : IClientProxy
    {
        public List<CapturedCall> Calls { get; } = [];

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new CapturedCall(method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record CapturedCall(string Method, object?[] Arguments);
}
