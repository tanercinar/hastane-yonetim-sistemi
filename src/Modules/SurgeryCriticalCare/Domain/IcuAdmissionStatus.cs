namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public enum IcuAdmissionStatus
{
    Active = 1,
    TransferredToWard = 2,
    Discharged = 3,
    Deceased = 4,
}
