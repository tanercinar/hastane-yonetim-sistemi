namespace HospitalManagement.Modules.Scheduling.Domain;

public enum AppointmentStatus
{
    Draft = 0,
    Reserved = 1,
    Confirmed = 2,
    CheckedIn = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
}
