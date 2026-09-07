namespace HospitalManagement.Modules.Emergency.Domain;

public enum EmergencyAdmissionStatus
{
    WaitingTriage = 1,
    TriagedWaitingDoctor = 2,
    InEvaluation = 3,
    InObservation = 4,
    AdmittedToInpatient = 5,
    AdmittedToIcu = 6,
    Discharged = 7,
    TransferredOut = 8,
    LeftWithoutBeingSeen = 9,
    Deceased = 10,
}
