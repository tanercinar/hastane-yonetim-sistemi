using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Reporting;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Host.Reporting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class ReportingSignalRTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public void IsAuthorizedForDepartmentReturnsFalseForUnscopedAdministrativeRoles()
    {
        var admin = CreateUserWithRole(HospitalRoles.SystemAdministrator);
        var cmo = CreateUserWithRole(HospitalRoles.ChiefMedicalOfficer);
        var manager = CreateUserWithRole(HospitalRoles.HospitalManager);
        var deptId = Guid.NewGuid();

        Assert.False(HospitalHub.IsAuthorizedForDepartment(admin, deptId));
        Assert.False(HospitalHub.IsAuthorizedForDepartment(cmo, deptId));
        Assert.False(HospitalHub.IsAuthorizedForDepartment(manager, deptId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public void IsAuthorizedForDepartmentReturnsFalseForUnscopedReportPermission()
    {
        var user = CreateUserWithPermissions(HospitalPermissions.ReportingAndAudit.ReportOperationsView);
        var deptId = Guid.NewGuid();

        Assert.False(HospitalHub.IsAuthorizedForDepartment(user, deptId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public void IsAuthorizedForDepartmentReturnsTrueWhenDepartmentIdMatchesClaim()
    {
        var deptId = Guid.NewGuid();
        var user = CreateUserWithDepartmentAndPermissions(
            deptId,
            HospitalPermissions.ReportingAndAudit.ReportOperationsView);

        Assert.True(HospitalHub.IsAuthorizedForDepartment(user, deptId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public void IsAuthorizedForDepartmentReturnsFalseWhenDoctorTriesDifferentDepartment()
    {
        var userDeptId = Guid.NewGuid();
        var foreignDeptId = Guid.NewGuid();
        var user = CreateUserWithDepartmentAndRole(userDeptId, HospitalRoles.Doctor);

        Assert.False(HospitalHub.IsAuthorizedForDepartment(user, foreignDeptId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public void IsAuthorizedForDepartmentReturnsFalseWhenUserHasNoDepartmentOrPermission()
    {
        var user = CreateUserWithRole(HospitalRoles.Nurse);
        var deptId = Guid.NewGuid();

        Assert.False(HospitalHub.IsAuthorizedForDepartment(user, deptId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    [Trait("Roadmap", "F13-G02")]
    public async Task JoinDepartmentGroupThrowsHubExceptionWhenUnauthorized()
    {
        var userDeptId = Guid.NewGuid();
        var foreignDeptId = Guid.NewGuid();
        var user = CreateUserWithDepartmentRoleAndPermissions(
            userDeptId,
            HospitalRoles.Doctor,
            HospitalPermissions.ReportingAndAudit.ReportOperationsView);

        var groups = new RecordingGroupManager();
        var hub = new HospitalHub
        {
            Context = new TestHubCallerContext(user),
            Groups = groups,
        };

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinDepartmentGroup(foreignDeptId));
        Assert.Contains("Yetkisiz", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(groups.AddedGroups);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task JoinDepartmentGroupAddsToGroupWhenAuthorized()
    {
        var deptId = Guid.NewGuid();
        var user = CreateUserWithDepartmentAndPermissions(
            deptId,
            HospitalPermissions.ReportingAndAudit.ReportOperationsView);

        var groups = new RecordingGroupManager();
        var hub = new HospitalHub
        {
            Context = new TestHubCallerContext(user),
            Groups = groups,
        };

        await hub.JoinDepartmentGroup(deptId);

        var added = Assert.Single(groups.AddedGroups);
        Assert.Equal($"department-{deptId}", added.GroupName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task JoinReportingDashboardThrowsHubExceptionWhenLacksPermission()
    {
        var user = CreateUserWithRole(HospitalRoles.Doctor);

        var groups = new RecordingGroupManager();
        var hub = new HospitalHub
        {
            Context = new TestHubCallerContext(user),
            Groups = groups,
        };

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinReportingDashboard("outpatient"));
        Assert.Contains("Yetkisiz", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(groups.AddedGroups);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task JoinReportingDashboardAddsToGroupWhenHasPermission()
    {
        var user = CreateUserWithPermissions(HospitalPermissions.ReportingAndAudit.ReportOperationsView);

        var groups = new RecordingGroupManager();
        var hub = new HospitalHub
        {
            Context = new TestHubCallerContext(user),
            Groups = groups,
        };

        await hub.JoinReportingDashboard("outpatient");

        var added = Assert.Single(groups.AddedGroups);
        Assert.Equal("reporting-outpatient", added.GroupName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task ReportingRealtimeNotifierIncrementsSequenceNumberMonotonically()
    {
        var proxy = new CapturingClientProxy();
        var hubContext = new CapturingHubContext(proxy);
        var notifier = new ReportingRealtimeNotifier(hubContext);

        var date = new DateOnly(2026, 9, 4);

        await notifier.NotifyDashboardUpdatedAsync("outpatient", date);
        await notifier.NotifyDashboardUpdatedAsync("outpatient", date);

        var updates = proxy.Calls
            .Where(c => c.Method == ReportingRealtimeNotifier.HubMethodName)
            .Select(c => c.Arguments.OfType<ReportingDashboardRealtimeUpdate>().FirstOrDefault())
            .Where(u => u is not null)
            .Cast<ReportingDashboardRealtimeUpdate>()
            .ToList();

        var distinctSequences = updates
            .Select(u => u.SequenceNumber)
            .Distinct()
            .ToList();

        Assert.True(distinctSequences.Count >= 2);
        Assert.True(distinctSequences[1] > distinctSequences[0]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task ReportingRealtimeNotifierSendsToExpectedGroups()
    {
        var targetedGroups = new List<string>();
        var proxy = new CapturingClientProxy();
        var hubClients = new TrackingHubClients(proxy, targetedGroups);
        var hubContext = new TrackingHubContext(hubClients);
        var notifier = new ReportingRealtimeNotifier(hubContext);

        var deptId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);

        await notifier.NotifyDashboardUpdatedAsync("outpatient", date, deptId);

        Assert.Contains(ReportingRealtimeNotifier.OperationsGroup, targetedGroups);
        Assert.Contains("reporting-outpatient", targetedGroups);
        Assert.Contains($"department-{deptId}", targetedGroups);
    }

    private static ClaimsPrincipal CreateUserWithRole(string role) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
        ], "TestAuth"));

    private static ClaimsPrincipal CreateUserWithPermissions(params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };
        claims.AddRange(permissions.Select(p => new Claim(HospitalClaimTypes.Permission, p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static ClaimsPrincipal CreateUserWithDepartment(Guid departmentId) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(HospitalClaimTypes.DepartmentId, departmentId.ToString()),
        ], "TestAuth"));

    private static ClaimsPrincipal CreateUserWithDepartmentAndPermissions(
        Guid departmentId,
        params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(HospitalClaimTypes.DepartmentId, departmentId.ToString()),
        };
        claims.AddRange(permissions.Select(permission =>
            new Claim(HospitalClaimTypes.Permission, permission)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static ClaimsPrincipal CreateUserWithDepartmentAndRole(Guid departmentId, string role) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(HospitalClaimTypes.DepartmentId, departmentId.ToString()),
        ], "TestAuth"));

    private static ClaimsPrincipal CreateUserWithDepartmentRoleAndPermissions(
        Guid departmentId,
        string role,
        params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role),
            new(HospitalClaimTypes.DepartmentId, departmentId.ToString()),
        };
        claims.AddRange(permissions.Select(permission =>
            new Claim(HospitalClaimTypes.Permission, permission)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private sealed class TestHubCallerContext(ClaimsPrincipal user, string connectionId = "conn-1") : HubCallerContext
    {
        public override string ConnectionId => connectionId;
        public override ClaimsPrincipal? User => user;
        public override string? UserIdentifier => user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override void Abort()
        {
        }
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> AddedGroups { get; } = [];
        public List<(string ConnectionId, string GroupName)> RemovedGroups { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            RemovedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingHubContext(CapturingClientProxy proxy) : IHubContext<HospitalHub>
    {
        public IHubClients Clients { get; } = new SimpleHubClients(proxy);
        public IGroupManager Groups { get; } = new RecordingGroupManager();
    }

    private sealed class SimpleHubClients(CapturingClientProxy proxy) : IHubClients
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

    private sealed class TrackingHubContext(IHubClients clients) : IHubContext<HospitalHub>
    {
        public IHubClients Clients { get; } = clients;
        public IGroupManager Groups { get; } = new RecordingGroupManager();
    }

    private sealed class TrackingHubClients(CapturingClientProxy proxy, List<string> targetedGroups) : IHubClients
    {
        public IClientProxy All => proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Client(string connectionId) => proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;
        public IClientProxy Group(string groupName)
        {
            targetedGroups.Add(groupName);
            return proxy;
        }
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            targetedGroups.AddRange(groupNames);
            return proxy;
        }
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

    private sealed record CapturedCall(string Method, object?[] Arguments);
}
