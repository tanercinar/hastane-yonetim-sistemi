using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientDashboardService : IInpatientDashboardService
{
    private readonly InpatientDbContext _dbContext;

    public InpatientDashboardService(InpatientDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<InpatientDashboardDto> GetDashboardSummaryAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var wardsQuery = _dbContext.Wards.AsNoTracking().Where(w => w.IsActive);
        if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            wardsQuery = wardsQuery.Where(w => w.Id == wardId.Value);
        }
        else if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            wardsQuery = wardsQuery.Where(w => w.DepartmentId == departmentId.Value);
        }

        var wards = await wardsQuery.OrderBy(w => w.Name).ToListAsync(cancellationToken);
        var wardIds = wards.Select(w => w.Id).ToList();

        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive && wardIds.Contains(b.WardId))
            .ToListAsync(cancellationToken);

        var activeAdmissions = await _dbContext.Admissions
            .AsNoTracking()
            .Where(a => (a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Transferring)
                        && wardIds.Contains(a.AdmittingWardId))
            .ToListAsync(cancellationToken);

        var pendingAdmissionsCount = await _dbContext.Admissions
            .AsNoTracking()
            .CountAsync(a => (a.Status == AdmissionStatus.Requested || a.Status == AdmissionStatus.Accepted)
                             && wardIds.Contains(a.AdmittingWardId), cancellationToken);

        var pendingTransfersCount = await _dbContext.Transfers
            .AsNoTracking()
            .CountAsync(t => (t.Status == TransferStatus.Requested || t.Status == TransferStatus.Accepted)
                             && (wardIds.Contains(t.SourceWardId) || wardIds.Contains(t.TargetWardId)), cancellationToken);

        var todayStartUtc = DateTime.UtcNow.Date;
        var todayDischargesCount = await _dbContext.Discharges
            .AsNoTracking()
            .CountAsync(
                d => d.DischargedAtUtc >= todayStartUtc
                    && _dbContext.Admissions.Any(
                        admission => admission.Id == d.AdmissionId
                            && wardIds.Contains(admission.AdmittingWardId)),
                cancellationToken);

        var wardSummaries = new List<WardOccupancySummaryDto>();

        foreach (var ward in wards)
        {
            var wardBeds = beds.Where(b => b.WardId == ward.Id).ToList();
            var totalBeds = wardBeds.Count;
            var occupied = wardBeds.Count(b => b.Status == BedStatus.Occupied);
            var available = wardBeds.Count(b => b.Status == BedStatus.Available);
            var cleaning = wardBeds.Count(b => b.Status == BedStatus.Cleaning);
            var maintenance = wardBeds.Count(b => b.Status == BedStatus.Maintenance);
            var activePatients = activeAdmissions.Count(a => a.AdmittingWardId == ward.Id);

            var occupancyPercent = totalBeds > 0
                ? Math.Round((double)occupied / totalBeds * 100.0, 1)
                : 0.0;

            wardSummaries.Add(new WardOccupancySummaryDto(
                ward.Id,
                ward.Name,
                ward.Code,
                ward.DepartmentId,
                totalBeds,
                occupied,
                available,
                cleaning,
                maintenance,
                occupancyPercent,
                activePatients));
        }

        var overallTotalBeds = wardSummaries.Sum(w => w.TotalBeds);
        var overallOccupied = wardSummaries.Sum(w => w.OccupiedBeds);
        var overallAvailable = wardSummaries.Sum(w => w.AvailableBeds);
        var overallCleaning = wardSummaries.Sum(w => w.CleaningBeds);
        var overallMaintenance = wardSummaries.Sum(w => w.MaintenanceBeds);
        var overallOccupancyPercentage = overallTotalBeds > 0
            ? Math.Round((double)overallOccupied / overallTotalBeds * 100.0, 1)
            : 0.0;

        return new InpatientDashboardDto(
            overallTotalBeds,
            overallOccupied,
            overallAvailable,
            overallCleaning,
            overallMaintenance,
            overallOccupancyPercentage,
            pendingAdmissionsCount,
            pendingTransfersCount,
            todayDischargesCount,
            wardSummaries);
    }
}
