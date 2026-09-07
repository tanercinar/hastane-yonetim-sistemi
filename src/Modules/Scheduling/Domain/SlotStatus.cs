namespace HospitalManagement.Modules.Scheduling.Domain;

public enum SlotStatus
{
    Available = 0,
    Held = 1,
    Booked = 2,
    Blocked = 3,
    Cancelled = 4,
}
