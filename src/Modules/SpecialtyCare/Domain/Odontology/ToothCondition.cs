namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

public enum ToothCondition
{
    Sound = 0,             // Sağlam
    Caries = 1,            // Çürük
    Filled = 2,            // Dolgulu
    Missing = 3,           // Eksik / Çekilmiş
    Impacted = 4,          // Gömülü
    Crown = 5,             // Kuron / Kaplama
    RootCanalTreated = 6,  // Kanal Tedavili
    Implant = 7,           // Dental İmplant
    DecayedRoot = 8,       // Kök Kalıntısı
    Fractured = 9,         // Kırık
}
