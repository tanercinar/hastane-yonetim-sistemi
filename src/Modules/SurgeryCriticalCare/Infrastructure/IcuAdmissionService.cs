using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class IcuAdmissionService : IIcuAdmissionService
{
    private readonly SurgeryDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public IcuAdmissionService(
        SurgeryDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<List<IcuBedDto>> GetIcuBedsAsync(CancellationToken cancellationToken = default)
    {
        var beds = await _dbContext.IcuBeds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.BedCode)
            .ToListAsync(cancellationToken);

        var activeAdmissions = await _dbContext.IcuAdmissions
            .AsNoTracking()
            .Where(a => a.Status == IcuAdmissionStatus.Active)
            .ToListAsync(cancellationToken);

        var activeMap = activeAdmissions.ToDictionary(a => a.IcuBedId);

        return beds.Select(b =>
        {
            var isOccupied = activeMap.TryGetValue(b.Id, out var adm);
            return new IcuBedDto(
                b.Id,
                b.BedCode,
                b.BedName,
                b.UnitName,
                b.IsActive,
                isOccupied,
                adm?.Id,
                adm?.AdmissionProtocolNumber);
        }).ToList();
    }

    public async Task<SurgeryOperationResult<IcuAdmissionDto>> AdmitToIcuAsync(
        CreateIcuAdmissionDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.InpatientStayId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("InpatientStayId", "Hastane yatış takip kaydı seçilmelidir.");
        }

        if (dto.PatientId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (dto.IcuBedId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("IcuBedId", "Yoğun bakım yatağı seçilmelidir.");
        }

        if (dto.AttendingDoctorId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("AttendingDoctorId", "Sorumlu yoğun bakım hekimi seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.AdmissionReason))
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("AdmissionReason", "Kabul gerekçesi belirtilmelidir.");
        }

        var bed = await _dbContext.IcuBeds.FirstOrDefaultAsync(b => b.Id == dto.IcuBedId, cancellationToken);
        if (bed is null || !bed.IsActive)
        {
            return SurgeryOperationResult.NotFound<IcuAdmissionDto>("Seçilen yoğun bakım yatağı bulunamadı veya pasif durumda.");
        }

        // Bed occupancy collision check
        var isBedOccupied = await _dbContext.IcuAdmissions
            .AnyAsync(a => a.IcuBedId == dto.IcuBedId && a.Status == IcuAdmissionStatus.Active, cancellationToken);

        if (isBedOccupied)
        {
            return SurgeryOperationResult.Conflict<IcuAdmissionDto>($"'{bed.BedCode}' kodlu yoğun bakım yatağında şu anda aktif bir hasta yatmaktadır.");
        }

        // Patient single active ICU admission check
        var hasActiveAdmission = await _dbContext.IcuAdmissions
            .AnyAsync(a => a.PatientId == dto.PatientId && a.Status == IcuAdmissionStatus.Active, cancellationToken);

        if (hasActiveAdmission)
        {
            return SurgeryOperationResult.Conflict<IcuAdmissionDto>("Bu hastanın hâlihazırda aktif bir yoğun bakım yatışı bulunmaktadır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var admission = IcuAdmission.Create(
            Guid.NewGuid(),
            dto.InpatientStayId,
            dto.PatientId,
            dto.EncounterId,
            bed.Id,
            bed.BedCode,
            dto.AttendingDoctorId,
            dto.PrimaryNurseId,
            dto.AdmissionReason,
            dto.AcuityLevel,
            dto.MonitoringFrequencyMinutes,
            dto.VentilationMode,
            dto.CarePlanNotes,
            now);

        _dbContext.IcuAdmissions.Add(admission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.IcuAdmit",
            admission.Id.ToString(),
            "Hasta yoğun bakıma kabul edildi",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                Acuity = dto.AcuityLevel.ToString(),
                Ventilation = dto.VentilationMode.ToString(),
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(admission));
    }

    public async Task<SurgeryOperationResult<IcuAdmissionDto>> UpdateCarePlanAsync(
        Guid admissionId,
        UpdateIcuCarePlanDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var admission = await _dbContext.IcuAdmissions.FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return SurgeryOperationResult.NotFound<IcuAdmissionDto>("Yoğun bakım kabul kaydı bulunamadı.");
        }

        if (admission.Status != IcuAdmissionStatus.Active)
        {
            return SurgeryOperationResult.Conflict<IcuAdmissionDto>("Yalnızca aktif yoğun bakım yatışlarının bakım planı güncellenebilir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            admission.UpdateCarePlan(
                dto.AcuityLevel,
                dto.MonitoringFrequencyMinutes,
                dto.VentilationMode,
                dto.PrimaryNurseId,
                dto.CarePlanNotes,
                now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("UpdateCarePlan", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.IcuCarePlanUpdate",
            admission.Id.ToString(),
            "Yoğun bakım bakım planı güncellendi",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                Acuity = dto.AcuityLevel.ToString(),
                Ventilation = dto.VentilationMode.ToString(),
                Frequency = dto.MonitoringFrequencyMinutes,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(admission));
    }

    public async Task<SurgeryOperationResult<IcuAdmissionDto>> DischargeOrTransferAsync(
        Guid admissionId,
        IcuDischargeOrTransferDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.DischargeNotes))
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("DischargeNotes", "Çıkış / devir notu belirtilmelidir.");
        }

        var admission = await _dbContext.IcuAdmissions.FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return SurgeryOperationResult.NotFound<IcuAdmissionDto>("Yoğun bakım kabul kaydı bulunamadı.");
        }

        if (admission.Status != IcuAdmissionStatus.Active)
        {
            return SurgeryOperationResult.Conflict<IcuAdmissionDto>("Bu yoğun bakım yatışı zaten sonlandırılmıştır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            admission.TransferOutOrDischarge(dto.DestinationStatus, dto.DischargeNotes, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<IcuAdmissionDto>("Discharge", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.IcuDischargeOrTransfer",
            admission.Id.ToString(),
            "Hasta yoğun bakımdan sevk/taburcu edildi",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                Status = dto.DestinationStatus.ToString(),
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(admission));
    }

    public async Task<IcuAdmissionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.IcuAdmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return admission is null ? null : MapToDto(admission);
    }

    public async Task<List<IcuAdmissionDto>> GetActiveAdmissionsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.IcuAdmissions
            .AsNoTracking()
            .Where(a => a.Status == IcuAdmissionStatus.Active)
            .OrderByDescending(a => a.AdmittedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    private static IcuAdmissionDto MapToDto(IcuAdmission a) =>
        new(
            a.Id,
            a.AdmissionProtocolNumber,
            a.InpatientStayId,
            a.PatientId,
            a.EncounterId,
            a.IcuBedId,
            a.IcuBedCode,
            a.AttendingDoctorId,
            a.PrimaryNurseId,
            a.AdmissionReason,
            a.AcuityLevel,
            a.MonitoringFrequencyMinutes,
            a.VentilationMode,
            a.Status,
            a.CarePlanNotes,
            a.AdmittedAtUtc,
            a.DischargedAtUtc,
            a.DischargeNotes,
            a.CreatedAtUtc,
            a.UpdatedAtUtc,
            a.Version);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        string reason,
        Guid actorUserId,
        string? detailsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: actorUserId != Guid.Empty ? actorUserId : null,
            ActorPersonId: null,
            ActorRole: null,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "IcuAdmission",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
