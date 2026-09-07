namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed record PreOpChecklistInfo(
    bool ConsentSigned,
    bool AnesthesiaClearance,
    bool NpoConfirmed,
    bool BloodProductsReserved,
    bool SiteMarked,
    bool AllergyChecked,
    Guid CompletedByStaffId,
    DateTime CompletedAtUtc,
    string? Notes)
{
    public bool IsFullyCleared =>
        ConsentSigned &&
        AnesthesiaClearance &&
        NpoConfirmed &&
        BloodProductsReserved &&
        SiteMarked &&
        AllergyChecked;
}
