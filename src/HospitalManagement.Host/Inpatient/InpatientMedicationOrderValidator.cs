using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Inpatient;

public sealed class InpatientMedicationOrderValidator(
    PharmacyDbContext pharmacyDbContext,
    TimeProvider timeProvider) : IInpatientMedicationOrderValidator
{
    private readonly PharmacyDbContext _pharmacyDbContext = pharmacyDbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<bool> IsActiveOrderAsync(
        Guid prescriptionId,
        Guid patientId,
        string medicationName,
        string dose,
        string route,
        CancellationToken cancellationToken = default)
    {
        if (prescriptionId == Guid.Empty || patientId == Guid.Empty)
        {
            return false;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var prescription = await _pharmacyDbContext.Prescriptions
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == prescriptionId
                    && candidate.PatientId == patientId,
                cancellationToken);

        if (prescription is null
            || prescription.Status is not (PrescriptionStatus.Signed or PrescriptionStatus.PartiallyDispensed)
            || prescription.ValidUntilUtc is not null && prescription.ValidUntilUtc <= nowUtc)
        {
            return false;
        }

        var normalizedName = Normalize(medicationName);
        var normalizedDose = Normalize(dose);
        var normalizedRoute = Normalize(route);
        return prescription.Items.Any(item =>
            (Normalize(item.BrandName) == normalizedName || Normalize(item.GenericName) == normalizedName)
            && Normalize($"{item.Dose:0.####} {item.DoseUnit}") == normalizedDose
            && Normalize(item.Route.ToString()) == normalizedRoute);
    }

    public async Task<List<ActiveMedicationOrderResponse>> GetActiveOrdersAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var prescriptions = await _pharmacyDbContext.Prescriptions
            .AsNoTracking()
            .Include(prescription => prescription.Items)
            .Where(prescription => prescription.PatientId == patientId
                && (prescription.Status == PrescriptionStatus.Signed
                    || prescription.Status == PrescriptionStatus.PartiallyDispensed)
                && (prescription.ValidUntilUtc == null || prescription.ValidUntilUtc > nowUtc))
            .OrderByDescending(prescription => prescription.SignedAtUtc)
            .ToListAsync(cancellationToken);

        return prescriptions
            .SelectMany(prescription => prescription.Items.Select(item => new ActiveMedicationOrderResponse(
                prescription.Id,
                item.Id,
                item.BrandName,
                $"{item.Dose:0.####} {item.DoseUnit}",
                item.Route.ToString(),
                prescription.ValidUntilUtc)))
            .ToList();
    }

    private static string Normalize(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();
}
