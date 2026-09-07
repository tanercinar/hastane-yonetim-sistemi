using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Interoperability;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Dicom;
using HospitalManagement.Modules.Interoperability.Domain.ENabiz;
using HospitalManagement.Modules.Interoperability.Domain.Fhir;
using HospitalManagement.Modules.Interoperability.Domain.Medula;
using HospitalManagement.Modules.Interoperability.Domain.Mhrs;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Interoperability;

public static class InteroperabilityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInteroperabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var interopGroup = endpoints.MapGroup("/api/v1/interoperability")
            .RequireAuthorization();

        var mockEngineGroup = interopGroup.MapGroup("/mock-engine")
            .RequirePermission(HospitalPermissions.Interoperability.MockManage);

        mockEngineGroup.MapGet("/configs", GetAllConfigsAsync)
            .WithName("GetAllMockServerConfigs")
            .Produces<List<MockServerConfigResponse>>();

        mockEngineGroup.MapPut("/configs/{systemType}", UpdateConfigAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdateMockServerConfig")
            .Produces<MockServerConfigResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        mockEngineGroup.MapGet("/logs", GetRecentLogsAsync)
            .WithName("GetIntegrationMessageLogs")
            .Produces<List<IntegrationMessageLogResponse>>();

        mockEngineGroup.MapPost("/reset-circuit/{systemType}", ResetCircuitAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ResetIntegrationCircuitBreaker")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        mockEngineGroup.MapPost("/simulate", SimulateOperationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SimulateMockEngineOperation")
            .Produces<SimulateMockEngineResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        var fhirGroup = interopGroup.MapGroup("/fhir/r4");

        fhirGroup.MapGet("/metadata", GetFhirCapabilityStatement)
            .WithName("GetFhirCapabilityStatement")
            .Produces<FhirCapabilityStatement>();

        fhirGroup.MapGet("/Patient/{id:guid}", GetFhirPatientAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirPatient")
            .Produces<FhirPatient>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        fhirGroup.MapGet("/Practitioner/{id:guid}", GetFhirPractitionerAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirPractitioner")
            .Produces<FhirPractitioner>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        fhirGroup.MapGet("/Observation/{id:guid}", GetFhirObservationAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirObservation")
            .Produces<FhirObservation>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        fhirGroup.MapGet("/DiagnosticReport/{id:guid}", GetFhirDiagnosticReportAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirDiagnosticReport")
            .Produces<FhirDiagnosticReport>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        fhirGroup.MapGet("/MedicationRequest/{id:guid}", GetFhirMedicationRequestAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirMedicationRequest")
            .Produces<FhirMedicationRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        fhirGroup.MapGet("/Patient/{id:guid}/$export", GetFhirPatientExportAsync)
            .RequirePermission(HospitalPermissions.Interoperability.FhirExport)
            .WithName("GetFhirPatientExport")
            .Produces<FhirBundle>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        var hl7Group = interopGroup.MapGroup("/hl7")
            .RequirePermission(HospitalPermissions.Interoperability.ClinicalExchange);

        hl7Group.MapPost("/inbound", ProcessHl7InboundAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ProcessHl7InboundMessage")
            .Produces<Hl7InboundResponse>();

        hl7Group.MapPost("/generate/{messageType}", GenerateHl7MessageAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("GenerateHl7Message")
            .Produces<Hl7GenerateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        hl7Group.MapGet("/dead-letter", GetHl7DeadLettersAsync)
            .WithName("GetHl7DeadLetterQueue")
            .Produces<List<Hl7DeadLetterResponse>>();

        hl7Group.MapPost("/dead-letter/{id:guid}/retry", RetryHl7DeadLetterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RetryHl7DeadLetter")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var dicomGroup = interopGroup.MapGroup("/dicom");

        dicomGroup.MapGet("/worklist", QueryDicomWorklistAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyWorklistView)
            .WithName("QueryDicomModalityWorklist")
            .Produces<List<DicomWorklistItemResponse>>();

        dicomGroup.MapPost("/worklist", CreateDicomWorklistOrderAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateDicomWorklistOrder")
            .Produces<DicomWorklistItemResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        dicomGroup.MapGet("/studies/{studyInstanceUid}", QueryDicomStudyAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyWorklistView)
            .WithName("QueryDicomStudyMetadata")
            .Produces<DicomStudyMetadataResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        dicomGroup.MapGet("/patients/{patientId:guid}/studies", QueryPatientDicomStudiesAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyWorklistView)
            .WithName("QueryPatientDicomStudies")
            .Produces<List<DicomStudyMetadataResponse>>();

        var mhrsGroup = interopGroup.MapGroup("/mhrs")
            .RequirePermission(HospitalPermissions.Appointment.ScheduleManage);

        mhrsGroup.MapGet("/slots", QueryMhrsSlotsAsync)
            .WithName("QueryMhrsSlots")
            .Produces<List<MhrsSlotResponse>>();

        mhrsGroup.MapPost("/appointments", BookMhrsAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("BookMhrsAppointment")
            .Produces<MhrsAppointmentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        mhrsGroup.MapPost("/appointments/{mhrsAppointmentId}/cancel", CancelMhrsAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelMhrsAppointment")
            .Produces<MhrsAppointmentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        mhrsGroup.MapPost("/patient-appointments/search", GetPatientMhrsAppointmentsAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("GetPatientMhrsAppointments")
            .Produces<List<MhrsAppointmentResponse>>();

        mhrsGroup.MapPost("/sync", SyncMhrsScheduleAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SyncMhrsSchedule")
            .Produces<MhrsSyncSummaryResponse>();

        var enabizGroup = interopGroup.MapGroup("/enabiz")
            .RequirePermission(HospitalPermissions.Interoperability.ClinicalExchange);

        enabizGroup.MapGet("/queue", QueryENabizQueueAsync)
            .WithName("QueryENabizQueue")
            .Produces<List<ENabizTransmissionResponse>>();

        enabizGroup.MapPost("/queue", EnqueueENabizPackageAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("EnqueueENabizPackage")
            .Produces<ENabizTransmissionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        enabizGroup.MapPost("/transmissions/{id:guid}/send", SendENabizTransmissionAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SendENabizTransmission")
            .Produces<ENabizTransmissionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        enabizGroup.MapPost("/transmissions/{id:guid}/retry", RetryENabizTransmissionAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RetryENabizTransmission")
            .Produces<ENabizTransmissionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        enabizGroup.MapGet("/transmissions/{id:guid}", GetENabizTransmissionByIdAsync)
            .WithName("GetENabizTransmissionById")
            .Produces<ENabizTransmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var medulaGroup = interopGroup.MapGroup("/medula")
            .RequirePermission(HospitalPermissions.Interoperability.MockManage);

        medulaGroup.MapGet("/boundaries", GetMedulaBoundaryInfoAsync)
            .WithName("GetMedulaBoundaryInfo")
            .Produces<List<MedulaOperationResultResponse>>();

        medulaGroup.MapPost("/demo-operation", ExecuteMedulaDemoOperationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ExecuteMedulaDemoOperation")
            .Produces<MedulaOperationResultResponse>();

        medulaGroup.MapPost("/reject", RejectMedulaOutOfScopeAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RejectMedulaOutOfScope")
            .Produces<MedulaOperationResultResponse>();

        return endpoints;
    }

    private static async Task<IResult> GetAllConfigsAsync(
        IIntegrationMockEngine engine,
        CancellationToken cancellationToken)
    {
        var configs = await engine.GetAllConfigurationsAsync(cancellationToken);
        var response = configs.Select(MapConfigToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateConfigAsync(
        IIntegrationMockEngine engine,
        string systemType,
        [FromBody] HospitalManagement.Contracts.Interoperability.UpdateMockServerConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExternalSystemType>(systemType, true, out _))
        {
            return Results.Problem(
                title: "Geçersiz sistem tipi",
                detail: $"'{systemType}' geçerli bir dış sistem tipi değildir.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var appRequest = new HospitalManagement.Modules.Interoperability.Application.UpdateMockServerConfigRequest(
            systemType,
            request.IsEnabled,
            request.FaultMode,
            request.LatencyMilliseconds,
            request.FailureRatePercentage,
            request.MaxRetryAttempts,
            request.TimeoutSeconds);

        var updated = await engine.UpdateConfigurationAsync(appRequest, cancellationToken);
        return Results.Ok(MapConfigToResponse(updated));
    }

    private static async Task<IResult> GetRecentLogsAsync(
        IIntegrationMockEngine engine,
        [FromQuery] string? systemType,
        [FromQuery] int count = 50,
        CancellationToken cancellationToken = default)
    {
        ExternalSystemType? filter = null;
        if (!string.IsNullOrWhiteSpace(systemType) && Enum.TryParse<ExternalSystemType>(systemType, true, out var parsed))
        {
            filter = parsed;
        }

        var logs = await engine.GetRecentLogsAsync(count, filter, cancellationToken);
        var response = logs.Select(l => new IntegrationMessageLogResponse(
            l.Id,
            l.CorrelationId,
            l.SystemType,
            l.Direction,
            l.ActionName,
            l.PayloadSummary,
            l.Status,
            l.RetryCount,
            l.DurationMs,
            l.ErrorMessage,
            l.TimestampUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> ResetCircuitAsync(
        IIntegrationMockEngine engine,
        string systemType,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExternalSystemType>(systemType, true, out var parsed))
        {
            return Results.Problem(
                title: "Geçersiz sistem tipi",
                detail: $"'{systemType}' geçerli bir dış sistem tipi değildir.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await engine.ResetCircuitBreakerAsync(parsed, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SimulateOperationAsync(
        IIntegrationMockEngine engine,
        [FromBody] SimulateMockEngineRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExternalSystemType>(request.SystemType, true, out var systemType))
        {
            return Results.Problem(
                title: "Geçersiz sistem tipi",
                detail: $"'{request.SystemType}' geçerli bir dış sistem tipi değildir.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await engine.ExecuteAsync(
            systemType,
            request.ActionName,
            () => Task.FromResult($"Simulated response for {request.ActionName}"),
            request.Payload,
            null,
            cancellationToken);

        var response = new SimulateMockEngineResponse(
            result.IsSuccess,
            result.Value,
            result.ErrorMessage,
            result.DurationMs,
            result.RetryAttempts,
            result.CorrelationId);

        return Results.Ok(response);
    }

    private static IResult GetFhirCapabilityStatement(IFhirR4Service fhirService)
    {
        var capability = fhirService.GetCapabilityStatement();
        return Results.Ok(capability);
    }

    private static async Task<IResult> GetFhirPatientAsync(
        IFhirR4Service fhirService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                actor,
                id,
                HospitalPermissions.Interoperability.FhirExport,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var patient = await fhirService.GetPatientAsync(id, cancellationToken);
        return patient is not null ? Results.Ok(patient) : Results.NotFound();
    }

    private static async Task<IResult> GetFhirPractitionerAsync(
        IFhirR4Service fhirService,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId)
            || (actorPersonId != id && !actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)))
        {
            return Results.Forbid();
        }

        var practitioner = await fhirService.GetPractitionerAsync(id, cancellationToken);
        return practitioner is not null ? Results.Ok(practitioner) : Results.NotFound();
    }

    private static async Task<IResult> GetFhirObservationAsync(
        IFhirR4Service fhirService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await CanExportPatientAsync(accessControl, actor, patientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var observation = await fhirService.GetObservationAsync(id, patientId, cancellationToken);
        return observation is not null ? Results.Ok(observation) : Results.NotFound();
    }

    private static async Task<IResult> GetFhirDiagnosticReportAsync(
        IFhirR4Service fhirService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await CanExportPatientAsync(accessControl, actor, patientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var report = await fhirService.GetDiagnosticReportAsync(id, patientId, cancellationToken);
        return report is not null ? Results.Ok(report) : Results.NotFound();
    }

    private static async Task<IResult> GetFhirMedicationRequestAsync(
        IFhirR4Service fhirService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await CanExportPatientAsync(accessControl, actor, patientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var med = await fhirService.GetMedicationRequestAsync(id, patientId, cancellationToken);
        return med is not null ? Results.Ok(med) : Results.NotFound();
    }

    private static async Task<IResult> GetFhirPatientExportAsync(
        IFhirR4Service fhirService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await CanExportPatientAsync(accessControl, actor, id, cancellationToken))
        {
            return Results.Forbid();
        }

        try
        {
            var bundle = await fhirService.GetPatientExportBundleAsync(id, cancellationToken);
            return Results.Ok(bundle);
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(
                title: "FHIR MOCK dış sistemi kullanılamıyor",
                detail: "FHIR R4 MOCK export işlemi geçici olarak tamamlanamadı.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> ProcessHl7InboundAsync(
        IHl7V2Service hl7Service,
        [FromBody] Hl7InboundRequest request,
        CancellationToken cancellationToken)
    {
        var ack = await hl7Service.ProcessInboundMessageAsync(request.RawEr7Message, cancellationToken);
        return Results.Ok(new Hl7InboundResponse(
            ack.MessageControlId,
            ack.AckCode,
            ack.TextMessage,
            ack.RawEr7Content));
    }

    private static async Task<IResult> GenerateHl7MessageAsync(
        IHl7V2Service hl7Service,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        TimeProvider timeProvider,
        string messageType,
        [FromBody] Hl7GenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.PatientId.HasValue || request.PatientId.Value == Guid.Empty)
        {
            return Results.Problem(
                title: "Hasta kimliği zorunludur",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var patId = request.PatientId.Value;
        if (!await accessControl.CanAccessPatientAsync(
                actor,
                patId,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }
        var protNo = request.ProtocolNumber ?? "PROTO-DEMO-001";

        string raw;
        switch (messageType.ToUpperInvariant())
        {
            case "ADT_A01":
            case "ADTA01":
            case "A01":
                raw = await hl7Service.GenerateAdtA01MessageAsync(
                    patId,
                    protNo,
                    request.WardName ?? "Dahiliye Servisi",
                    request.BedNumber ?? "102-A",
                    cancellationToken);
                return Results.Ok(new Hl7GenerateResponse("ADT^A01", raw));

            case "ADT_A03":
            case "ADTA03":
            case "A03":
                raw = await hl7Service.GenerateAdtA03MessageAsync(
                    patId,
                    protNo,
                    timeProvider.GetUtcNow().UtcDateTime,
                    cancellationToken);
                return Results.Ok(new Hl7GenerateResponse("ADT^A03", raw));

            case "ORM_O01":
            case "ORMO01":
            case "O01":
                raw = await hl7Service.GenerateOrmO01MessageAsync(
                    request.OrderId ?? Guid.NewGuid(),
                    patId,
                    request.TestCode ?? "CBC",
                    request.TestName ?? "Hemogram",
                    cancellationToken);
                return Results.Ok(new Hl7GenerateResponse("ORM^O01", raw));

            case "ORU_R01":
            case "ORUR01":
            case "R01":
                raw = await hl7Service.GenerateOruR01MessageAsync(
                    request.OrderId ?? Guid.NewGuid(),
                    patId,
                    request.TestCode ?? "HGB",
                    request.ResultValue ?? "14.2",
                    request.Units ?? "g/dL",
                    cancellationToken);
                return Results.Ok(new Hl7GenerateResponse("ORU^R01", raw));

            default:
                return Results.Problem(
                    title: "Geçersiz HL7 mesaj türü",
                    detail: $"Desteklenen mesaj türleri: ADT_A01, ADT_A03, ORM_O01, ORU_R01. Alınan: {messageType}",
                    statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetHl7DeadLettersAsync(
        IHl7V2Service hl7Service,
        CancellationToken cancellationToken)
    {
        var entries = await hl7Service.GetDeadLetterEntriesAsync(cancellationToken);
        var response = entries.Select(e => new Hl7DeadLetterResponse(
            e.Id,
            e.MessageControlId,
            e.MessageType,
            e.FailureReason,
            e.PayloadSummary,
            e.ReceivedAtUtc,
            e.RetryCount,
            e.IsResolved,
            e.ResolvedAtUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> RetryHl7DeadLetterAsync(
        IHl7V2Service hl7Service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var success = await hl7Service.RetryDeadLetterEntryAsync(id, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> QueryDicomWorklistAsync(
        IDicomPacsService dicomService,
        IDiagnosticsAccessContext accessContext,
        ClaimsPrincipal actor,
        [FromQuery] string? modality,
        [FromQuery] string? scheduledDate,
        [FromQuery] string? aeTitle,
        CancellationToken cancellationToken)
    {
        if (!await accessContext.CanAccessDiagnosticAreaAsync(
                actor,
                DiagnosticOrderType.Radiology,
                HospitalPermissions.Diagnostics.RadiologyWorklistView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var items = await dicomService.QueryModalityWorklistAsync(modality, scheduledDate, aeTitle, cancellationToken);
        var response = items.Select(MapWorklistToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateDicomWorklistOrderAsync(
        IDicomPacsService dicomService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        [FromBody] CreateDicomWorklistOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                actor,
                request.PatientId,
                HospitalPermissions.Diagnostics.DiagnosticOrderCreate,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var dto = new CreateDicomWorklistOrderDto(
            request.PatientId,
            request.Modality,
            request.ProcedureDescription,
            request.AeTitle);

        var created = await dicomService.CreateWorklistOrderAsync(dto, cancellationToken);
        return Results.Ok(MapWorklistToResponse(created));
    }

    private static async Task<IResult> QueryDicomStudyAsync(
        IDicomPacsService dicomService,
        IDiagnosticsAccessContext accessContext,
        ClaimsPrincipal actor,
        string studyInstanceUid,
        CancellationToken cancellationToken)
    {
        if (!await accessContext.CanAccessDiagnosticAreaAsync(
                actor,
                DiagnosticOrderType.Radiology,
                HospitalPermissions.Diagnostics.RadiologyWorklistView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var study = await dicomService.QueryStudyMetadataAsync(studyInstanceUid, cancellationToken);
        return study is not null ? Results.Ok(MapStudyToResponse(study)) : Results.NotFound();
    }

    private static async Task<IResult> QueryPatientDicomStudiesAsync(
        IDicomPacsService dicomService,
        IDiagnosticsAccessContext accessContext,
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessContext.CanAccessDiagnosticAreaAsync(
                actor,
                DiagnosticOrderType.Radiology,
                HospitalPermissions.Diagnostics.RadiologyWorklistView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var studies = await dicomService.QueryPatientStudiesAsync(patientId, cancellationToken);
        var response = studies.Select(MapStudyToResponse).ToList();
        return Results.Ok(response);
    }

    private static DicomWorklistItemResponse MapWorklistToResponse(DicomWorklistItem item) =>
        new(
            item.AccessionNumber,
            item.PatientId,
            item.PatientName,
            item.Modality,
            item.ScheduledStationAeTitle,
            item.ScheduledDate,
            item.ScheduledTime,
            item.ScheduledProcedureStepDescription,
            item.RequestedProcedureId,
            item.StudyInstanceUid,
            item.Status.ToString());

    private static DicomStudyMetadataResponse MapStudyToResponse(DicomStudyMetadata study) =>
        new(
            study.StudyInstanceUid,
            study.AccessionNumber,
            study.PatientId,
            study.PatientName,
            study.StudyDate,
            study.StudyDescription,
            study.Modality,
            study.NumberOfSeries,
            study.NumberOfInstances,
            study.Series.Select(s => new DicomSeriesMetadataResponse(
                s.SeriesInstanceUid,
                s.SeriesNumber,
                s.Modality,
                s.SeriesDescription,
                s.NumberOfInstances,
                s.Instances.Select(i => new DicomInstanceMetadataResponse(
                    i.SopInstanceUid,
                    i.InstanceNumber,
                    i.SopClassUid,
                    i.Rows,
                    i.Columns,
                    i.BitsAllocated,
                    i.SyntheticImageUrl)).ToList())).ToList());

    private static async Task<IResult> QueryMhrsSlotsAsync(
        IMhrsService mhrsService,
        [FromQuery] Guid? doctorId,
        [FromQuery] string? clinicCode,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var slots = await mhrsService.QueryAvailableSlotsAsync(doctorId, clinicCode, date, cancellationToken);
        var response = slots.Select(s => new MhrsSlotResponse(
            s.SlotId,
            s.DoctorId,
            s.DoctorName,
            s.ClinicCode,
            s.ClinicName,
            s.HospitalCode,
            s.SlotDateTimeUtc,
            s.DurationMinutes,
            s.IsAvailable)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> BookMhrsAppointmentAsync(
        IMhrsService mhrsService,
        [FromBody] MhrsBookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new BookMhrsAppointmentDto(
            request.SlotId,
            request.PatientNationalId,
            request.PatientFullName,
            request.DoctorId,
            request.DoctorName,
            request.ClinicName,
            request.AppointmentDateTimeUtc,
            request.IdempotencyKey);

        var booked = await mhrsService.BookAppointmentAsync(dto, cancellationToken);
        return Results.Ok(MapMhrsToResponse(booked));
    }

    private static async Task<IResult> CancelMhrsAppointmentAsync(
        IMhrsService mhrsService,
        string mhrsAppointmentId,
        [FromBody] MhrsCancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var cancelled = await mhrsService.CancelAppointmentAsync(mhrsAppointmentId, request.Reason, request.IsDoctor, cancellationToken);
        return Results.Ok(MapMhrsToResponse(cancelled));
    }

    private static async Task<IResult> GetPatientMhrsAppointmentsAsync(
        IMhrsService mhrsService,
        [FromBody] MhrsPatientAppointmentsQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PatientNationalId))
        {
            return Results.Problem(
                title: "Hasta kimlik numarası zorunludur",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var list = await mhrsService.GetPatientAppointmentsAsync(request.PatientNationalId, cancellationToken);
        var response = list.Select(MapMhrsToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> SyncMhrsScheduleAsync(
        IMhrsService mhrsService,
        TimeProvider timeProvider,
        [FromQuery] DateTime? syncDate,
        CancellationToken cancellationToken)
    {
        var targetDate = syncDate ?? timeProvider.GetUtcNow().UtcDateTime;
        var summary = await mhrsService.SyncWithLocalScheduleAsync(targetDate, cancellationToken);
        return Results.Ok(new MhrsSyncSummaryResponse(
            summary.TotalSynced,
            summary.ConflictsDetected,
            summary.NewAppointmentsAdded,
            summary.CancelledAppointments,
            summary.SyncTimestampUtc));
    }

    private static MhrsAppointmentResponse MapMhrsToResponse(MhrsAppointmentDto a) =>
        new(
            a.Id,
            a.MhrsAppointmentId,
            a.SlotId,
            a.PatientNationalId,
            a.PatientFullName,
            a.DoctorId,
            a.DoctorName,
            a.ClinicName,
            a.AppointmentDateTimeUtc,
            a.Status,
            a.IdempotencyKey,
            a.CancellationReason,
            a.CreatedAtUtc,
            a.UpdatedAtUtc);

    private static async Task<IResult> QueryENabizQueueAsync(
        IENabizService enabizService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        if (!patientId.HasValue || patientId == Guid.Empty
            || !await accessControl.CanAccessPatientAsync(
                actor,
                patientId.Value,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        ENabizTransmissionStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ENabizTransmissionStatus>(status, true, out var parsed))
        {
            statusEnum = parsed;
        }

        var list = await enabizService.QueryTransmissionQueueAsync(statusEnum, patientId, cancellationToken);
        var response = list.Select(MapENabizToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> EnqueueENabizPackageAsync(
        IENabizService enabizService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        [FromBody] EnqueueENabizPackageRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                actor,
                request.PatientId,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var packageType = (ENabizPackageType)request.PackageType;
        var dto = new EnqueueENabizPackageDto(
            packageType,
            request.PatientId,
            request.PatientNationalId,
            request.HasPatientConsent,
            request.PayloadSummary);

        var queued = await enabizService.EnqueuePackageAsync(dto, cancellationToken);
        return Results.Ok(MapENabizToResponse(queued));
    }

    private static async Task<IResult> SendENabizTransmissionAsync(
        IENabizService enabizService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var existing = await enabizService.GetTransmissionByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        if (!await accessControl.CanAccessPatientAsync(
                actor,
                existing.PatientId,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var sent = await enabizService.SendTransmissionAsync(id, cancellationToken);
        return Results.Ok(MapENabizToResponse(sent));
    }

    private static async Task<IResult> RetryENabizTransmissionAsync(
        IENabizService enabizService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var existing = await enabizService.GetTransmissionByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        if (!await accessControl.CanAccessPatientAsync(
                actor,
                existing.PatientId,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var retried = await enabizService.RetryTransmissionAsync(id, cancellationToken);
        return Results.Ok(MapENabizToResponse(retried));
    }

    private static async Task<IResult> GetENabizTransmissionByIdAsync(
        IENabizService enabizService,
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await enabizService.GetTransmissionByIdAsync(id, cancellationToken);
        if (record is null)
        {
            return Results.NotFound();
        }

        return await accessControl.CanAccessPatientAsync(
                actor,
                record.PatientId,
                HospitalPermissions.Interoperability.ClinicalExchange,
                false,
                cancellationToken)
            ? Results.Ok(MapENabizToResponse(record))
            : Results.Forbid();
    }

    private static ENabizTransmissionResponse MapENabizToResponse(ENabizTransmissionDto t) =>
        new(
            t.Id,
            t.SysTakipNo,
            t.PackageTypeCode,
            t.PackageTypeName,
            t.PatientId,
            t.PatientNationalId,
            t.HasPatientConsent,
            t.Status,
            t.PayloadSummary,
            t.ResponseCode,
            t.ResponseMessage,
            t.RetryCount,
            t.QueuedAtUtc,
            t.SentAtUtc,
            t.LastAttemptAtUtc);

    private static MockServerConfigResponse MapConfigToResponse(MockServerConfigDto c) =>
        new(
            c.Id,
            c.SystemType,
            c.IsEnabled,
            c.FaultMode,
            c.LatencyMilliseconds,
            c.FailureRatePercentage,
            c.MaxRetryAttempts,
            c.TimeoutSeconds,
            c.UpdatedAtUtc);

    private static async Task<IResult> GetMedulaBoundaryInfoAsync(
        IMedulaBoundaryService medulaService,
        CancellationToken cancellationToken)
    {
        var list = await medulaService.GetBoundaryInfoAsync(cancellationToken);
        var response = list.Select(MapMedulaToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> ExecuteMedulaDemoOperationAsync(
        IMedulaBoundaryService medulaService,
        [FromBody] MedulaDemoOperationRequest request,
        CancellationToken cancellationToken)
    {
        var opType = (MedulaOperationType)request.OperationType;
        var result = await medulaService.ExecuteDemoOperationAsync(opType, request.RequestSummary, cancellationToken);
        return Results.Ok(MapMedulaToResponse(result));
    }

    private static async Task<IResult> RejectMedulaOutOfScopeAsync(
        IMedulaBoundaryService medulaService,
        [FromBody] MedulaOutOfScopeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medulaService.RejectOutOfScopeAsync(request.OperationName, cancellationToken);
        return Results.Ok(MapMedulaToResponse(result));
    }

    private static MedulaOperationResultResponse MapMedulaToResponse(MedulaOperationResultDto r) =>
        new(
            r.Id,
            r.OperationType,
            r.Status,
            r.StatusDescription,
            r.DemoDisclaimer,
            r.RequestSummary,
            r.ResponseSummary,
            r.ProcessedAtUtc);

    private static Task<bool> CanExportPatientAsync(
        ClinicalRecordAccessControl accessControl,
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken) =>
        patientId == Guid.Empty
            ? Task.FromResult(false)
            : accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.Interoperability.FhirExport,
                false,
                cancellationToken);
}
