namespace HospitalManagement.Modules.Interoperability.Domain;

public enum ExternalSystemType
{
    Fhir = 1,
    Hl7V2 = 2,
    DicomPacs = 3,
    Mhrs = 4,
    ENabiz = 5,
    Medula = 6,
    Notification = 7,
}
