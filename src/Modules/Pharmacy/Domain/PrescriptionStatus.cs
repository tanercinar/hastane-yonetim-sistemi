namespace HospitalManagement.Modules.Pharmacy.Domain;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "Prescription signature status")]
public enum PrescriptionStatus
{
    Draft = 1,
    Signed = 2,
    PartiallyDispensed = 3,
    Dispensed = 4,
    Cancelled = 5,
    Expired = 6,
    EnteredInError = 7,
}
