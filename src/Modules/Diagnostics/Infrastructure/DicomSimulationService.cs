using System.Security;
using System.Security.Claims;
using System.Text;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class DicomSimulationService : IDicomSimulationService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly IDicomPreviewTokenProtector _tokenProtector;

    public DicomSimulationService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider,
        IDiagnosticsAccessContext accessContext,
        IDicomPreviewTokenProtector tokenProtector)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _tokenProtector = tokenProtector ?? throw new ArgumentNullException(nameof(tokenProtector));
    }

    public async Task<DiagnosticOrderOperationResult<DicomStudyMetadataDto>> GetStudyMetadataAsync(
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
            return DiagnosticOrderOperationResult.NotFound<DicomStudyMetadataDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<DicomStudyMetadataDto>("Bu radyoloji görüntülerine erişim yetkiniz veya kaynak kapsamınız yok.");
        }

        var authorizedPersonId = GetActorPersonId(actor);
        if (!authorizedPersonId.HasValue)
        {
            return DiagnosticOrderOperationResult.Forbidden<DicomStudyMetadataDto>("Kimliği doğrulanmış kişi kapsamı bulunamadı.");
        }

        if (study.Status == RadiologyStudyStatus.Ordered || study.Status == RadiologyStudyStatus.Scheduled)
        {
            return DiagnosticOrderOperationResult.Validation<DicomStudyMetadataDto>("Status", "Görüntü çekimi henüz tamamlanmadığı için DICOM serileri üretilmemiştir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = nowUtc.AddMinutes(5);

        var studyInstanceUid = $"1.2.826.0.1.3680043.9.7123.{study.Id:N}";
        var modalityStr = study.Modality.ToString();

        var seriesList = new List<DicomSeriesDto>();

        // Series 1: Standard Acquisition Series
        var series1Uid = $"{studyInstanceUid}.1";
        var instances1 = new List<DicomInstanceDto>();
        for (var i = 1; i <= 3; i++)
        {
            var instanceUid = $"{series1Uid}.{i}";
            var token = _tokenProtector.Protect(new DicomPreviewGrant(study.Id, instanceUid, authorizedPersonId.Value, expiresAtUtc));
            instances1.Add(new DicomInstanceDto(
                instanceUid,
                i,
                modalityStr,
                "image/svg+xml",
                512000,
                token));
        }

        seriesList.Add(new DicomSeriesDto(
            series1Uid,
            1,
            modalityStr,
            $"{study.ProcedureName} - Standart Kesit / Seri",
            instances1.Count,
            instances1));

        // Series 2: Contrast / Reconstruction Series if CT or MR
        if (study.Modality == RadiologyModality.CT || study.Modality == RadiologyModality.MR)
        {
            var series2Uid = $"{studyInstanceUid}.2";
            var instances2 = new List<DicomInstanceDto>();
            for (var i = 1; i <= 2; i++)
            {
                var instanceUid = $"{series2Uid}.{i}";
                var token = _tokenProtector.Protect(new DicomPreviewGrant(study.Id, instanceUid, authorizedPersonId.Value, expiresAtUtc));
                instances2.Add(new DicomInstanceDto(
                    instanceUid,
                    i,
                    modalityStr,
                    "image/svg+xml",
                    512000,
                    token));
            }

            seriesList.Add(new DicomSeriesDto(
                series2Uid,
                2,
                modalityStr,
                $"{study.ProcedureName} - Koronal / Rekonstrüksiyon Serisi",
                instances2.Count,
                instances2));
        }

        var metadata = new DicomStudyMetadataDto(
            study.Id,
            study.AccessionNumber,
            studyInstanceUid,
            study.PerformedAtUtc ?? study.CreatedAtUtc,
            modalityStr,
            study.ProcedureName,
            study.PatientId.ToString(),
            true,
            seriesList);

        await PublishAuditAsync(
            actor,
            "Diagnostics.DicomMetadataAccess",
            "DicomStudy",
            study.Id.ToString(),
            $"DICOM çalışma metadata ve serileri görüntülendi: {study.AccessionNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(metadata);
    }

    public async Task<DiagnosticOrderOperationResult<DicomPreviewTokenDto>> GeneratePreviewTokenAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string sopInstanceUid,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(sopInstanceUid))
        {
            return DiagnosticOrderOperationResult.Validation<DicomPreviewTokenDto>("SopInstanceUid", "SOP Instance UID zorunludur.");
        }

        var study = await _dbContext.RadiologyStudies
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studyId, cancellationToken);

        if (study is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DicomPreviewTokenDto>("Radyoloji çekim kaydı bulunamadı.");
        }

        if (!await CanAccessStudyAsync(actor, study, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<DicomPreviewTokenDto>("Bu radyoloji görüntülerine erişim yetkiniz veya kaynak kapsamınız yok.");
        }

        var authorizedPersonId = GetActorPersonId(actor);
        if (!authorizedPersonId.HasValue)
        {
            return DiagnosticOrderOperationResult.Forbidden<DicomPreviewTokenDto>("Kimliği doğrulanmış kişi kapsamı bulunamadı.");
        }

        if (study.Status is RadiologyStudyStatus.Ordered or RadiologyStudyStatus.Scheduled or RadiologyStudyStatus.Cancelled)
        {
            return DiagnosticOrderOperationResult.Validation<DicomPreviewTokenDto>("Status", "Bu radyoloji kaydı için görüntü önizlemesi kullanılamaz.");
        }

        if (!GetValidInstanceUids(study).Contains(sopInstanceUid, StringComparer.Ordinal))
        {
            return DiagnosticOrderOperationResult.Validation<DicomPreviewTokenDto>("SopInstanceUid", "SOP Instance UID bu radyoloji çalışmasına ait değildir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = nowUtc.AddMinutes(5);

        var token = _tokenProtector.Protect(new DicomPreviewGrant(
            study.Id,
            sopInstanceUid,
            authorizedPersonId.Value,
            expiresAtUtc));

        return DiagnosticOrderOperationResult.Success(new DicomPreviewTokenDto(token, expiresAtUtc));
    }

    public async Task<DiagnosticOrderOperationResult<(byte[] Content, string ContentType)>> RenderPreviewImageAsync(
        ClaimsPrincipal actor,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (string.IsNullOrWhiteSpace(token))
        {
            return DiagnosticOrderOperationResult.Validation<(byte[], string)>("Token", "Geçersiz veya boş önizleme belirteci.");
        }

        if (!_tokenProtector.TryUnprotect(token, out var grant) || grant is null)
        {
            return DiagnosticOrderOperationResult.Validation<(byte[], string)>("Token", "Önizleme belirteci geçersiz veya tahrif edilmiş.");
        }

        var actorPersonId = GetActorPersonId(actor);
        if (!actorPersonId.HasValue || actorPersonId.Value != grant.AuthorizedPersonId)
        {
            return DiagnosticOrderOperationResult.Forbidden<(byte[], string)>("Önizleme belirteci bu kullanıcı için geçerli değildir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (grant.ExpiresAtUtc <= nowUtc)
        {
            return DiagnosticOrderOperationResult.Forbidden<(byte[], string)>("Önizleme bağlantısının süresi dolmuştur.");
        }

        var study = await _dbContext.RadiologyStudies
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == grant.StudyId, cancellationToken);
        if (study is null
            || study.Status is RadiologyStudyStatus.Ordered or RadiologyStudyStatus.Scheduled or RadiologyStudyStatus.Cancelled
            || !GetValidInstanceUids(study).Contains(grant.SopInstanceUid, StringComparer.Ordinal))
        {
            return DiagnosticOrderOperationResult.Forbidden<(byte[], string)>("Önizleme bağlantısı artık geçerli değildir.");
        }

        if (!await CanAccessStudyAsync(actor, study, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<(byte[], string)>("Radyoloji görüntüsü için güncel kaynak yetkisi bulunmamaktadır.");
        }

        var instanceNumber = int.TryParse(grant.SopInstanceUid.Split('.').LastOrDefault(), out var parsed)
            ? parsed
            : 1;
        var svg = GenerateSimulatedDicomSvg(study, grant, instanceNumber);
        var bytes = Encoding.UTF8.GetBytes(svg);

        return DiagnosticOrderOperationResult.Success<(byte[] Content, string ContentType)>((bytes, "image/svg+xml"));
    }

    private static string GenerateSimulatedDicomSvg(
        RadiologyStudy study,
        DicomPreviewGrant grant,
        int instanceNumber)
    {
        var modality = SecurityElement.Escape(study.Modality.ToString());
        var procedureName = SecurityElement.Escape(study.ProcedureName);
        var accessionNumber = SecurityElement.Escape(study.AccessionNumber);
        var instanceUid = SecurityElement.Escape(grant.SopInstanceUid);
        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 800 600"" width=""100%"" height=""100%"" style=""background-color:#0d1117; font-family:monospace; color:#58a6ff;"">
  <defs>
    <linearGradient id=""grad"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" style=""stop-color:#161b22;stop-opacity:1"" />
      <stop offset=""100%"" style=""stop-color:#0d1117;stop-opacity:1"" />
    </linearGradient>
    <radialGradient id=""scanRadial"" cx=""50%"" cy=""50%"" r=""50%"" fx=""50%"" fy=""50%"">
      <stop offset=""0%"" style=""stop-color:#30363d;stop-opacity:0.8"" />
      <stop offset=""70%"" style=""stop-color:#161b22;stop-opacity:0.5"" />
      <stop offset=""100%"" style=""stop-color:#0d1117;stop-opacity:0"" />
    </radialGradient>
  </defs>

  <!-- Background Grid -->
  <rect width=""800"" height=""600"" fill=""url(#grad)"" />
  <circle cx=""400"" cy=""300"" r=""220"" fill=""url(#scanRadial)"" />
  
  <!-- Simulated Anatomic Matrix / Crosshairs -->
  <circle cx=""400"" cy=""300"" r=""180"" fill=""none"" stroke=""#30363d"" stroke-width=""2"" stroke-dasharray=""6,6"" />
  <circle cx=""400"" cy=""300"" r=""90"" fill=""none"" stroke=""#238636"" stroke-width=""1.5"" opacity=""0.5"" />
  <line x1=""400"" y1=""60"" x2=""400"" y2=""540"" stroke=""#21262d"" stroke-width=""1"" />
  <line x1=""100"" y1=""300"" x2=""700"" y2=""300"" stroke=""#21262d"" stroke-width=""1"" />

  <!-- DICOM Overlay Text - Top Left -->
  <text x=""20"" y=""35"" fill=""#58a6ff"" font-size=""14"" font-weight=""bold"">HOSPITAL MANAGEMENT DICOM VIEWER</text>
  <text x=""20"" y=""55"" fill=""#8b949e"" font-size=""12"">MODALITE: {modality} | KESIT: #{instanceNumber}</text>
  <text x=""20"" y=""75"" fill=""#8b949e"" font-size=""12"">ISLEM: {procedureName}</text>
  <text x=""20"" y=""95"" fill=""#8b949e"" font-size=""12"">ACCESSION: {accessionNumber}</text>

  <!-- DICOM Overlay Text - Top Right -->
  <text x=""780"" y=""35"" fill=""#f0883e"" font-size=""13"" text-anchor=""end"" font-weight=""bold"">SENTETIK PACS SIMULASYONU</text>
  <text x=""780"" y=""55"" fill=""#8b949e"" font-size=""12"" text-anchor=""end"">GERCEK KLINIK KARAR ICIN KULLANILMAZ</text>
  <text x=""780"" y=""75"" fill=""#8b949e"" font-size=""11"" text-anchor=""end"">SOP UID: {instanceUid?.Substring(Math.Max(0, instanceUid.Length - 16))}...</text>

  <!-- Center Watermark -->
  <g transform=""translate(400, 300) rotate(-25)"">
    <text x=""0"" y=""-10"" fill=""#da3633"" font-size=""32"" font-weight=""bold"" text-anchor=""middle"" opacity=""0.28"">MOCK DICOM PREVIEW</text>
    <text x=""0"" y=""25"" fill=""#f85149"" font-size=""18"" text-anchor=""middle"" opacity=""0.28"">EGITIM VE TEST SIMULASYONU</text>
  </g>

  <!-- Bottom Left - Technical Metadata -->
  <text x=""20"" y=""545"" fill=""#8b949e"" font-size=""11"">KVp: 120 | mA: 250 | FOV: 350mm</text>
  <text x=""20"" y=""565"" fill=""#8b949e"" font-size=""11"">W: 1500 L: -600 (LUNG/STANDARD)</text>
  <text x=""20"" y=""585"" fill=""#3fb950"" font-size=""11"">DURUM: IMZALI GORUNTULEME SERISI DOGRULANDI</text>

  <!-- Bottom Right - Expiration -->
  <text x=""780"" y=""585"" fill=""#8b949e"" font-size=""11"" text-anchor=""end"">Gecerlilik: {grant.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC</text>
</svg>";
    }

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private static Guid? GetActorPersonId(ClaimsPrincipal actor) =>
        Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out var personId)
            ? personId
            : null;

    private async Task<bool> CanAccessStudyAsync(
        ClaimsPrincipal actor,
        RadiologyStudy study,
        CancellationToken cancellationToken)
    {
        if (IsPatient(actor)
            && study.Status is not (RadiologyStudyStatus.ReportFinalized or RadiologyStudyStatus.AddendumAdded))
        {
            return false;
        }

        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == study.DiagnosticOrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        var permission = IsPatient(actor)
            ? HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn
            : actor.HasClaim(
                HospitalClaimTypes.Permission,
                HospitalPermissions.Diagnostics.RadiologyWorklistView)
                ? HospitalPermissions.Diagnostics.RadiologyWorklistView
                : HospitalPermissions.ClinicalRecords.EncounterView;

        return await _accessContext.CanAccessResourceAsync(
            actor,
            new DiagnosticResourceContext(order.EncounterId, order.PatientId, order.DepartmentId, order.OrderType),
            permission,
            IsPatient(actor),
            cancellationToken);
    }

    private static List<string> GetValidInstanceUids(RadiologyStudy study)
    {
        var studyInstanceUid = $"1.2.826.0.1.3680043.9.7123.{study.Id:N}";
        var uids = new List<string>
        {
            $"{studyInstanceUid}.1.1",
            $"{studyInstanceUid}.1.2",
            $"{studyInstanceUid}.1.3",
        };

        if (study.Modality is RadiologyModality.CT or RadiologyModality.MR)
        {
            uids.Add($"{studyInstanceUid}.2.1");
            uids.Add($"{studyInstanceUid}.2.2");
        }

        return uids;
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
