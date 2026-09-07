namespace HospitalManagement.Modules.Interoperability.Domain;

public enum IntegrationMessageStatus
{
    Success = 1,
    Failed = 2,
    Retried = 3,
    DeadLetter = 4,
}
