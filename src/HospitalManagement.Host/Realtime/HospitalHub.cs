using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Realtime;

[Authorize]
public sealed class HospitalHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user is not null)
        {
            var personIdStr = user.FindFirst(HospitalClaimTypes.PersonId)?.Value;
            if (Guid.TryParse(personIdStr, out var personId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"person-{personId}");
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role == HospitalRoles.RegistrationStaff || role == HospitalRoles.SystemAdministrator)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff-queue");
            }
            else if (role == HospitalRoles.Doctor && Guid.TryParse(personIdStr, out _))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor-{personId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff-queue");
            }

            if (HasAnyInpatientPermission(user))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "inpatient-staff");
            }

            if (HasAnyEmergencyPermission(user))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "emergency-staff");
            }

            var deptIdStr = user.FindFirst(HospitalClaimTypes.DepartmentId)?.Value;
            if (Guid.TryParse(deptIdStr, out var deptId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"department-{deptId}");
            }

            if (user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.ReportingAndAudit.ReportOperationsView))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "reporting-operations");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Context.User;
        if (user is not null)
        {
            var personIdStr = user.FindFirst(HospitalClaimTypes.PersonId)?.Value;
            if (Guid.TryParse(personIdStr, out var personId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"person-{personId}");
            }

            var deptIdStr = user.FindFirst(HospitalClaimTypes.DepartmentId)?.Value;
            if (Guid.TryParse(deptIdStr, out var deptId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department-{deptId}");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "staff-queue");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "inpatient-staff");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "emergency-staff");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "reporting-operations");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinDepartmentGroup(Guid departmentId)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Oturum açılması zorunludur.");
        }

        if (!IsAuthorizedForDepartment(user, departmentId))
        {
            throw new HubException("Yetkisiz bölüm grubu erişimi: Kullanıcı belirtilen bölüme erişim yetkisine sahip değil.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"department-{departmentId}");
    }

    public async Task LeaveDepartmentGroup(Guid departmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department-{departmentId}");
    }

    public async Task JoinReportingDashboard(string dashboardName)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Oturum açılması zorunludur.");
        }

        if (!user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            && !user.IsInRole(HospitalRoles.SystemAdministrator)
            && !user.IsInRole(HospitalRoles.ChiefMedicalOfficer)
            && !user.IsInRole(HospitalRoles.HospitalManager))
        {
            throw new HubException("Yetkisiz raporlama panosu erişimi: Raporlama panolarına erişim yetkiniz bulunmamaktadır.");
        }

        var normalized = dashboardName?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new HubException("Geçersiz pano adı.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"reporting-{normalized}");
    }

    public async Task LeaveReportingDashboard(string dashboardName)
    {
        var normalized = dashboardName?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"reporting-{normalized}");
        }
    }

    public static bool IsAuthorizedForDepartment(ClaimsPrincipal user, Guid departmentId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (departmentId == Guid.Empty
            || user.Identity?.IsAuthenticated != true
            || !user.HasClaim(
                HospitalClaimTypes.Permission,
                HospitalPermissions.ReportingAndAudit.ReportOperationsView))
        {
            return false;
        }

        var userDepartmentId = user.FindFirst(HospitalClaimTypes.DepartmentId)?.Value;
        return Guid.TryParse(userDepartmentId, out var parsedDeptId)
            && parsedDeptId == departmentId;
    }

    private static bool HasAnyInpatientPermission(ClaimsPrincipal user) =>
        user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.AdmissionRequest)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.AdmissionAccept)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.BedAssign)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.BedTransfer)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.CarePlanManage)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.MedicationAdminister)
        || user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.DischargeComplete);

    private static bool HasAnyEmergencyPermission(ClaimsPrincipal user) =>
        user.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Inpatient.EmergencyTriageRecord);
}
