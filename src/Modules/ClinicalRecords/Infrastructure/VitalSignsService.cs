using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class VitalSignsService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IVitalSignsService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<VitalSignObservationDto>> RecordObservationAsync(
        ClaimsPrincipal actor,
        RecordVitalSignObservationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignObservationDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        var (isValid, errorMessage) = VitalSignValidationRules.Validate(command.MeasurementType, command.Value, command.Unit);
        if (!isValid)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignObservationDto>(
                "value", errorMessage ?? "Geçersiz değer veya birim.");
        }

        if (!await CanRecordForResourceAsync(
                actor,
                command.PatientId,
                command.EncounterId,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<VitalSignObservationDto>(
                "Vital bulgu kaydetme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var observationId = Guid.NewGuid();

        var observation = VitalSignObservation.Create(
            observationId,
            command.PatientId,
            command.EncounterId,
            command.MeasurementType,
            command.Value,
            command.Unit,
            command.MeasurementMethod,
            command.MeasuredAtUtc ?? nowUtc,
            command.Notes,
            practitionerId,
            nowUtc);

        _dbContext.VitalSignObservations.Add(observation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.VitalSignRecord",
            observation.Id.ToString(),
            AuditOutcome.Success,
            "Vital bulgu kaydedildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(observation));
    }

    public async Task<ClinicalEncounterOperationResult<VitalSignsPanelDto>> RecordPanelAsync(
        ClaimsPrincipal actor,
        RecordVitalSignsPanelCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignsPanelDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await CanRecordForResourceAsync(
                actor,
                command.PatientId,
                command.EncounterId,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<VitalSignsPanelDto>(
                "Vital bulgu paneli kaydetme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var observations = new List<VitalSignObservation>();
        string? panelValidationError = null;

        void TryAddObservation(VitalSignType type, decimal? value, string unit)
        {
            if (!value.HasValue)
            {
                return;
            }

            var (isValid, errorMessage) = VitalSignValidationRules.Validate(type, value.Value, unit);
            if (!isValid)
            {
                panelValidationError ??= errorMessage ?? "Geçersiz vital bulgu değeri.";
                return;
            }

            var obs = VitalSignObservation.Create(
                Guid.NewGuid(),
                command.PatientId,
                command.EncounterId,
                type,
                value.Value,
                unit,
                null,
                nowUtc,
                command.Notes,
                practitionerId,
                nowUtc);

            observations.Add(obs);
        }

        TryAddObservation(VitalSignType.BodyTemperature, command.TemperatureCelsius, "°C");
        TryAddObservation(VitalSignType.BloodPressureSystolic, command.SystolicBloodPressureMmHg, "mmHg");
        TryAddObservation(VitalSignType.BloodPressureDiastolic, command.DiastolicBloodPressureMmHg, "mmHg");
        TryAddObservation(VitalSignType.HeartRate, command.HeartRateBpm, "bpm");
        TryAddObservation(VitalSignType.RespiratoryRate, command.RespiratoryRatePerMin, "/dk");
        TryAddObservation(VitalSignType.OxygenSaturation, command.OxygenSaturationPercent, "%");
        TryAddObservation(VitalSignType.BodyWeight, command.BodyWeightKg, "kg");
        TryAddObservation(VitalSignType.BodyHeight, command.BodyHeightCm, "cm");
        TryAddObservation(VitalSignType.BloodGlucose, command.BloodGlucoseMgDl, "mg/dL");
        TryAddObservation(VitalSignType.PainScore, command.PainScore, "skor");

        // Calculate BMI if both height and weight are provided
        if (command.BodyHeightCm.HasValue && command.BodyWeightKg.HasValue && command.BodyHeightCm.Value > 0)
        {
            var heightMeters = command.BodyHeightCm.Value / 100m;
            var bmi = Math.Round(command.BodyWeightKg.Value / (heightMeters * heightMeters), 1);
            TryAddObservation(VitalSignType.BodyMassIndex, bmi, "kg/m²");
        }

        if (observations.Count == 0)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignsPanelDto>(
                "panel", "En az bir geçerli vital bulgu değeri girilmelidir.");
        }

        if (panelValidationError is not null)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignsPanelDto>(
                "panel", panelValidationError);
        }

        _dbContext.VitalSignObservations.AddRange(observations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.VitalSignPanelRecord",
            command.PatientId.ToString(),
            AuditOutcome.Success,
            "Vital bulgu paneli atomik olarak kaydedildi.",
            cancellationToken);

        var dtos = observations.Select(MapToDto).ToList();
        var panelDto = new VitalSignsPanelDto(
            command.PatientId,
            command.EncounterId,
            dtos,
            command.ConsciousnessState,
            nowUtc);

        return ClinicalEncounterOperationResult.Success(panelDto);
    }

    public async Task<ClinicalEncounterOperationResult<VitalSignObservationDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkVitalSignEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignObservationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<VitalSignObservationDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var observation = await _dbContext.VitalSignObservations
            .FirstOrDefaultAsync(v => v.Id == command.ObservationId, cancellationToken);

        if (observation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<VitalSignObservationDto>(
                "Vital bulgu kaydı bulunamadı.");
        }

        if (!await CanRecordForResourceAsync(
                actor,
                observation.PatientId,
                observation.EncounterId,
                cancellationToken,
                requireOpenEncounter: false)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<VitalSignObservationDto>(
                "Vital bulguyu hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(observation).Property(v => v.Version).OriginalValue = command.ExpectedVersion;
        observation.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return ClinicalEncounterOperationResult.Conflict<VitalSignObservationDto>(
                "Vital bulgu başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.VitalSignEnteredInError",
            observation.Id.ToString(),
            AuditOutcome.Success,
            "Vital bulgu gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(observation));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<VitalSignObservationDto>>> GetPatientObservationsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<VitalSignObservationDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<VitalSignObservationDto>>();
        }

        var observationQuery = _dbContext.VitalSignObservations
            .AsNoTracking()
            .Where(v => v.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            observationQuery = observationQuery.Where(v => !v.IsEnteredInError);
        }

        var observations = await observationQuery
            .OrderByDescending(v => v.MeasuredAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.VitalSignView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta vital bulguları görüntülendi.",
            cancellationToken);

        var dtos = observations.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<VitalSignObservationDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<VitalSignObservationDto>>> GetEncounterObservationsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<VitalSignObservationDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<IReadOnlyList<VitalSignObservationDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<VitalSignObservationDto>>();
        }

        var observationQuery = _dbContext.VitalSignObservations
            .AsNoTracking()
            .Where(v => v.EncounterId == encounterId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId))
        {
            observationQuery = observationQuery.Where(v => !v.IsEnteredInError);
        }

        var observations = await observationQuery
            .OrderBy(v => v.MeasuredAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = observations.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<VitalSignObservationDto>>(dtos);
    }

    private async Task<bool> CanRecordForResourceAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        Guid? encounterId,
        CancellationToken cancellationToken,
        bool requireOpenEncounter = true)
    {
        const string permission = HospitalPermissions.ClinicalRecords.ObservationRecordVital;
        if (!ClinicalRecordAccessControl.HasPermission(actor, permission))
        {
            return false;
        }

        if (!encounterId.HasValue)
        {
            return await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                permission,
                allowPatientOwnRecord: false,
                cancellationToken);
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId.Value, cancellationToken);
        if (encounter is null
            || encounter.PatientId != patientId
            || (requireOpenEncounter
                && !ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter)))
        {
            return false;
        }

        return await _accessControl.CanAccessEncounterAsync(
            actor,
            encounter,
            permission,
            allowPatientOwnRecord: false,
            cancellationToken);
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string reason,
        CancellationToken cancellationToken)
    {
        Guid? actorUserId = null;
        Guid? actorPersonId = null;

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var uid))
        {
            actorUserId = uid;
        }

        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var pid))
        {
            actorPersonId = pid;
        }

        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "ClinicalRecords",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static VitalSignObservationDto MapToDto(VitalSignObservation v)
    {
        return new VitalSignObservationDto(
            v.Id,
            v.PatientId,
            v.EncounterId,
            v.MeasurementType,
            v.Value,
            v.Unit,
            v.Interpretation,
            v.MeasurementMethod,
            v.MeasuredAtUtc,
            v.Notes,
            v.RecordedByPractitionerId,
            v.RecordedAtUtc,
            v.UpdatedAtUtc,
            v.IsEnteredInError,
            v.EnteredInErrorReason,
            v.Version);
    }
}
