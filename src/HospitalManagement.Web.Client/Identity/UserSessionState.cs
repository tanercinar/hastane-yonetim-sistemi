using HospitalManagement.Contracts.Identity;

namespace HospitalManagement.Web.Client.Identity;

public sealed class UserSessionState
{
    private readonly IdentityApiClient _identityApiClient;
    private CurrentAccountResponse? _currentAccount;
    private bool _isInitialized;

    public event Action? OnChange;

    public UserSessionState(IdentityApiClient identityApiClient)
    {
        _identityApiClient = identityApiClient ?? throw new ArgumentNullException(nameof(identityApiClient));
    }

    public CurrentAccountResponse? CurrentAccount => _currentAccount;
    public bool IsAuthenticated => _currentAccount is not null;
    public string? Email => _currentAccount?.Email;
    public string? AccountKind => _currentAccount?.AccountKind;
    public string? PersonId => _currentAccount?.PersonId;
    public IReadOnlyList<string> Roles => _currentAccount?.Roles ?? Array.Empty<string>();
    public IReadOnlyList<string> Permissions => _currentAccount?.Permissions ?? Array.Empty<string>();

    public bool IsInRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    public bool HasPermission(string permission) =>
        Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));

    public bool IsPatient => IsInAnyRole("PAT", "Patient")
        || string.Equals(AccountKind, "Patient", StringComparison.OrdinalIgnoreCase);
    public bool IsDoctor => IsInAnyRole("DOC", "Doctor", "CHM", "ChiefMedicalOfficer");
    public bool IsRegistrationStaff => IsInAnyRole("REG", "RegistrationStaff");
    public bool IsSystemAdmin => IsInAnyRole("ADM", "SystemAdministrator");
    public bool IsHospitalManager => IsInAnyRole("MGR", "HospitalManager");
    public bool IsNurse => IsInAnyRole("NUR", "Nurse");
    public bool IsPharmacist => IsInAnyRole("PHA", "Pharmacist");
    public bool IsLabTechnician => IsInAnyRole("LAB", "LabTechnician");
    public bool IsRadiologist => IsInAnyRole("RAD", "Radiologist");
    public bool IsStaff => !IsPatient && IsAuthenticated;

    public string GetRoleDisplayName()
    {
        if (IsSystemAdmin)
            return "Sistem Yöneticisi";
        if (IsHospitalManager)
            return "Hastane Müdürü";
        if (IsDoctor)
            return "Hekim";
        if (IsPharmacist)
            return "Eczacı";
        if (IsLabTechnician)
            return "Laboratuvar Teknisyeni";
        if (IsRadiologist)
            return "Radyolog";
        if (IsRegistrationStaff)
            return "Kayıt Personeli";
        if (IsNurse)
            return "Hemşire";
        if (IsPatient)
            return "Hasta";
        if (Roles.Count > 0)
            return Roles[0];
        return AccountKind ?? "Kullanıcı";
    }

    public string GetHomeRouteForCurrentRole()
    {
        if (!IsAuthenticated)
        {
            return "account/login";
        }
        if (IsPatient)
        {
            return "patient/appointments";
        }
        if (IsDoctor || IsRegistrationStaff || IsNurse)
        {
            return "staff/queue";
        }
        if (IsPharmacist)
        {
            return "pharmacy/worklist";
        }
        if (IsSystemAdmin || IsHospitalManager)
        {
            return "admin/users";
        }
        return "account";
    }

    public async Task<CurrentAccountResponse?> EnsureLoadedAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (_isInitialized && !forceRefresh)
        {
            return _currentAccount;
        }

        try
        {
            _currentAccount = await _identityApiClient.GetCurrentAccountAsync(cancellationToken);
        }
        catch
        {
            _currentAccount = null;
        }
        finally
        {
            _isInitialized = true;
            NotifyStateChanged();
        }

        return _currentAccount;
    }

    public async Task<IdentityApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var result = await _identityApiClient.LogoutAsync(cancellationToken);
        _currentAccount = null;
        _isInitialized = true;
        NotifyStateChanged();
        return result;
    }

    public void SetAccount(CurrentAccountResponse? account)
    {
        _currentAccount = account;
        _isInitialized = true;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();

    private bool IsInAnyRole(params string[] roles) => roles.Any(IsInRole);
}
