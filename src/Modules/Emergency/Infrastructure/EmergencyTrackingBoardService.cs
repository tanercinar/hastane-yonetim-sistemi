using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Emergency.Infrastructure;

public sealed class EmergencyTrackingBoardService : IEmergencyTrackingBoardService
{
    private static readonly EmergencyAdmissionStatus[] ActiveStatuses =
    [
        EmergencyAdmissionStatus.WaitingTriage,
        EmergencyAdmissionStatus.TriagedWaitingDoctor,
        EmergencyAdmissionStatus.InEvaluation,
        EmergencyAdmissionStatus.InObservation,
    ];

    private readonly EmergencyDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EmergencyTrackingBoardService(
        EmergencyDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EmergencyBoardSummaryDto> GetBoardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var todayStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);

        var activeAdmissions = await _dbContext.Admissions
            .AsNoTracking()
            .Where(a => ActiveStatuses.Contains(a.Status))
            .ToListAsync(cancellationToken);

        var todayDischargedCount = await _dbContext.Admissions
            .AsNoTracking()
            .CountAsync(a => a.Status == EmergencyAdmissionStatus.Discharged && a.CompletedAtUtc >= todayStartUtc, cancellationToken);

        var admittedPendingTransferCount = await _dbContext.Admissions
            .AsNoTracking()
            .CountAsync(a => (a.Status == EmergencyAdmissionStatus.AdmittedToInpatient || a.Status == EmergencyAdmissionStatus.AdmittedToIcu)
                             && a.CompletedAtUtc >= todayStartUtc, cancellationToken);

        var waitingTriage = activeAdmissions.Where(a => a.Status == EmergencyAdmissionStatus.WaitingTriage).ToList();
        var waitingDoctor = activeAdmissions.Where(a => a.Status == EmergencyAdmissionStatus.TriagedWaitingDoctor).ToList();
        var inEvaluation = activeAdmissions.Where(a => a.Status == EmergencyAdmissionStatus.InEvaluation).ToList();
        var inObservation = activeAdmissions.Where(a => a.Status == EmergencyAdmissionStatus.InObservation).ToList();

        var red1Count = activeAdmissions.Count(a => a.Triage?.TriageLevel == TriageLevel.Red1Resuscitation);
        var red2Count = activeAdmissions.Count(a => a.Triage?.TriageLevel == TriageLevel.Red2Emergency);
        var yellowCount = activeAdmissions.Count(a => a.Triage?.TriageLevel == TriageLevel.YellowUrgent);
        var greenCount = activeAdmissions.Count(a => a.Triage?.TriageLevel == TriageLevel.GreenStandard);
        var blackCount = activeAdmissions.Count(a => a.Triage?.TriageLevel == TriageLevel.BlackExpectant);

        var avgWaitTriage = waitingTriage.Count > 0
            ? waitingTriage.Average(a => Math.Max(0, (nowUtc - a.AdmittedAtUtc).TotalMinutes))
            : 0.0;

        var avgWaitDoctor = waitingDoctor.Count > 0
            ? waitingDoctor.Average(a => a.Triage != null ? Math.Max(0, (nowUtc - a.Triage.TriagedAtUtc).TotalMinutes) : 0.0)
            : 0.0;

        var avgLos = activeAdmissions.Count > 0
            ? activeAdmissions.Average(a => Math.Max(0, (nowUtc - a.AdmittedAtUtc).TotalMinutes))
            : 0.0;

        return new EmergencyBoardSummaryDto(
            TotalActiveAdmissions: activeAdmissions.Count,
            WaitingTriageCount: waitingTriage.Count,
            TriagedWaitingDoctorCount: waitingDoctor.Count,
            InEvaluationCount: inEvaluation.Count,
            InObservationCount: inObservation.Count,
            AdmittedPendingTransferCount: admittedPendingTransferCount,
            TodayDischargedCount: todayDischargedCount,
            Red1Count: red1Count,
            Red2Count: red2Count,
            YellowCount: yellowCount,
            GreenCount: greenCount,
            BlackCount: blackCount,
            AverageWaitMinutesTriage: Math.Round(avgWaitTriage, 1),
            AverageWaitMinutesDoctor: Math.Round(avgWaitDoctor, 1),
            AverageLengthOfStayMinutes: Math.Round(avgLos, 1));
    }

    public async Task<List<EmergencyBoardWorklistItemDto>> GetBoardWorklistAsync(
        EmergencyAdmissionStatus? status = null,
        TriageLevel? triageLevel = null,
        string? zone = null,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var query = _dbContext.Admissions
            .AsNoTracking()
            .Where(a => ActiveStatuses.Contains(a.Status));

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (triageLevel.HasValue)
        {
            query = query.Where(a => a.Triage != null && a.Triage.TriageLevel == triageLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            var zoneTrim = zone.Trim();
            query = query.Where(a => a.AssignedBedOrZone != null && a.AssignedBedOrZone.Contains(zoneTrim));
        }

        var admissions = await query.ToListAsync(cancellationToken);

        // Sorting by priority: Triage Level (Red1=1, Red2=2, Yellow=3, Green=4, Black=5, None=6) then AdmittedAtUtc ascending (longest waiting first)
        var items = admissions
            .Select(a =>
            {
                var waitingMinutes = (int)Math.Max(0, (nowUtc - a.AdmittedAtUtc).TotalMinutes);
                int? doctorWaitingMinutes = a.Triage != null
                    ? (int)Math.Max(0, (nowUtc - a.Triage.TriagedAtUtc).TotalMinutes)
                    : null;

                return new EmergencyBoardWorklistItemDto(
                    a.Id,
                    a.EmergencyProtocolNumber,
                    a.PatientId,
                    a.ArrivalType,
                    a.ChiefComplaint,
                    a.Status,
                    a.Triage?.TriageLevel,
                    a.Triage?.TriageCategoryReason,
                    a.AdmittedAtUtc,
                    a.Triage?.TriagedAtUtc,
                    a.AssignedDoctorId,
                    a.AssignedBedOrZone,
                    waitingMinutes,
                    doctorWaitingMinutes);
            })
            .OrderBy(GetTriageSortWeight)
            .ThenByDescending(i => i.WaitingMinutes)
            .ToList();

        return items;
    }

    private static int GetTriageSortWeight(EmergencyBoardWorklistItemDto item) =>
        item.TriageLevel switch
        {
            TriageLevel.Red1Resuscitation => 1,
            TriageLevel.Red2Emergency => 2,
            TriageLevel.YellowUrgent => 3,
            TriageLevel.GreenStandard => 4,
            TriageLevel.BlackExpectant => 5,
            _ => 6,
        };
}
