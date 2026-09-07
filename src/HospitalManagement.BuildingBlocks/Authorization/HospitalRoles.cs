namespace HospitalManagement.BuildingBlocks.Authorization;

public static class HospitalRoles
{
    public const string Patient = "PAT";
    public const string Doctor = "DOC";
    public const string Nurse = "NUR";
    public const string ChiefMedicalOfficer = "CHM";
    public const string RegistrationStaff = "REG";
    public const string LaboratoryStaff = "LAB";
    public const string RadiologyStaff = "RAD";
    public const string Pharmacist = "PHA";
    public const string SystemAdministrator = "ADM";
    public const string HospitalManager = "MGR";
    public const string BillingStaff = "FIN";
    public const string HumanResourcesStaff = "HR";

    public static readonly IReadOnlyList<RoleDefinition> All =
    [
        new(Patient, "Hasta", "Yalnızca kendi portal kaynaklarına erişebilir."),
        new(Doctor, "Doktor", "Bakım ilişkisi ve ataması bulunan hastalarda klinik işlemleri yürütür."),
        new(Nurse, "Hemşire", "Atandığı veya bölümündeki hastalarda hemşirelik ve bakım işlemlerini yürütür."),
        new(ChiefMedicalOfficer, "Başhekim", "Klinik rol yetkilerine ek olarak bölüm gözetimi ve özel onayları yürütür."),
        new(RegistrationStaff, "Kayıt/Danışma Personeli", "Demografi, randevu, check-in ve sıra işlemlerini yürütür; klinik içerik göremez."),
        new(LaboratoryStaff, "Laboratuvar Personeli", "Laboratuvar iş listesi, numune ve teknik sonuç işlemlerini yürütür."),
        new(RadiologyStaff, "Radyoloji Personeli", "Görüntüleme iş listesi, çekim ve rapor işlemlerini yürütür."),
        new(Pharmacist, "Eczacı", "Reçete doğrulama, ilaç teslimi ve eczane stok işlemlerini yürütür."),
        new(SystemAdministrator, "Sistem Yöneticisi", "Hesap, rol, izin ve organizasyon yönetimini yürütür; klinik içerik göremez."),
        new(HospitalManager, "Hastane Yöneticisi", "Kimliksiz ve minimum operasyonel raporları görüntüler; klinik düzenleme yapamaz."),
        new(BillingStaff, "Muhasebe Personeli", "Gelecek rolü; bu sürümde işlev ve uygulama izni bulunmaz."),
        new(HumanResourcesStaff, "İnsan Kaynakları Personeli", "Gelecek rolü; bu sürümde işlev ve uygulama izni bulunmaz."),
    ];

    private static readonly HashSet<string> RoleCodeSet = new(
        All.Select(r => r.Code),
        StringComparer.Ordinal);

    public static bool IsKnown(string role) =>
        !string.IsNullOrWhiteSpace(role) && RoleCodeSet.Contains(role.Trim());
}

public sealed record RoleDefinition(string Code, string Name, string Description);

