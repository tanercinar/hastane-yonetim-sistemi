namespace HospitalManagement.Modules.Inpatient.Domain;

public enum AdmissionStatus
{
    Requested = 1,     // Yatış İstemi Verildi
    Accepted = 2,      // Yatış Kabul Edildi (Yatak Bekliyor)
    Admitted = 3,      // Yatağa Yerleştirildi / Yatış Aktif
    Transferring = 4,  // Transfer Sürecinde
    Discharged = 5,    // Taburcu Edildi
    Cancelled = 6,     // İptal Edildi
}
