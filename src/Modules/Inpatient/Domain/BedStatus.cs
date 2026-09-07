namespace HospitalManagement.Modules.Inpatient.Domain;

public enum BedStatus
{
    Available = 1,    // Boş / Temiz
    Occupied = 2,     // Dolu
    Cleaning = 3,     // Temizlikte / Dezenfeksiyonda
    Maintenance = 4,  // Bakım Dışı / Arızalı
    Reserved = 5,     // Rezerve
}
