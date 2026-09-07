using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class LabResultService : ILabResultService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly ICriticalResultNotificationService _criticalNotificationService;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public LabResultService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        ICriticalResultNotificationService criticalNotificationService,
        IDiagnosticsAccessContext accessContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _criticalNotificationService = criticalNotificationService ?? throw new ArgumentNullException(nameof(criticalNotificationService));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreateDraftLabResultCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.DiagnosticOrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Tanısal istem bulunamadı.");
        }

        if (order.OrderType != DiagnosticOrderType.Laboratory
            || order.Status is DiagnosticOrderStatus.Draft or DiagnosticOrderStatus.Cancelled or DiagnosticOrderStatus.EnteredInError)
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>(
                "DiagnosticOrderId",
                "Sonuç yalnızca aktif bir laboratuvar istemine girilebilir.");
        }

        if (!await CanAccessOrderAsync(
                actor,
                order,
                HospitalPermissions.Diagnostics.LaboratoryResultEditDraft,
                false,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>(
                "Bu laboratuvar istemi için sonuç girişi atamanız bulunmamaktadır.");
        }

        if (order.PatientId != command.PatientId)
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>(
                "PatientId",
                "Sonucun hasta kimliği ile istemin hasta kimliği uyuşmuyor.");
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == command.DiagnosticOrderItemId);
        if (orderItem is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("İstem kalemi bulunamadı.");
        }

        if (!orderItem.CatalogCode.Equals(command.CatalogCode, StringComparison.OrdinalIgnoreCase)
            || !orderItem.CatalogItemName.Equals(command.CatalogItemName, StringComparison.Ordinal))
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>(
                "CatalogCode",
                "Sonuç kataloğu istem kalemiyle uyuşmuyor.");
        }

        if (await _dbContext.LabResults
            .AsNoTracking()
            .AnyAsync(
                result => result.DiagnosticOrderItemId == orderItem.Id && result.PreviousResultId == null,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>(
                "Bu istem kalemi için bir laboratuvar sonuç zinciri zaten bulunmaktadır.");
        }

        if (command.SpecimenId.HasValue)
        {
            var validSpecimen = await _dbContext.Specimens
                .AsNoTracking()
                .AnyAsync(
                    specimen => specimen.Id == command.SpecimenId.Value
                        && specimen.DiagnosticOrderId == order.Id
                        && specimen.PatientId == order.PatientId
                        && specimen.Status != SpecimenStatus.Rejected,
                    cancellationToken);
            if (!validSpecimen)
            {
                return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>(
                    "SpecimenId",
                    "Numune istem/hasta bağlamıyla uyuşmuyor veya reddedilmiş.");
            }
        }

        // Fetch catalog parameters if available to scaffold result items
        var catalogItem = await _dbContext.LabCatalogItems
            .Include(c => c.Parameters)
            .FirstOrDefaultAsync(c => c.Code == command.CatalogCode, cancellationToken);

        var resultId = Guid.NewGuid();
        var resultItems = new List<LabResultItem>();

        var valueDict = command.ParameterValues?.ToDictionary(v => v.ParameterCode.ToUpperInvariant(), v => v)
            ?? new Dictionary<string, ParameterValueCommand>();

        if (catalogItem != null && catalogItem.Parameters.Count > 0)
        {
            foreach (var p in catalogItem.Parameters.OrderBy(p => p.SortOrder))
            {
                valueDict.TryGetValue(p.Code.ToUpperInvariant(), out var val);

                var item = LabResultItem.Create(
                    Guid.NewGuid(),
                    resultId,
                    p.Code,
                    p.Name,
                    val?.NumericValue,
                    val?.StringValue,
                    p.Unit,
                    p.ReferenceRangeLow,
                    p.ReferenceRangeHigh,
                    p.ReferenceRangeLow.HasValue && p.ReferenceRangeHigh.HasValue ? $"{p.ReferenceRangeLow} - {p.ReferenceRangeHigh}" : null,
                    val?.Notes);

                resultItems.Add(item);
            }
        }
        else if (command.ParameterValues != null)
        {
            foreach (var val in command.ParameterValues)
            {
                var item = LabResultItem.Create(
                    Guid.NewGuid(),
                    resultId,
                    val.ParameterCode,
                    val.ParameterCode,
                    val.NumericValue,
                    val.StringValue,
                    null,
                    null,
                    null,
                    null,
                    val.Notes);

                resultItems.Add(item);
            }
        }

        var labResult = LabResult.CreateDraft(
            resultId,
            command.DiagnosticOrderId,
            command.DiagnosticOrderItemId,
            command.SpecimenId,
            command.PatientId,
            command.CatalogCode,
            command.CatalogItemName,
            command.ClinicalNotes,
            nowUtc,
            resultItems);

        _dbContext.LabResults.Add(labResult);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>("Laboratuvar sonuç kaydı başka bir işlem tarafından güncellendi.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>(
                "Bu istem kalemi için sonuç taslağı eşzamanlı başka bir işlem tarafından oluşturuldu.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.LabResultCreateDraft",
            "LabResult",
            labResult.Id.ToString(),
            $"Taslak laboratuvar sonucu oluşturuldu: {labResult.CatalogCode} ({labResult.CatalogItemName})",
            nowUtc,
            cancellationToken);

        await _criticalNotificationService.ProcessAndNotifyCriticalResultsAsync(labResult, cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(labResult));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> UpdateItemsAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        UpdateLabResultItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var labResult = await _dbContext.LabResults
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (labResult is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Laboratuvar sonuç kaydı bulunamadı.");
        }

        if (!await CanAccessLabResultAsync(actor, labResult, HospitalPermissions.Diagnostics.LaboratoryResultEditDraft, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>();
        }

        try
        {
            var tuples = command.ParameterValues.Select(v => (v.ParameterCode, v.NumericValue, v.StringValue, v.Notes));
            labResult.UpdateItems(tuples, command.ClinicalNotes, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>("Laboratuvar sonuç kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.LabResultUpdateDraft",
            "LabResult",
            labResult.Id.ToString(),
            $"Taslak laboratuvar sonucu parametreleri güncellendi: {labResult.CatalogCode}",
            nowUtc,
            cancellationToken);

        await _criticalNotificationService.ProcessAndNotifyCriticalResultsAsync(labResult, cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(labResult));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> ApproveTechnicallyAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var labResult = await _dbContext.LabResults
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (labResult is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Laboratuvar sonuç kaydı bulunamadı.");
        }

        if (!await CanAccessLabResultAsync(actor, labResult, HospitalPermissions.Diagnostics.LaboratoryResultEditDraft, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>();
        }

        try
        {
            labResult.ApproveTechnically(actorUserId, nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>("Laboratuvar sonuç kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.LabResultTechnicalApprove",
            "LabResult",
            labResult.Id.ToString(),
            $"Laboratuvar sonucuna teknisyen teknik onayı verildi: {labResult.CatalogCode}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(labResult));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> ApproveClinicallyAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var labResult = await _dbContext.LabResults
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (labResult is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Laboratuvar sonuç kaydı bulunamadı.");
        }

        if (!await CanAccessLabResultAsync(actor, labResult, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>();
        }

        try
        {
            labResult.ApproveClinically(actorUserId, nowUtc);

            // Also advance diagnostic order item status
            var order = await _dbContext.DiagnosticOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == labResult.DiagnosticOrderId, cancellationToken);

            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == labResult.DiagnosticOrderItemId);
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
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>("Laboratuvar sonuç kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.LabResultClinicalApprove",
            "LabResult",
            labResult.Id.ToString(),
            $"Laboratuvar sonucuna uzman hekim klinik onayı verildi: {labResult.CatalogCode}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(labResult));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> CorrectAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CorrectLabResultCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.CorrectionReason))
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>(
                "CorrectionReason",
                "Sonuç düzeltmesi için zorunlu gerekçe girilmelidir.");
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var previousResult = await _dbContext.LabResults
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (previousResult is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Düzeltilecek laboratuvar sonucu bulunamadı.");
        }

        if (!await CanAccessLabResultAsync(actor, previousResult, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, false, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>();
        }

        LabResult newResult;
        try
        {
            var tuples = command.CorrectedValues.Select(v => (v.ParameterCode, v.NumericValue, v.StringValue, v.Notes));
            newResult = previousResult.CreateCorrection(
                Guid.NewGuid(),
                command.CorrectionReason,
                actorUserId,
                nowUtc,
                tuples);

            _dbContext.LabResults.Add(newResult);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<LabResultDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<LabResultDetailDto>("Laboratuvar sonuç kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.LabResultCorrect",
            "LabResult",
            newResult.Id.ToString(),
            $"Laboratuvar sonucuna düzeltme uygulandı (Önceki: {previousResult.Id}). Gerekçe: {command.CorrectionReason}",
            nowUtc,
            cancellationToken);

        await _criticalNotificationService.ProcessAndNotifyCriticalResultsAsync(newResult, cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(newResult));
    }

    public async Task<DiagnosticOrderOperationResult<LabResultDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var labResult = await _dbContext.LabResults
            .Include(r => r.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);

        if (labResult is null)
        {
            return DiagnosticOrderOperationResult.NotFound<LabResultDetailDto>("Laboratuvar sonucu bulunamadı.");
        }

        var readPermission = IsPatient(actor)
            ? HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn
            : actor.IsInRole(HospitalRoles.LaboratoryStaff)
                ? HospitalPermissions.Diagnostics.LaboratoryWorklistView
                : HospitalPermissions.ClinicalRecords.EncounterView;
        if (!await CanAccessLabResultAsync(actor, labResult, readPermission, IsPatient(actor), cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>(
                "Bu sonuç için gerekli izin, kaynak kapsamı veya bakım ilişkisi bulunmamaktadır.");
        }

        if (IsPatient(actor))
        {
            var patientId = GetActorPatientId(actor);
            if (patientId != labResult.PatientId)
            {
                return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>("Yalnızca kendi sonuçlarınıza erişebilirsiniz.");
            }

            // Patients can only view final approved or corrected results
            if (labResult.Status != LabResultStatus.FinalApproved && labResult.Status != LabResultStatus.Corrected)
            {
                return DiagnosticOrderOperationResult.Forbidden<LabResultDetailDto>("Taslak veya henüz kesinleşmemiş sonuçlar hasta portalında görüntülenemez.");
            }
        }

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(labResult));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<LabResultDetailDto>>> GetByOrderIdAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<IReadOnlyList<LabResultDetailDto>>("Tanısal istem bulunamadı.");
        }

        var readPermission = IsPatient(actor)
            ? HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn
            : actor.IsInRole(HospitalRoles.LaboratoryStaff)
                ? HospitalPermissions.Diagnostics.LaboratoryWorklistView
                : HospitalPermissions.ClinicalRecords.EncounterView;
        if (!await CanAccessOrderAsync(actor, order, readPermission, IsPatient(actor), cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<LabResultDetailDto>>();
        }

        var query = _dbContext.LabResults
            .Include(r => r.Items)
            .AsNoTracking()
            .Where(r => r.DiagnosticOrderId == orderId);

        if (IsPatient(actor))
        {
            var patientId = GetActorPatientId(actor);
            query = query.Where(r => r.PatientId == patientId &&
                (r.Status == LabResultStatus.FinalApproved || r.Status == LabResultStatus.Corrected));
        }

        var results = await query
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = results.Select(MapToDetailDto).ToList();
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<LabResultDetailDto>>(dtos);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<LabResultSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        LabResultStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (IsPatient(actor)
            || !actor.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Diagnostics.LaboratoryWorklistView))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<LabResultSummaryDto>>();
        }

        var query = _dbContext.LabResults
            .Include(r => r.Items)
            .AsNoTracking();

        if (IsPatient(actor))
        {
            var actorPatientId = GetActorPatientId(actor);
            query = query.Where(r => r.PatientId == actorPatientId &&
                (r.Status == LabResultStatus.FinalApproved || r.Status == LabResultStatus.Corrected));
        }
        else
        {
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (patientId.HasValue)
            {
                query = query.Where(r => r.PatientId == patientId.Value);
            }
        }

        var results = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        var dtos = new List<LabResultSummaryDto>();
        foreach (var result in results)
        {
            if (await CanAccessLabResultAsync(actor, result, HospitalPermissions.Diagnostics.LaboratoryWorklistView, false, cancellationToken))
            {
                dtos.Add(new LabResultSummaryDto(
                    result.Id,
                    result.DiagnosticOrderId,
                    result.DiagnosticOrderItemId,
                    result.PatientId,
                    result.CatalogCode,
                    result.CatalogItemName,
                    result.Status,
                    result.Items.Any(i => i.Flag == LabResultInterpretation.CriticalLow || i.Flag == LabResultInterpretation.CriticalHigh),
                    result.Items.Any(i => i.Flag == LabResultInterpretation.Low || i.Flag == LabResultInterpretation.High || i.Flag == LabResultInterpretation.Abnormal),
                    result.CreatedAtUtc,
                    result.ClinicallyApprovedAtUtc));
            }
        }

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<LabResultSummaryDto>>(dtos);
    }

    private static LabResultDetailDto MapToDetailDto(LabResult r) =>
        new(
            r.Id,
            r.DiagnosticOrderId,
            r.DiagnosticOrderItemId,
            r.SpecimenId,
            r.PatientId,
            r.CatalogCode,
            r.CatalogItemName,
            r.Status,
            r.TechnicallyApprovedByUserId,
            r.TechnicallyApprovedAtUtc,
            r.ClinicallyApprovedByUserId,
            r.ClinicallyApprovedAtUtc,
            r.PreviousResultId,
            r.CorrectionReason,
            r.ClinicalNotes,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.Version,
            r.Items.Select(i => new LabResultItemDto(
                i.Id,
                i.LabResultId,
                i.ParameterCode,
                i.ParameterName,
                i.NumericValue,
                i.StringValue,
                i.Unit,
                i.ReferenceRangeLow,
                i.ReferenceRangeHigh,
                i.ReferenceRangeText,
                i.Flag,
                i.Notes)).ToList());

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private async Task<bool> CanAccessLabResultAsync(
        ClaimsPrincipal actor,
        LabResult result,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == result.DiagnosticOrderId, cancellationToken);
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
        var claim = actor.FindFirst("PatientId")?.Value ?? actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
