using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class SpecialtyReportingService : ISpecialtyReportingService
{
    private readonly SpecialtyCareDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SpecialtyReportingService(
        SpecialtyCareDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SpecialtyOperationalSummaryDto> GetOperationalSummaryAsync(
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Obstetrics Aggregations
        var activePregnancies = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .CountAsync(p => p.Status == PregnancyEpisodeStatus.Active, cancellationToken);

        var highRiskPregnancies = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .CountAsync(p => p.Status == PregnancyEpisodeStatus.Active &&
                             p.RiskCategory != PregnancyRiskCategory.LowRisk, cancellationToken);

        var deliveriesQuery = _dbContext.DeliveryRecords.AsNoTracking();
        if (startDateUtc.HasValue)
        {
            deliveriesQuery = deliveriesQuery.Where(d => d.DeliveryTimeUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            deliveriesQuery = deliveriesQuery.Where(d => d.DeliveryTimeUtc <= endDateUtc.Value);
        }

        var totalDeliveries = await deliveriesQuery.CountAsync(cancellationToken);
        var cesareanDeliveries = await deliveriesQuery.CountAsync(d =>
            d.DeliveryMode == DeliveryMode.CesareanElective || d.DeliveryMode == DeliveryMode.CesareanEmergency, cancellationToken);
        var normalDeliveries = await deliveriesQuery.CountAsync(d =>
            d.DeliveryMode == DeliveryMode.SpontaneousVaginal ||
            d.DeliveryMode == DeliveryMode.AssistedVaginalVacuum ||
            d.DeliveryMode == DeliveryMode.AssistedVaginalForceps ||
            d.DeliveryMode == DeliveryMode.Vbac, cancellationToken);

        // Odontology Aggregations
        var proceduresQuery = _dbContext.DentalProcedures.AsNoTracking();
        if (startDateUtc.HasValue)
        {
            proceduresQuery = proceduresQuery.Where(p => p.CreatedAtUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            proceduresQuery = proceduresQuery.Where(p => p.CreatedAtUtc <= endDateUtc.Value);
        }

        var totalProcedures = await proceduresQuery.CountAsync(cancellationToken);
        var completedProcedures = await proceduresQuery.CountAsync(p => p.Status == DentalProcedureStatus.Completed, cancellationToken);
        var plannedProcedures = await proceduresQuery.CountAsync(p => p.Status == DentalProcedureStatus.Planned, cancellationToken);

        var totalDentalExams = await _dbContext.DentalExaminations
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Home Health Aggregations
        var activeHomeVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .CountAsync(v => v.Status == HomeVisitStatus.Requested ||
                             v.Status == HomeVisitStatus.Approved ||
                             v.Status == HomeVisitStatus.Assigned ||
                             v.Status == HomeVisitStatus.InProgress, cancellationToken);

        var pendingHomeVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .CountAsync(v => v.Status == HomeVisitStatus.Requested, cancellationToken);

        var assignedHomeVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .CountAsync(v => v.Status == HomeVisitStatus.Assigned || v.Status == HomeVisitStatus.InProgress, cancellationToken);

        var completedHomeVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .CountAsync(v => v.Status == HomeVisitStatus.Completed, cancellationToken);

        var urgentHomeVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .CountAsync(v => v.Priority == HomeVisitPriority.Urgent &&
                             v.Status != HomeVisitStatus.Completed &&
                             v.Status != HomeVisitStatus.Cancelled, cancellationToken);

        return new SpecialtyOperationalSummaryDto(
            ActivePregnanciesCount: activePregnancies,
            HighRiskPregnanciesCount: highRiskPregnancies,
            TotalDeliveriesCount: totalDeliveries,
            CesareanDeliveriesCount: cesareanDeliveries,
            NormalDeliveriesCount: normalDeliveries,
            TotalDentalProceduresCount: totalProcedures,
            CompletedDentalProceduresCount: completedProcedures,
            PlannedDentalProceduresCount: plannedProcedures,
            TotalDentalExaminationsCount: totalDentalExams,
            ActiveHomeVisitsCount: activeHomeVisits,
            PendingHomeVisitRequestsCount: pendingHomeVisits,
            AssignedHomeVisitsCount: assignedHomeVisits,
            CompletedHomeVisitsCount: completedHomeVisits,
            UrgentHomeVisitsCount: urgentHomeVisits,
            GeneratedAtUtc: now);
    }
}
