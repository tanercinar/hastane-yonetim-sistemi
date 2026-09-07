namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

[Flags]
public enum ToothSurface
{
    None = 0,
    Mesial = 1,     // M
    Distal = 2,     // D
    Occlusal = 4,   // O (Arka diş çiğneme) / Incisal I (Ön diş kesici)
    Buccal = 8,     // B (Yanak / Dudak yönü)
    Lingual = 16,   // L (Dil / Damak yönü)
}
