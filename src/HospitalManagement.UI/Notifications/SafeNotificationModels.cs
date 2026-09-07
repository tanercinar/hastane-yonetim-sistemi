namespace HospitalManagement.UI.Notifications;

public enum SafeNotificationCategory
{
    General,
    Appointment,
    Prescription,
    DiagnosticResult,
    StaffUrgent
}

public sealed record SafeNotificationMessage
{
    public Guid Id
    {
        get;
        init;
    } = Guid.NewGuid();

    public SafeNotificationCategory Category
    {
        get;
        init;
    } = SafeNotificationCategory.General;

    /// <summary>
    /// Generic title safe for public lock screen and OS push previews.
    /// Never contains protected health information (PHI).
    /// </summary>
    public string PublicLockScreenTitle
    {
        get;
        init;
    } = string.Empty;

    /// <summary>
    /// Generic preview message safe for public lock screen (no diagnosis, medication, or lab value).
    /// </summary>
    public string PublicLockScreenPreview
    {
        get;
        init;
    } = string.Empty;

    /// <summary>
    /// Detailed clinical information visible only inside authenticated app screen after unlock.
    /// </summary>
    public string AuthenticatedDetail
    {
        get;
        init;
    } = string.Empty;

    /// <summary>
    /// Associated deep-link (e.g. "hospitalapp://appointments?id=...").
    /// </summary>
    public string DeepLinkUrl
    {
        get;
        init;
    } = string.Empty;

    public DateTime CreatedAt
    {
        get;
        init;
    } = DateTime.UtcNow;

    public bool IsRead
    {
        get;
        set;
    }
}
