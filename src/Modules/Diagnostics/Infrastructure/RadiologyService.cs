using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class RadiologyService : IRadiologyService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public RadiologyService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IDiagnosticsAccessContext accessContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<RadiologyCatalogItemDto>>> GetCatalogItemsAsync(
        ClaimsPrincipal actor,
        RadiologyModality? modality = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var query = _dbContext.RadiologyCatalogItems
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (modality.HasValue)
        {
            query = query.Where(c => c.Modality == modality.Value);
        }

        var items = await query
            .OrderBy(c => c.Modality)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapCatalogToDto).ToList();
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<RadiologyCatalogItemDto>>(dtos);
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> EnsureStudyForOrderItemAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Tanısal istem kaydı bulunamadı.");
        }

        if (order.OrderType != DiagnosticOrderType.Radiology)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>(
                "orderId",
                "Radyoloji çalışması yalnızca radyoloji isteminden oluşturulabilir.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.RadiologyWorklistView, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var existing = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.DiagnosticOrderId == orderId && s.DiagnosticOrderItemId == orderItemId, cancellationToken);
        if (existing != null)
        {
            return DiagnosticOrderOperationResult.Success(MapToDetailDto(existing));
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("İstem kalemi bulunamadı.");
        }

        var catalogItem = await _dbContext.RadiologyCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == orderItem.CatalogCode, cancellationToken);

        var modality = catalogItem?.Modality ?? RadiologyModality.XR;
        var bodySite = catalogItem?.BodySite ?? "Genel";
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var accessionNumber = GenerateAccessionNumber(nowUtc);

        var study = RadiologyStudy.Create(
            Guid.NewGuid(),
            orderId,
            orderItemId,
            order.PatientId,
            accessionNumber,
            modality,
            orderItem.CatalogCode,
            orderItem.CatalogItemName,
            bodySite,
            nowUtc);

        _dbContext.RadiologyStudies.Add(study);
        if (order.Status == DiagnosticOrderStatus.Placed)
        {
            order.StartProcessing(nowUtc);
        }
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            _dbContext.ChangeTracker.Clear();
            var concurrentlyCreated = await _dbContext.RadiologyStudies
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DiagnosticOrderItemId == orderItemId, cancellationToken);

            return concurrentlyCreated is null
                ? DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>(
                    "Radyoloji çalışması eşzamanlı bir işlem nedeniyle oluşturulamadı; lütfen yeniden deneyin.")
                : DiagnosticOrderOperationResult.Success(MapToDetailDto(concurrentlyCreated));
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            var concurrentlyCreated = await _dbContext.RadiologyStudies
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DiagnosticOrderItemId == orderItemId, cancellationToken);

            return concurrentlyCreated is null
                ? DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>(
                    "Radyoloji istemi başka bir işlem tarafından güncellendi; lütfen yeniden deneyin.")
                : DiagnosticOrderOperationResult.Success(MapToDetailDto(concurrentlyCreated));
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyStudyCreate",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Radyoloji çekim kaydı oluşturuldu: {study.AccessionNumber} - {study.ProcedureName}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> ScheduleAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyStudyComplete, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.Schedule(scheduledAtUtc, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyStudySchedule",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Radyoloji çekimi randevulandı: {study.AccessionNumber} Tarih: {scheduledAtUtc:yyyy-MM-dd HH:mm}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> CompleteAcquisitionAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string? technicianNotes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyStudyComplete, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.CompleteAcquisition(actorUserId, technicianNotes, nowUtc);

            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == study.DiagnosticOrderId, cancellationToken);

            var orderItem = order?.Items.FirstOrDefault(i => i.Id == study.DiagnosticOrderItemId);
            if (orderItem != null && orderItem.Status == DiagnosticOrderItemStatus.Pending)
            {
                orderItem.UpdateStatus(DiagnosticOrderItemStatus.InAnalysis, nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyAcquisitionComplete",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Görüntü çekimi teknisyen tarafından tamamlandı: {study.AccessionNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> DraftReportAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reportText,
        string? impression,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(reportText))
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("ReportText", "Rapor metni zorunludur.");
        }

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyReportFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.DraftReport(actorUserId, reportText, impression, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyReportDraft",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Radyoloji rapor taslağı kaydedildi: {study.AccessionNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> FinalizeReportAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reportText,
        string impression,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(reportText))
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("ReportText", "Rapor metni zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(impression))
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Impression", "Klinik sonuç/kanaat (Impression) zorunludur.");
        }

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyReportFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.FinalizeReport(actorUserId, reportText, impression, nowUtc);

            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == study.DiagnosticOrderId, cancellationToken);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == study.DiagnosticOrderItemId);
                if (orderItem != null && orderItem.Status != DiagnosticOrderItemStatus.Reported)
                {
                    orderItem.UpdateStatus(DiagnosticOrderItemStatus.Reported, nowUtc);
                }

                if (order.Items.All(i => i.Status == DiagnosticOrderItemStatus.Reported || i.Status == DiagnosticOrderItemStatus.Cancelled))
                {
                    order.Complete(nowUtc);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyReportFinalize",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Radyoloji raporu uzman radyolog tarafından kesinleştirildi: {study.AccessionNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> AddAddendumAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string addendumText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(addendumText))
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("AddendumText", "Ek rapor metni zorunludur.");
        }

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyReportFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.AddAddendum(actorUserId, addendumText, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyReportAddendum",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Kesinleşmiş radyoloji raporuna ek rapor (addendum) eklendi: {study.AccessionNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Reason", "İptal gerekçesi zorunludur.");
        }

        var study = await _dbContext.RadiologyStudies
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyReportFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            study.Cancel(reason, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<RadiologyStudyDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<RadiologyStudyDetailDto>("Çekim kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.RadiologyStudyCancel",
            "RadiologyStudy",
            study.Id.ToString(),
            $"Radyoloji çekimi iptal edildi: {study.AccessionNumber}. Gerekçe: {reason}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<RadiologyStudyDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var study = await _dbContext.RadiologyStudies
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<RadiologyStudyDetailDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        var readPermission = IsPatient(actor)
            ? HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn
            : actor.IsInRole(HospitalRoles.RadiologyStaff)
                ? HospitalPermissions.Diagnostics.RadiologyWorklistView
                : HospitalPermissions.ClinicalRecords.EncounterView;
        if (!await CanAccessStudyAsync(actor, study, readPermission, IsPatient(actor), cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>();
        }

        if (IsPatient(actor))
        {
            var patientId = GetActorPatientId(actor);
            if (study.PatientId != patientId)
            {
                return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>("Yalnızca kendi radyoloji tetkiklerinize erişebilirsiniz.");
            }

            if (study.Status != RadiologyStudyStatus.ReportFinalized && study.Status != RadiologyStudyStatus.AddendumAdded)
            {
                return DiagnosticOrderOperationResult.Forbidden<RadiologyStudyDetailDto>("Hastalar yalnızca kesinleşmiş veya ek raporlu radyoloji sonuçlarını görüntüleyebilir.");
            }
        }

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(study));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<RadiologyStudySummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        RadiologyModality? modality = null,
        RadiologyStudyStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var query = _dbContext.RadiologyStudies
            .AsNoTracking();

        if (IsPatient(actor))
        {
            var actorPatientId = GetActorPatientId(actor);
            query = query.Where(s => s.PatientId == actorPatientId &&
                (s.Status == RadiologyStudyStatus.ReportFinalized || s.Status == RadiologyStudyStatus.AddendumAdded));
        }
        else
        {
            if (patientId.HasValue && patientId.Value != Guid.Empty)
            {
                query = query.Where(s => s.PatientId == patientId.Value);
            }

            if (modality.HasValue)
            {
                query = query.Where(s => s.Modality == modality.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }
        }

        var list = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (IsPatient(actor))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<RadiologyStudySummaryDto>>();
        }

        var dtos = new List<RadiologyStudySummaryDto>();
        foreach (var study in list)
        {
            if (await CanAccessStudyAsync(actor, study, HospitalPermissions.Diagnostics.RadiologyWorklistView, false, cancellationToken))
            {
                dtos.Add(MapToSummaryDto(study));
            }
        }
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<RadiologyStudySummaryDto>>(dtos);
    }

    private static RadiologyCatalogItemDto MapCatalogToDto(RadiologyCatalogItem c) =>
        new(
            c.Id,
            c.Code,
            c.Name,
            c.Modality,
            c.BodySite,
            c.Description,
            c.PreparationInstructions,
            c.ContrastRequired,
            c.EstimatedDurationMinutes,
            c.IsActive);

    private static RadiologyStudyDetailDto MapToDetailDto(RadiologyStudy s) =>
        new(
            s.Id,
            s.DiagnosticOrderId,
            s.DiagnosticOrderItemId,
            s.PatientId,
            s.AccessionNumber,
            s.Modality,
            s.ProcedureCode,
            s.ProcedureName,
            s.BodySite,
            s.Status,
            s.ScheduledAtUtc,
            s.PerformedAtUtc,
            s.TechnicianUserId,
            s.TechnicianNotes,
            s.RadiologistUserId,
            s.ReportText,
            s.Impression,
            s.ReportDraftedAtUtc,
            s.ReportFinalizedAtUtc,
            s.AddendumText,
            s.AddendumAddedAtUtc,
            s.AddendumByUserId,
            s.CancellationReason,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.Version);

    private static RadiologyStudySummaryDto MapToSummaryDto(RadiologyStudy s) =>
        new(
            s.Id,
            s.DiagnosticOrderId,
            s.PatientId,
            s.AccessionNumber,
            s.Modality,
            s.ProcedureCode,
            s.ProcedureName,
            s.BodySite,
            s.Status,
            s.ScheduledAtUtc,
            s.PerformedAtUtc,
            s.ReportFinalizedAtUtc,
            s.CreatedAtUtc);

    private static string GenerateAccessionNumber(DateTime nowUtc)
    {
        var randomSuffix = Random.Shared.Next(100000, 999999);
        return $"DEMO-ACC-{nowUtc:yyyyMMdd}-{randomSuffix}";
    }

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private async Task<bool> CanAccessStudyAsync(
        ClaimsPrincipal actor,
        RadiologyStudy study,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == study.DiagnosticOrderId, cancellationToken);
        return order is not null
            && await CanAccessOrderAsync(actor, order, permission, allowPatientOwnFinalResult, cancellationToken);
    }

    private Task<bool> CanAccessOrderAsync(
        ClaimsPrincipal actor,
        DiagnosticOrder order,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken) =>
        _accessContext.CanAccessResourceAsync(
            actor,
            new DiagnosticResourceContext(order.EncounterId, order.PatientId, order.DepartmentId, order.OrderType),
            permission,
            allowPatientOwnFinalResult,
            cancellationToken);

    private static Guid GetActorUserId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid GetActorPatientId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value
            ?? actor.FindFirst("PatientId")?.Value
            ?? actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var pid) ? pid : Guid.Empty;
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceType,
        string targetResourceId,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var actorUserId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var actorPersonId = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value;

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: Guid.TryParse(actorUserId, out var uid) ? uid : null,
            ActorPersonId: Guid.TryParse(actorPersonId, out var personId) ? personId : null,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: targetResourceType,
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: null,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
