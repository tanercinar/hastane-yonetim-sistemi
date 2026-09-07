namespace HospitalManagement.Modules.Emergency.Domain;

public enum EmergencyDispositionType
{
    DischargeHome = 1,
    AdmitToWard = 2,
    AdmitToIcu = 3,
    DirectToSurgery = 4,
    TransferToOtherHospital = 5,
    Exitus = 6,
}
