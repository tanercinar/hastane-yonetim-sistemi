namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum CriticalNotificationStatus
{
    Active = 1,
    Acknowledged = 2,
    Escalated = 3,
    Closed = 4,
}
