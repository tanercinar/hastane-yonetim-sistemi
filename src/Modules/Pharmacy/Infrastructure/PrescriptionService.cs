using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public sealed class PrescriptionService : IPrescriptionService
{
    private readonly PharmacyDbContext _dbContext;
    private readonly IAuditEventPublisher _auditEventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly IMedicationSafetyChecker _safetyChecker;
    private readonly IPrescriptionAccessContext _accessContext;

    public PrescriptionService(
        PharmacyDbContext dbContext,
        IAuditEventPublisher auditEventPublisher,
        TimeProvider timeProvider,
        IMedicationSafetyChecker safetyChecker,
        IPrescriptionAccessContext accessContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditEventPublisher = auditEventPublisher ?? throw new ArgumentNullException(nameof(auditEventPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _safetyChecker = safetyChecker ?? throw new ArgumentNullException(nameof(safetyChecker));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreatePrescriptionDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionCreate)
            || !TryExtractActorPersonId(actor, out var doctorId))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçete taslağı oluşturma yetkiniz bulunmamaktadır.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (command.EncounterId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.DepartmentId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "departmentId", "Bölüm kimliği zorunludur.");
        }

        if (command.PrescribingDoctorId != Guid.Empty
            && command.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Başka bir hekim adına reçete taslağı oluşturamazsınız.");
        }

        var encounter = await _accessContext.FindEncounterAsync(
            command.EncounterId,
            cancellationToken);
        if (encounter is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>(
                "Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId
            || encounter.DepartmentId != command.DepartmentId)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "encounterId",
                "Hasta veya bölüm bilgisi karşılaşma ile eşleşmiyor.");
        }

        if (!encounter.AllowsClinicalEntry)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete yalnızca klinik girişe açık bir karşılaşmada oluşturulabilir.");
        }

        if (!await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionCreate,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Bu karşılaşma için reçete oluşturma yetkiniz bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var prescriptionId = Guid.NewGuid();
        var rxNumber = GeneratePrescriptionNumber(nowUtc);

        var prescription = Prescription.CreateDraft(
            prescriptionId,
            rxNumber,
            command.PatientId,
            command.EncounterId,
            doctorId,
            command.DepartmentId,
            command.DiagnosisSummary,
            command.GeneralInstructions,
            nowUtc);

        if (command.Items is { Count: > 0 })
        {
            var medIds = command.Items.Select(i => i.MedicationCatalogItemId).Distinct().ToList();
            var meds = await _dbContext.MedicationCatalogItems
                .Where(m => medIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, cancellationToken);

            foreach (var itemCmd in command.Items)
            {
                if (!meds.TryGetValue(itemCmd.MedicationCatalogItemId, out var med))
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "items", $"'{itemCmd.MedicationCatalogItemId}' kimlikli ilaç katalogda bulunamadı.");
                }

                if (itemCmd.Dose <= 0)
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "dose", "İlaç dozu 0'dan büyük olmalıdır.");
                }

                if (itemCmd.DurationDays <= 0)
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "durationDays", "Kullanım süresi 0'dan büyük olmalıdır.");
                }

                if (itemCmd.Quantity <= 0)
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "quantity", "Reçete edilen miktar 0'dan büyük olmalıdır.");
                }

                prescription.AddItem(
                    Guid.NewGuid(),
                    med.Id,
                    med.Code,
                    med.BrandName,
                    med.GenericName,
                    med.Form,
                    med.Route,
                    itemCmd.Dose,
                    string.IsNullOrWhiteSpace(itemCmd.DoseUnit) ? med.StrengthUnit : itemCmd.DoseUnit,
                    string.IsNullOrWhiteSpace(itemCmd.Frequency) ? "1x1" : itemCmd.Frequency,
                    itemCmd.DurationDays,
                    itemCmd.Quantity,
                    string.IsNullOrWhiteSpace(itemCmd.QuantityUnit) ? "kutu" : itemCmd.QuantityUnit,
                    itemCmd.Instructions,
                    nowUtc);
            }
        }

        _dbContext.Prescriptions.Add(prescription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionCreateDraft",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete taslağı oluşturuldu.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> UpdateDraftAsync(
        ClaimsPrincipal actor,
        UpdatePrescriptionDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionCreate)
            || !TryExtractActorPersonId(actor, out var doctorId))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçete taslağı düzenleme yetkiniz bulunmamaktadır.");
        }

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        if (prescription.Status != PrescriptionStatus.Draft)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                $"Yalnızca taslak durumundaki reçeteler düzenlenebilir. Mevcut durum: '{prescription.Status}'.");
        }

        if (command.PrescribingDoctorId != Guid.Empty
            && command.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Başka bir hekim adına reçete taslağı düzenleyemezsiniz.");
        }

        if (prescription.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Yalnızca reçete taslağının yazarı düzenleme yapabilir.");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != prescription.Version)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var encounter = await _accessContext.FindEncounterAsync(
            prescription.EncounterId,
            cancellationToken);
        if (encounter is null
            || encounter.PatientId != prescription.PatientId
            || encounter.DepartmentId != prescription.DepartmentId)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçetenin bağlı olduğu karşılaşma geçersizdir.");
        }

        if (!encounter.AllowsClinicalEntry
            || !await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionCreate,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Bu karşılaşmadaki reçete taslağını düzenleme yetkiniz bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        prescription.UpdateDetails(
            command.DiagnosisSummary,
            command.GeneralInstructions,
            nowUtc);

        if (command.Items is not null)
        {
            _dbContext.PrescriptionItems.RemoveRange(prescription.Items);

            var medIds = command.Items.Select(i => i.MedicationCatalogItemId).Distinct().ToList();
            var meds = await _dbContext.MedicationCatalogItems
                .Where(m => medIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, cancellationToken);

            foreach (var itemCmd in command.Items)
            {
                if (!meds.TryGetValue(itemCmd.MedicationCatalogItemId, out var med))
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "items", $"'{itemCmd.MedicationCatalogItemId}' kimlikli ilaç katalogda bulunamadı.");
                }

                if (itemCmd.Dose <= 0 || itemCmd.DurationDays <= 0 || itemCmd.Quantity <= 0)
                {
                    return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                        "items",
                        "İlaç dozu, kullanım süresi ve miktarı 0'dan büyük olmalıdır.");
                }

                prescription.AddItem(
                    Guid.NewGuid(),
                    med.Id,
                    med.Code,
                    med.BrandName,
                    med.GenericName,
                    med.Form,
                    med.Route,
                    itemCmd.Dose,
                    string.IsNullOrWhiteSpace(itemCmd.DoseUnit) ? med.StrengthUnit : itemCmd.DoseUnit,
                    string.IsNullOrWhiteSpace(itemCmd.Frequency) ? "1x1" : itemCmd.Frequency,
                    itemCmd.DurationDays,
                    itemCmd.Quantity,
                    string.IsNullOrWhiteSpace(itemCmd.QuantityUnit) ? "kutu" : itemCmd.QuantityUnit,
                    itemCmd.Instructions,
                    nowUtc);
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionUpdateDraft",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete taslağı güncellendi.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> SignAsync(
        ClaimsPrincipal actor,
        SignPrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionSign)
            || !TryExtractActorPersonId(actor, out var doctorId))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçete imzalama yetkiniz bulunmamaktadır.");
        }

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        if (prescription.Status != PrescriptionStatus.Draft)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                $"Yalnızca taslak durumundaki reçeteler imzalanabilir. Mevcut durum: '{prescription.Status}'.");
        }

        if (prescription.Items.Count == 0)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "items", "En az bir ilaç kalemi bulunmayan reçete imzalanamaz.");
        }

        if (command.PrescribingDoctorId != Guid.Empty
            && command.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Başka bir hekim adına reçete imzalayamazsınız.");
        }

        if (prescription.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Yalnızca reçete taslağının yazarı imza atabilir.");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != prescription.Version)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var encounter = await _accessContext.FindEncounterAsync(
            prescription.EncounterId,
            cancellationToken);
        if (encounter is null
            || encounter.PatientId != prescription.PatientId
            || encounter.DepartmentId != prescription.DepartmentId)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçetenin bağlı olduğu karşılaşma geçersizdir.");
        }

        if (!encounter.AllowsClinicalEntry
            || !await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionSign,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Bu karşılaşmadaki reçeteyi imzalama yetkiniz bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var validDays = command.ValidDays ?? 14;
        if (validDays <= 0 || validDays > 90)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "validDays", "Reçete geçerlilik süresi 1 ile 90 gün arasında olmalıdır.");
        }

        // Güvenlik Uyarı Kontrolü (Safety Check)
        var safetyCandidates = prescription.Items
            .Select(i => new PrescriptionItemSafetyCandidate(
                i.MedicationCatalogItemId,
                i.Dose,
                i.DoseUnit,
                i.Frequency,
                i.DurationDays))
            .ToList();

        var safetyResult = await _safetyChecker.CheckSafetyAsync(
            prescription.PatientId,
            safetyCandidates,
            prescription.Id,
            cancellationToken);

        var requiresSafetyOverride = safetyResult.HasCriticalWarnings || safetyResult.HasModerateWarnings;
        if (requiresSafetyOverride)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason) || command.OverrideReason.Trim().Length < 5)
            {
                var warningDict = safetyResult.Warnings
                    .ToDictionary(
                        w => w.WarningCode,
                        w => new[] { $"{w.Title}: {w.Message}" });

                return PrescriptionOperationResult.SafetyWarningOverrideRequired<PrescriptionDetailDto>(
                    "Reçetede kritik veya orta düzey ilaç güvenliği uyarısı bulunmaktadır. İmzalamak için geçerli bir geçersiz kılma gerekçesi (Override Reason) girilmelidir.",
                    warningDict);
            }

            var requiredWarningCodes = safetyResult.Warnings
                .Where(warning => warning.RequiresOverrideReason)
                .Select(warning => warning.WarningCode)
                .ToHashSet(StringComparer.Ordinal);
            var acknowledgedWarningCodes = command.AcknowledgedWarningCodes?
                .ToHashSet(StringComparer.Ordinal)
                ?? [];

            if (!requiredWarningCodes.IsSubsetOf(acknowledgedWarningCodes))
            {
                return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                    "acknowledgedWarningCodes",
                    "Geçersiz kılma gerektiren tüm güncel uyarılar açıkça onaylanmalıdır.");
            }
        }

        var validUntilUtc = nowUtc.AddDays(validDays);

        try
        {
            prescription.Sign(doctorId, validUntilUtc, nowUtc);
        }
        catch (Exception ex)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        if (requiresSafetyOverride)
        {
            await PublishAuditAsync(
                actor,
                "Pharmacy.PrescriptionSafetyWarningOverride",
                prescription.Id.ToString(),
                AuditOutcome.Success,
                "Kural tabanlı güvenlik uyarıları gerekçeli ve açık onayla geçersiz kılındı.",
                cancellationToken);
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionSign",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete dijital olarak imzalandı.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        CancelPrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionCancel)
            || !TryExtractActorPersonId(actor, out var doctorId))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçete iptal etme yetkiniz bulunmamaktadır.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "reason", "Reçete iptal gerekçesi zorunludur.");
        }

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        if (command.PrescribingDoctorId != Guid.Empty
            && command.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Başka bir hekim adına reçete iptal edemezsiniz.");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != prescription.Version)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var canCancel = prescription.PrescribingDoctorId == doctorId
            || (actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)
                && await _accessContext.IsAssignedToDepartmentAsync(
                    actor,
                    prescription.DepartmentId,
                    cancellationToken));
        if (!canCancel)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Yalnızca reçete yazarı veya bölüm kapsamındaki başhekim iptal yapabilir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            prescription.Cancel(doctorId, command.Reason, nowUtc);
        }
        catch (Exception ex)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionCancel",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete gerekçeli olarak iptal edildi; gerekçe klinik kayıtta saklandı.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkPrescriptionEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionCancel)
            || !TryExtractActorPersonId(actor, out var doctorId))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçeteyi hatalı giriş olarak işaretleme yetkiniz bulunmamaktadır.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        if (command.PrescribingDoctorId != Guid.Empty
            && command.PrescribingDoctorId != doctorId)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Başka bir hekim adına hatalı giriş kaydı oluşturamazsınız.");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != prescription.Version)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var canCorrect = prescription.PrescribingDoctorId == doctorId
            || (actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)
                && await _accessContext.IsAssignedToDepartmentAsync(
                    actor,
                    prescription.DepartmentId,
                    cancellationToken));
        if (!canCorrect)
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Yalnızca reçete yazarı veya bölüm kapsamındaki başhekim düzeltme yapabilir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            prescription.MarkEnteredInError(doctorId, command.Reason, nowUtc);
        }
        catch (Exception ex)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionEnteredInError",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete gerekçeli olarak hatalı giriş durumuna alındı; gerekçe klinik kayıtta saklandı.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> DispenseAsync(
        ClaimsPrincipal actor,
        DispensePrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionDispense)
            || !TryExtractActorPersonId(actor, out _))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Reçete teslim kaydı girme yetkiniz bulunmamaktadır.");
        }

        if (command.PrescriptionId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "prescriptionId", "Reçete kimliği zorunludur.");
        }

        if (command.Items == null || command.Items.Count == 0)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "items", "En az bir teslim kalemi belirtilmelidir.");
        }

        if (command.IdempotencyKey == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "idempotencyKey", "Teslim işlemi için idempotency anahtarı zorunludur.");
        }

        if (command.Items
            .GroupBy(item => new { item.ItemId, item.StockItemId })
            .Any(group => group.Count() > 1))
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "items", "Aynı reçete kalemi ve stok lotu istekte birden fazla kez yer alamaz.");
        }

        var requestFingerprint = ComputeDispenseFingerprint(command);

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.PrescriptionId, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        if (!await _accessContext.IsAssignedToFacilityForDepartmentAsync(
                actor,
                prescription.DepartmentId,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                "Bu reçetenin bağlı olduğu tesiste ilaç teslim etme yetkiniz bulunmamaktadır.");
        }

        var completedOperation = await _dbContext.PrescriptionDispenseOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operation => operation.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);
        if (completedOperation is not null)
        {
            return completedOperation.PrescriptionId == command.PrescriptionId
                && string.Equals(
                    completedOperation.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal)
                ? PrescriptionOperationResult.Success(MapToDetailDto(prescription, redactClinicalContext: true))
                : PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    "Idempotency anahtarı farklı bir teslim isteği için daha önce kullanılmıştır.");
        }

        if (prescription.Status != PrescriptionStatus.Signed && prescription.Status != PrescriptionStatus.PartiallyDispensed)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                $"Yalnızca imzalı veya kısmen karşılanan reçeteler teslim edilebilir. Mevcut durum: {prescription.Status}");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != prescription.Version)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                "Reçete başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (prescription.ValidUntilUtc.HasValue && prescription.ValidUntilUtc.Value < nowUtc)
        {
            return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                $"Reçetenin geçerlilik süresi dolmuştur. Geçerlilik: {prescription.ValidUntilUtc.Value:dd.MM.yyyy}");
        }

        var actorUserId = ExtractActorUserId(actor);
        var stockIds = command.Items.Select(item => item.StockItemId).Distinct().ToList();
        var stocks = await _dbContext.MedicationStockItems
            .Where(stock => stockIds.Contains(stock.Id))
            .ToDictionaryAsync(stock => stock.Id, cancellationToken);

        if (stocks.Count != stockIds.Count)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>(
                "Seçilen stok kalemlerinden biri bulunamadı.");
        }

        foreach (var itemCmd in command.Items)
        {
            var pItem = prescription.Items.FirstOrDefault(i => i.Id == itemCmd.ItemId);
            if (pItem is null)
            {
                return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                    "itemId", $"Reçete kalemi bulunamadı: {itemCmd.ItemId}");
            }

            if (itemCmd.Quantity <= 0)
            {
                return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                    "quantity", "Teslim miktarı 0'dan büyük olmalıdır.");
            }

            var stockItem = stocks[itemCmd.StockItemId];

            if (stockItem.MedicationCatalogItemId != pItem.MedicationCatalogItemId)
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    $"Seçilen stok kalemi ({stockItem.LotNumber}) reçetedeki ilaç ile eşleşmiyor.");
            }

            if (stockItem.IsExpired(nowUtc))
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    $"Seçilen parti/lotun ({stockItem.LotNumber}) son kullanma tarihi dolmuştur: {stockItem.ExpirationDateUtc:dd.MM.yyyy}");
            }

            if (itemCmd.ExpectedStockVersion <= 0
                || itemCmd.ExpectedStockVersion != stockItem.Version)
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    "Stok lotu başka bir işlem tarafından değiştirildi. FEFO listesini yenileyip yeniden deneyiniz.");
            }

            if (!await _accessContext.IsAssignedToDepartmentAsync(
                    actor,
                    stockItem.DepartmentId,
                    cancellationToken))
            {
                return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>(
                    "Seçilen stok lotunun bölümünde işlem yapma yetkiniz bulunmamaktadır.");
            }
        }

        foreach (var itemGroup in command.Items.GroupBy(item => item.ItemId))
        {
            var prescriptionItem = prescription.Items.Single(item => item.Id == itemGroup.Key);
            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var remainingQuantity = prescriptionItem.Quantity - prescriptionItem.DispensedQuantity;
            if (requestedQuantity > remainingQuantity)
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    "Teslim miktarı kalan reçete miktarını aşamaz.");
            }

            var selectedDepartmentIds = itemGroup
                .Select(item => stocks[item.StockItemId].DepartmentId)
                .Distinct()
                .ToList();
            if (selectedDepartmentIds.Count != 1)
            {
                return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                    "items", "Bir reçete kalemi tek bir stok bölümünden karşılanmalıdır.");
            }

            var fefoLots = await _dbContext.MedicationStockItems
                .Where(stock => stock.MedicationCatalogItemId == prescriptionItem.MedicationCatalogItemId
                    && stock.DepartmentId == selectedDepartmentIds[0]
                    && stock.QuantityOnHand - stock.QuantityReserved > 0
                    && stock.ExpirationDateUtc > nowUtc)
                .OrderBy(stock => stock.ExpirationDateUtc)
                .ThenBy(stock => stock.LotNumber)
                .ToListAsync(cancellationToken);

            var expectedAllocation = new Dictionary<Guid, int>();
            var quantityToAllocate = requestedQuantity;
            foreach (var lot in fefoLots)
            {
                if (quantityToAllocate == 0)
                {
                    break;
                }

                var allocated = Math.Min(quantityToAllocate, lot.QuantityAvailable);
                if (allocated > 0)
                {
                    expectedAllocation[lot.Id] = allocated;
                    quantityToAllocate -= allocated;
                }
            }

            var requestedAllocation = itemGroup.ToDictionary(
                item => item.StockItemId,
                item => item.Quantity);
            if (quantityToAllocate > 0
                || expectedAllocation.Count != requestedAllocation.Count
                || expectedAllocation.Any(allocation =>
                    !requestedAllocation.TryGetValue(allocation.Key, out var quantity)
                    || quantity != allocation.Value))
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    "Seçilen lotlar FEFO sırasına veya kullanılabilir stok miktarına uymuyor.");
            }
        }

        foreach (var itemCmd in command.Items
            .OrderBy(item => stocks[item.StockItemId].ExpirationDateUtc)
            .ThenBy(item => stocks[item.StockItemId].LotNumber))
        {
            var pItem = prescription.Items.Single(item => item.Id == itemCmd.ItemId);
            var stockItem = stocks[itemCmd.StockItemId];

            var prevQty = stockItem.QuantityOnHand;

            try
            {
                stockItem.DeductStock(itemCmd.Quantity, nowUtc);
                prescription.RecordDispense(pItem.Id, itemCmd.Quantity, nowUtc);
            }
            catch (Exception ex)
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(ex.Message);
            }

            var tx = MedicationStockTransaction.Create(
                Guid.NewGuid(),
                stockItem.Id,
                StockTransactionType.Dispense,
                itemCmd.Quantity,
                prevQty,
                stockItem.QuantityOnHand,
                referenceId: prescription.PrescriptionNumber,
                notes: "DEMO eczane teslim kaydı",
                performedByUserId: actorUserId,
                performedAtUtc: nowUtc);

            _dbContext.MedicationStockTransactions.Add(tx);
        }

        _dbContext.PrescriptionDispenseOperations.Add(
            PrescriptionDispenseOperation.Create(
                Guid.NewGuid(),
                command.IdempotencyKey,
                command.PrescriptionId,
                requestFingerprint,
                nowUtc));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await ResolveConcurrentDispenseAsync(
                command,
                requestFingerprint,
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            return await ResolveConcurrentDispenseAsync(
                command,
                requestFingerprint,
                cancellationToken);
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionDispense",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            "Reçete teslimi ve stok hareketi atomik olarak kaydedildi.",
            cancellationToken);

        return PrescriptionOperationResult.Success(MapToDetailDto(prescription, redactClinicalContext: true));
    }

    public async Task<PrescriptionOperationResult<PrescriptionDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (id == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<PrescriptionDetailDto>(
                "id", "Reçete kimliği zorunludur.");
        }

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionView))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>();
        }

        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (prescription is null)
        {
            return PrescriptionOperationResult.NotFound<PrescriptionDetailDto>("Reçete bulunamadı.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await CanViewPrescriptionAsync(actor, prescription, nowUtc, cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<PrescriptionDetailDto>();
        }

        if (prescription.CheckAndMarkExpired(nowUtc))
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
                    "Reçete durumu eşzamanlı olarak değişti. Güncel veriyi yeniden yükleyiniz.");
            }
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionView",
            prescription.Id.ToString(),
            AuditOutcome.Success,
            null,
            cancellationToken);

        return PrescriptionOperationResult.Success(
            MapToDetailDto(prescription, redactClinicalContext: IsPharmacistActor(actor)));
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByEncounterAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<IReadOnlyList<PrescriptionSummaryDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionView))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<PrescriptionSummaryDto>>();
        }

        var encounter = await _accessContext.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return PrescriptionOperationResult.NotFound<IReadOnlyList<PrescriptionSummaryDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<PrescriptionSummaryDto>>();
        }

        var prescriptions = await _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var visiblePrescriptions = IsPatientActor(actor)
            ? prescriptions.Where(IsVisibleToPatient)
            : prescriptions;

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionEncounterQuery",
            encounterId.ToString(),
            AuditOutcome.Success,
            null,
            cancellationToken);

        return PrescriptionOperationResult.Success<IReadOnlyList<PrescriptionSummaryDto>>(
            visiblePrescriptions.Select(MapToSummaryDto).ToList());
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<IReadOnlyList<PrescriptionSummaryDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionView)
            || !await _accessContext.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.Pharmacy.PrescriptionView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<PrescriptionSummaryDto>>(
                "Bu hastanın reçetelerini görüntüleme yetkiniz bulunmamaktadır.");
        }

        var query = _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId);

        // If patient is viewing, hide Draft and EnteredInError prescriptions
        if (IsPatientActor(actor))
        {
            query = query.Where(p => p.Status != PrescriptionStatus.Draft
                && p.Status != PrescriptionStatus.EnteredInError);
        }

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Pharmacy.PrescriptionPatientQuery",
            patientId.ToString(),
            AuditOutcome.Success,
            null,
            cancellationToken);

        return PrescriptionOperationResult.Success<IReadOnlyList<PrescriptionSummaryDto>>(
            prescriptions.Select(MapToSummaryDto).ToList());
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        string? status = null,
        string? prescriptionNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionView)
            || !HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionDispense))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<PrescriptionSummaryDto>>(
                "Eczane iş listesini görüntüleme yetkiniz bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var query = _dbContext.Prescriptions
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.Status == PrescriptionStatus.Signed
                || p.Status == PrescriptionStatus.PartiallyDispensed
                || p.Status == PrescriptionStatus.Dispensed)
            .Where(p => !p.ValidUntilUtc.HasValue || p.ValidUntilUtc > nowUtc);

        if (!string.IsNullOrWhiteSpace(status)
            && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<PrescriptionStatus>(status, ignoreCase: true, out var parsedStatus)
                && parsedStatus is PrescriptionStatus.Signed
                    or PrescriptionStatus.PartiallyDispensed
                    or PrescriptionStatus.Dispensed)
            {
                query = query.Where(p => p.Status == parsedStatus);
            }
        }
        else if (string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == PrescriptionStatus.Signed
                || p.Status == PrescriptionStatus.PartiallyDispensed);
        }

        if (!string.IsNullOrWhiteSpace(prescriptionNumber))
        {
            var num = prescriptionNumber.Trim();
            query = query.Where(p => p.PrescriptionNumber.Contains(num));
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(p => p.PatientId == patientId.Value);
        }

        var limit = Math.Clamp(maxResults, 1, 100);
        var candidates = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(limit * 2)
            .ToListAsync(cancellationToken);

        var prescriptions = new List<Prescription>(limit);
        foreach (var candidate in candidates)
        {
            if (await _accessContext.IsAssignedToFacilityForDepartmentAsync(
                    actor,
                    candidate.DepartmentId,
                    cancellationToken))
            {
                prescriptions.Add(candidate);
                if (prescriptions.Count == limit)
                {
                    break;
                }
            }
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.WorklistView",
            "Prescriptions",
            AuditOutcome.Success,
            "Eczane iş listesi kapsam denetimiyle sorgulandı.",
            cancellationToken);

        return PrescriptionOperationResult.Success<IReadOnlyList<PrescriptionSummaryDto>>(
            prescriptions.Select(MapToSummaryDto).ToList());
    }

    private static PrescriptionDetailDto MapToDetailDto(
        Prescription p,
        bool redactClinicalContext = false) =>
        new(
            p.Id,
            p.PrescriptionNumber,
            p.PatientId,
            redactClinicalContext ? Guid.Empty : p.EncounterId,
            redactClinicalContext ? Guid.Empty : p.PrescribingDoctorId,
            redactClinicalContext ? Guid.Empty : p.DepartmentId,
            p.Status,
            p.ValidUntilUtc,
            p.SignedAtUtc,
            redactClinicalContext ? null : p.SignedByDoctorId,
            redactClinicalContext ? null : p.CancelledAtUtc,
            redactClinicalContext ? null : p.CancelledByDoctorId,
            redactClinicalContext ? null : p.CancellationReason,
            redactClinicalContext ? null : p.EnteredInErrorAtUtc,
            redactClinicalContext ? null : p.EnteredInErrorByDoctorId,
            redactClinicalContext ? null : p.EnteredInErrorReason,
            redactClinicalContext ? null : p.DiagnosisSummary,
            p.GeneralInstructions,
            p.Version,
            p.CreatedAtUtc,
            p.UpdatedAtUtc,
            p.Items.Select(MapItemToDto).ToList());

    private static PrescriptionItemDto MapItemToDto(PrescriptionItem i) =>
        new(
            i.Id,
            i.PrescriptionId,
            i.MedicationCatalogItemId,
            i.MedicationCode,
            i.BrandName,
            i.GenericName,
            i.Form,
            i.Route,
            i.Dose,
            i.DoseUnit,
            i.Frequency,
            i.DurationDays,
            i.Quantity,
            i.QuantityUnit,
            i.DispensedQuantity,
            i.IsFullyDispensed,
            i.Instructions,
            i.CreatedAtUtc);

    private static PrescriptionSummaryDto MapToSummaryDto(Prescription p) =>
        new(
            p.Id,
            p.PrescriptionNumber,
            p.PatientId,
            p.EncounterId,
            p.PrescribingDoctorId,
            p.DepartmentId,
            p.Status,
            p.ValidUntilUtc,
            p.SignedAtUtc,
            p.Items.Count,
            p.CreatedAtUtc);

    private static string GeneratePrescriptionNumber(DateTime nowUtc)
    {
        var randomHex = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"DEMO-RX-{nowUtc:yyyyMMdd}-{randomHex}";
    }

    private async Task<bool> CanViewPrescriptionAsync(
        ClaimsPrincipal actor,
        Prescription prescription,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(actor))
        {
            return TryExtractActorPersonId(actor, out var personId)
                && personId == prescription.PatientId
                && IsVisibleToPatient(prescription);
        }

        if (IsPharmacistActor(actor))
        {
            var isPharmacyVisible = prescription.Status == PrescriptionStatus.Dispensed
                || ((prescription.Status == PrescriptionStatus.Signed
                        || prescription.Status == PrescriptionStatus.PartiallyDispensed)
                    && (!prescription.ValidUntilUtc.HasValue
                        || prescription.ValidUntilUtc.Value > nowUtc));

            return isPharmacyVisible
                && HasPermission(actor, HospitalPermissions.Pharmacy.PrescriptionDispense)
                && await _accessContext.IsAssignedToFacilityForDepartmentAsync(
                    actor,
                    prescription.DepartmentId,
                    cancellationToken);
        }

        var encounter = await _accessContext.FindEncounterAsync(
            prescription.EncounterId,
            cancellationToken);

        return encounter is not null
            && encounter.PatientId == prescription.PatientId
            && encounter.DepartmentId == prescription.DepartmentId
            && await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionView,
                allowPatientOwnRecord: false,
                cancellationToken);
    }

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.Identity?.IsAuthenticated == true
        && actor.HasClaim(HospitalClaimTypes.Permission, permission);

    private static bool IsPatientActor(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private static bool IsPharmacistActor(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Pharmacist);

    private static bool IsVisibleToPatient(Prescription prescription) =>
        prescription.Status != PrescriptionStatus.Draft
        && prescription.Status != PrescriptionStatus.EnteredInError;

    private static bool TryExtractActorPersonId(ClaimsPrincipal actor, out Guid personId) =>
        Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out personId)
        && personId != Guid.Empty;

    private static Guid? ExtractActorUserId(ClaimsPrincipal actor)
    {
        var uid = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(uid, out var userGuid) ? userGuid : null;
    }

    private async Task<PrescriptionOperationResult<PrescriptionDetailDto>> ResolveConcurrentDispenseAsync(
        DispensePrescriptionCommand command,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        _dbContext.ChangeTracker.Clear();

        var completedOperation = await _dbContext.PrescriptionDispenseOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operation => operation.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);

        if (completedOperation is not null
            && completedOperation.PrescriptionId == command.PrescriptionId
            && string.Equals(
                completedOperation.RequestFingerprint,
                requestFingerprint,
                StringComparison.Ordinal))
        {
            var currentPrescription = await _dbContext.Prescriptions
                .AsNoTracking()
                .Include(prescription => prescription.Items)
                .SingleAsync(
                    prescription => prescription.Id == command.PrescriptionId,
                    cancellationToken);

            return PrescriptionOperationResult.Success(
                MapToDetailDto(currentPrescription, redactClinicalContext: true));
        }

        return PrescriptionOperationResult.Conflict<PrescriptionDetailDto>(
            "Teslim sırasında eşzamanlı bir değişiklik oluştu. Güncel reçete ve stok verisini yükleyip yeniden deneyiniz.");
    }

    private static string ComputeDispenseFingerprint(DispensePrescriptionCommand command)
    {
        var canonical = new StringBuilder()
            .Append(command.PrescriptionId.ToString("N"))
            .Append('|')
            .Append(command.ExpectedVersion)
            .Append('|');

        foreach (var item in command.Items
            .OrderBy(item => item.ItemId)
            .ThenBy(item => item.StockItemId))
        {
            canonical
                .Append(item.ItemId.ToString("N"))
                .Append(':')
                .Append(item.StockItemId.ToString("N"))
                .Append(':')
                .Append(item.ExpectedStockVersion)
                .Append(':')
                .Append(item.Quantity)
                .Append('|');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value is { } uid && Guid.TryParse(uid, out var userGuid)
            ? userGuid
            : (Guid?)null;

        var actorPersonId = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value is { } pid && Guid.TryParse(pid, out var personGuid)
            ? personGuid
            : (Guid?)null;

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
            TargetResourceType: "Prescription",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
