using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class PharmacyDashboardService : IPharmacyDashboardService
{
    private readonly ReportingDbContext _dbContext;

    public PharmacyDashboardService(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PharmacyDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.PharmacyDispensingMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate)
            .ToListAsync(cancellationToken);

        var total = metrics.Sum(m => m.TotalPrescriptions);
        var pending = metrics.Sum(m => m.PendingDispenseCount);
        var dispensed = metrics.Sum(m => m.DispensedCount);
        var lowStock = metrics.Count > 0 ? metrics.Max(m => m.LowStockItemCount) : 0;
        var nearExpiry = metrics.Count > 0 ? metrics.Max(m => m.NearExpiryLotCount) : 0;
        var lastUpdated = metrics.Count > 0 ? metrics.Max(m => m.LastUpdatedUtc) : DateTime.UtcNow;

        return new PharmacyDashboardSummaryDto(
            queryDate,
            total,
            pending,
            dispensed,
            lowStock,
            nearExpiry,
            lastUpdated);
    }

    public async Task<IReadOnlyList<PharmacyStockAlertMetricDto>> GetStockAlertsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAsync(targetDate, cancellationToken);
        var list = new List<PharmacyStockAlertMetricDto>();

        if (summary.LowStockItemCount > 0)
        {
            list.Add(new PharmacyStockAlertMetricDto(
                "Kritik Düşük Stok",
                "DEMO-Parasetamol 500mg Tablet",
                12,
                50,
                "Kutu"));

            if (summary.LowStockItemCount > 1)
            {
                list.Add(new PharmacyStockAlertMetricDto(
                    "Kritik Düşük Stok",
                    "DEMO-Amoksisilin 1000mg Film Tablet",
                    8,
                    30,
                    "Kutu"));
            }
        }

        if (summary.NearExpiryLotCount > 0)
        {
            list.Add(new PharmacyStockAlertMetricDto(
                "Son Kullanma Tarihi Yaklaşan Lot",
                "DEMO-Serum Fizyolojik %0.9 500ml",
                45,
                100,
                "Şişe"));
        }

        return list;
    }
}
