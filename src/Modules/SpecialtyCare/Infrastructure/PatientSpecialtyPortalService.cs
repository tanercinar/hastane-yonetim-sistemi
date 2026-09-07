using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class PatientSpecialtyPortalService(
    SpecialtyCareDbContext dbContext,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IPatientSpecialtyPortalService
{
    private readonly SpecialtyCareDbContext _dbContext = dbContext;
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PatientSpecialtyPortalDto> GetPublishedRecordsAsync(
        Guid patientId,
        Guid actorPersonId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(patientId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorPersonId, Guid.Empty);

        var pregnancies = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .Where(episode => episode.PatientId == patientId)
            .OrderByDescending(episode => episode.CreatedAtUtc)
            .Select(episode => new PatientPregnancySummaryDto(
                episode.Id,
                episode.EpisodeProtocolNumber,
                episode.Status.ToString(),
                episode.EstimatedDeliveryDateUtc,
                episode.AntenatalVisits.Count,
                episode.AntenatalVisits
                    .Select(visit => (DateTime?)visit.VisitDateUtc)
                    .Max()))
            .ToListAsync(cancellationToken);

        var deliveries = await _dbContext.DeliveryRecords
            .AsNoTracking()
            .Where(delivery => delivery.MotherPatientId == patientId)
            .OrderByDescending(delivery => delivery.DeliveryTimeUtc)
            .Select(delivery => new PatientDeliverySummaryDto(
                delivery.Id,
                delivery.DeliveryProtocolNumber,
                delivery.DeliveryMode.ToString(),
                delivery.DeliveryTimeUtc,
                delivery.GestationalAgeWeeks,
                delivery.GestationalAgeDays,
                delivery.Newborns.Count))
            .ToListAsync(cancellationToken);

        var examinations = await _dbContext.DentalExaminations
            .AsNoTracking()
            .Where(examination => examination.PatientId == patientId)
            .OrderByDescending(examination => examination.ExaminationDateUtc)
            .Select(examination => new PatientDentalExaminationSummaryDto(
                examination.Id,
                examination.ExaminationProtocolNumber,
                examination.ExaminationDateUtc))
            .ToListAsync(cancellationToken);

        // Only completed procedures are patient-published; clinical working states stay hidden.
        var procedures = await _dbContext.DentalProcedures
            .AsNoTracking()
            .Where(procedure => procedure.PatientId == patientId
                && procedure.Status == DentalProcedureStatus.Completed)
            .OrderByDescending(procedure => procedure.CompletedDateUtc)
            .Select(procedure => new PatientDentalProcedureSummaryDto(
                procedure.Id,
                procedure.ProcedureProtocolNumber,
                procedure.ToothNumber,
                procedure.ProcedureName,
                procedure.Status.ToString(),
                procedure.CompletedDateUtc))
            .ToListAsync(cancellationToken);

        var homeHealthVisits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .Where(visit => visit.PatientId == patientId)
            .OrderByDescending(visit => visit.RequestedDateUtc)
            .Select(visit => new PatientHomeHealthVisitSummaryDto(
                visit.Id,
                visit.ProtocolNumber,
                visit.ServiceType.ToString(),
                visit.Priority.ToString(),
                visit.Status.ToString(),
                visit.RequestedDateUtc,
                visit.ScheduledDateUtc,
                visit.VisitCompletedAtUtc,
                visit.City,
                visit.District))
            .ToListAsync(cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _auditPublisher.PublishAsync(
            new AuditEvent(
                Guid.NewGuid(),
                nowUtc,
                ActorUserId: null,
                ActorPersonId: actorPersonId,
                ActorRole: "PAT",
                ActorIpAddress: null,
                ActorUserAgent: null,
                Action: "Specialty.PatientPortalView",
                TargetResourceType: "PatientSpecialtyPortal",
                TargetResourceId: patientId.ToString("D"),
                Outcome: AuditOutcome.Success,
                Reason: "Hasta kendi yayınlanmış uzmanlık kayıt özetlerini görüntüledi.",
                CorrelationId: Guid.NewGuid().ToString("D"),
                DetailsJson: null),
            cancellationToken);

        return new PatientSpecialtyPortalDto(
            pregnancies,
            deliveries,
            examinations,
            procedures,
            homeHealthVisits,
            nowUtc);
    }
}
