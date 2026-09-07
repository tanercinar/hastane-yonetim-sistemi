namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public enum SurgeryBookingStatus
{
    Scheduled = 1,
    PreOpCleared = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Postponed = 6,
}
