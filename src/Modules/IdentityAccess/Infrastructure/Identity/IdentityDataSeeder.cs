using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

public sealed partial class IdentityDataSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IdentityAccessDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<IdentityDataSeeder> logger) : IIdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly IdentityAccessDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IdentityDataSeeder> _logger = logger;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deterministik DEMO kullanicilari ve rolleri basariyla seed edildi.")]
    private static partial void LogSeedCompleted(ILogger logger);

    public static readonly IReadOnlyList<DemoUserDefinition> DemoUsers =
    [
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("00000000-0000-0000-0000-000000000101"),
            "DEMO-admin@hospital.invalid",
            "DEMO-Admin-Pass!1",
            HospitalRoles.SystemAdministrator,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Guid.Parse("00000000-0000-0000-0000-000000000102"),
            "DEMO-doctor@hospital.invalid",
            "DEMO-Doc-Pass!1",
            HospitalRoles.Doctor,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Guid.Parse("00000000-0000-0000-0000-000000000103"),
            "DEMO-nurse@hospital.invalid",
            "DEMO-Nurse-Pass!1",
            HospitalRoles.Nurse,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Guid.Parse("00000000-0000-0000-0000-000000000104"),
            "DEMO-pharmacist@hospital.invalid",
            "DEMO-Pharm-Pass!1",
            HospitalRoles.Pharmacist,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000005"),
            Guid.Parse("00000000-0000-0000-0000-000000000105"),
            "DEMO-labtech@hospital.invalid",
            "DEMO-LabTech-Pass!1",
            HospitalRoles.LaboratoryStaff,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000006"),
            Guid.Parse("00000000-0000-0000-0000-000000000106"),
            "DEMO-radtech@hospital.invalid",
            "DEMO-RadTech-Pass!1",
            HospitalRoles.RadiologyStaff,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000007"),
            Guid.Parse("00000000-0000-0000-0000-000000000107"),
            "DEMO-receptionist@hospital.invalid",
            "DEMO-Recep-Pass!1",
            HospitalRoles.RegistrationStaff,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000008"),
            Guid.Parse("00000000-0000-0000-0000-000000000108"),
            "DEMO-chief@hospital.invalid",
            "DEMO-Chief-Pass!1",
            HospitalRoles.ChiefMedicalOfficer,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000009"),
            Guid.Parse("00000000-0000-0000-0000-000000000109"),
            "DEMO-patient@hospital.invalid",
            "DEMO-Patient-Pass!1",
            HospitalRoles.Patient,
            IsStaff: false),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            Guid.Parse("00000000-0000-0000-0000-000000000110"),
            "DEMO-manager@hospital.invalid",
            "DEMO-Manager-Pass!1",
            HospitalRoles.HospitalManager,
            IsStaff: true),
        new DemoUserDefinition(
            Guid.Parse("00000000-0000-0000-0000-000000000011"),
            Guid.Parse("00000000-0000-0000-0000-000000000111"),
            "DEMO-anesthesiologist@hospital.invalid",
            "DEMO-Anesth-Pass!1",
            HospitalRoles.Doctor,
            IsStaff: true),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // 1. Rolleri ve varsayılan izin claim'lerini seed et (Idempotent)
        foreach (var (roleName, permissions) in RolePermissionDefaults.GetAllRolePermissions())
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new IdentityRole<Guid>(roleName) { NormalizedName = roleName.ToUpperInvariant() };
                var createRoleResult = await _roleManager.CreateAsync(role);
                if (!createRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", createRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Rol '{roleName}' oluşturulamadı: {errors}");
                }
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingPermissionClaims = existingClaims
                .Where(c => c.Type == HospitalClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var permission in permissions)
            {
                if (!existingPermissionClaims.Contains(permission))
                {
                    await _roleManager.AddClaimAsync(role, new Claim(HospitalClaimTypes.Permission, permission));
                }
            }
        }

        // 2. Sentetik demo kullanıcılarını seed et (Idempotent)
        foreach (var def in DemoUsers)
        {
            var existingUser = await _userManager.FindByEmailAsync(def.Email)
                ?? await _userManager.FindByIdAsync(def.UserId.ToString());

            if (existingUser is null)
            {
                ApplicationUser user;
                if (def.IsStaff)
                {
                    user = ApplicationUser.CreateInvitedStaff(
                        def.UserId,
                        def.PersonId,
                        def.Email,
                        now);
                    user.AcceptStaffInvitation();
                }
                else
                {
                    user = ApplicationUser.CreatePatient(
                        def.UserId,
                        def.PersonId,
                        def.Email,
                        now);
                    user.ConfirmPatientEmail();
                }

                var createResult = await _userManager.CreateAsync(user, def.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Demo kullanıcı '{def.Email}' oluşturulamadı: {errors}");
                }

                existingUser = user;
            }

            if (!await _userManager.IsInRoleAsync(existingUser, def.RoleName))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(existingUser, def.RoleName);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Demo kullanıcı '{def.Email}' rolüne '{def.RoleName}' atanamadı: {errors}");
                }
            }
        }

        LogSeedCompleted(_logger);
    }
}

public sealed record DemoUserDefinition(
    Guid UserId,
    Guid PersonId,
    string Email,
    string Password,
    string RoleName,
    bool IsStaff);
