using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Diagnostics;

public static class DiagnosticsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var diagnosticsGroup = endpoints.MapGroup("/api/v1/diagnostics")
            .RequireAuthorization();

        var orderGroup = diagnosticsGroup.MapGroup("/orders");

        var labCatalogGroup = diagnosticsGroup.MapGroup("/lab-catalog");

        var specimenGroup = diagnosticsGroup.MapGroup("/specimens");

        specimenGroup.MapPost("/collect", CollectSpecimenAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .WithName("CollectSpecimen")
            .Produces<SpecimenDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        specimenGroup.MapPost("/{id:guid}/transit", TransitSpecimenAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .WithName("TransitSpecimen")
            .Produces<SpecimenDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        specimenGroup.MapPost("/{id:guid}/receive", ReceiveSpecimenAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .WithName("ReceiveSpecimen")
            .Produces<SpecimenDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        specimenGroup.MapPost("/{id:guid}/reject", RejectSpecimenAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .WithName("RejectSpecimen")
            .Produces<SpecimenDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        specimenGroup.MapGet("/{id:guid}", GetSpecimenByIdAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetSpecimenById")
            .Produces<SpecimenDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        specimenGroup.MapGet("/by-barcode/{barcode}", GetSpecimenByBarcodeAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetSpecimenByBarcode")
            .Produces<SpecimenDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        specimenGroup.MapGet("/by-order/{orderId:guid}", GetSpecimensByOrderIdAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetSpecimensByOrderId")
            .Produces<List<SpecimenSummaryResponse>>();

        specimenGroup.MapGet("/worklist", GetSpecimenWorklistAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetSpecimenWorklist")
            .Produces<List<SpecimenSummaryResponse>>();

        var labResultGroup = diagnosticsGroup.MapGroup("/lab-results");

        labResultGroup.MapPost(string.Empty, CreateDraftLabResultAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .WithName("CreateDraftLabResult")
            .Produces<LabResultDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        labResultGroup.MapPut("/{id:guid}/items", UpdateLabResultItemsAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .WithName("UpdateLabResultItems")
            .Produces<LabResultDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        labResultGroup.MapPost("/{id:guid}/technical-approve", ApproveLabResultTechnicallyAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .WithName("ApproveLabResultTechnically")
            .Produces<LabResultDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        labResultGroup.MapPost("/{id:guid}/clinical-approve", ApproveLabResultClinicallyAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .WithName("ApproveLabResultClinically")
            .Produces<LabResultDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        labResultGroup.MapPost("/{id:guid}/correct", CorrectLabResultAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .WithName("CorrectLabResult")
            .Produces<LabResultDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        labResultGroup.MapGet("/{id:guid}", GetLabResultByIdAsync)
            .WithName("GetLabResultById")
            .Produces<LabResultDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        labResultGroup.MapGet("/by-order/{orderId:guid}", GetLabResultsByOrderIdAsync)
            .WithName("GetLabResultsByOrderId")
            .Produces<List<LabResultDetailResponse>>();

        labResultGroup.MapGet("/worklist", GetLabResultWorklistAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetLabResultWorklist")
            .Produces<List<LabResultSummaryResponse>>();

        var criticalNotificationGroup = diagnosticsGroup.MapGroup("/critical-notifications");

        criticalNotificationGroup.MapGet("/active", GetActiveCriticalNotificationsAsync)
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterView)
            .WithName("GetActiveCriticalNotifications")
            .Produces<List<CriticalResultNotificationResponse>>();

        criticalNotificationGroup.MapGet("/{id:guid}", GetCriticalNotificationByIdAsync)
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterView)
            .WithName("GetCriticalNotificationById")
            .Produces<CriticalResultNotificationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        criticalNotificationGroup.MapPost("/{id:guid}/acknowledge", AcknowledgeCriticalNotificationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterView)
            .WithName("AcknowledgeCriticalNotification")
            .Produces<CriticalResultNotificationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        criticalNotificationGroup.MapPost("/{id:guid}/escalate", EscalateCriticalNotificationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterView)
            .WithName("EscalateCriticalNotification")
            .Produces<CriticalResultNotificationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var radiologyGroup = diagnosticsGroup.MapGroup("/radiology");

        radiologyGroup.MapGet("/catalog", GetRadiologyCatalogAsync)
            .WithName("GetRadiologyCatalog")
            .Produces<List<RadiologyCatalogItemResponse>>();

        radiologyGroup.MapGet("/studies/worklist", GetRadiologyWorklistAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyWorklistView)
            .WithName("GetRadiologyWorklist")
            .Produces<List<RadiologyStudySummaryResponse>>();

        radiologyGroup.MapGet("/studies/{id:guid}", GetRadiologyStudyByIdAsync)
            .WithName("GetRadiologyStudyById")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        radiologyGroup.MapPost("/studies/ensure", EnsureRadiologyStudyAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyWorklistView)
            .WithName("EnsureRadiologyStudy")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        radiologyGroup.MapPost("/studies/{id:guid}/schedule", ScheduleRadiologyStudyAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyStudyComplete)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ScheduleRadiologyStudy")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapPost("/studies/{id:guid}/complete-acquisition", CompleteRadiologyAcquisitionAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyStudyComplete)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteRadiologyAcquisition")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapPost("/studies/{id:guid}/draft-report", DraftRadiologyReportAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyReportFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("DraftRadiologyReport")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapPost("/studies/{id:guid}/finalize-report", FinalizeRadiologyReportAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyReportFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("FinalizeRadiologyReport")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapPost("/studies/{id:guid}/add-addendum", AddRadiologyAddendumAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyReportFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AddRadiologyAddendum")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapPost("/studies/{id:guid}/cancel", CancelRadiologyStudyAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.RadiologyReportFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelRadiologyStudy")
            .Produces<RadiologyStudyDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        radiologyGroup.MapGet("/studies/{id:guid}/dicom-metadata", GetDicomStudyMetadataAsync)
            .WithName("GetDicomStudyMetadata")
            .Produces<DicomStudyMetadataResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        radiologyGroup.MapPost("/studies/{id:guid}/dicom-preview-token", GenerateDicomPreviewTokenAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("GenerateDicomPreviewToken")
            .Produces<DicomPreviewTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        radiologyGroup.MapPost("/dicom-preview", GetDicomPreviewImageAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("GetDicomPreviewImage")
            .Produces(StatusCodes.Status200OK, contentType: "image/svg+xml")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var pathologyGroup = diagnosticsGroup.MapGroup("/pathology");

        pathologyGroup.MapGet("/cases/worklist", GetPathologyWorklistAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetPathologyWorklist")
            .Produces<List<PathologyCaseSummaryResponse>>();

        pathologyGroup.MapGet("/cases/{id:guid}", GetPathologyCaseByIdAsync)
            .WithName("GetPathologyCaseById")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        pathologyGroup.MapPost("/cases/ensure", EnsurePathologyCaseAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("EnsurePathologyCase")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        pathologyGroup.MapPost("/cases/{id:guid}/receive-specimen", ReceivePathologySpecimenAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ReceivePathologySpecimen")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/gross-exam", RecordPathologyGrossExamAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordPathologyGrossExam")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/microscopic-exam", RecordPathologyMicroscopicExamAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordPathologyMicroscopicExam")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/draft-report", DraftPathologyReportAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultEditDraft)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("DraftPathologyReport")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/finalize-report", FinalizePathologyReportAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("FinalizePathologyReport")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/correct-report", CorrectPathologyReportAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CorrectPathologyReport")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pathologyGroup.MapPost("/cases/{id:guid}/cancel", CancelPathologyCaseAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelPathologyCase")
            .Produces<PathologyCaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var bloodBankGroup = diagnosticsGroup.MapGroup("/blood-bank");

        bloodBankGroup.MapGet("/inventory", GetBloodInventoryAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetBloodInventory")
            .Produces<List<BloodUnitResponse>>();

        bloodBankGroup.MapGet("/inventory/summary", GetBloodInventorySummaryAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryWorklistView)
            .WithName("GetBloodInventorySummary")
            .Produces<BloodInventorySummaryResponse>();

        bloodBankGroup.MapGet("/crossmatch/worklist", GetCrossmatchWorklistAsync)
            .WithName("GetCrossmatchWorklist")
            .Produces<List<CrossmatchSummaryResponse>>();

        bloodBankGroup.MapGet("/crossmatch/{id:guid}", GetCrossmatchByIdAsync)
            .WithName("GetCrossmatchById")
            .Produces<CrossmatchDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        bloodBankGroup.MapPost("/crossmatch", CreateCrossmatchRequestAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateCrossmatchRequest")
            .Produces<CrossmatchDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bloodBankGroup.MapPost("/crossmatch/{id:guid}/test", PerformCrossmatchTestAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratoryResultFinalize)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("PerformCrossmatchTest")
            .Produces<CrossmatchDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bloodBankGroup.MapPost("/units/{id:guid}/issue", IssueBloodUnitAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.LaboratorySpecimenTransition)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("IssueBloodUnit")
            .Produces<BloodUnitResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bloodBankGroup.MapPost("/units/{id:guid}/transfuse", RecordTransfusionAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.BloodTransfusionRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordTransfusion")
            .Produces<BloodUnitResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var timelineGroup = diagnosticsGroup.MapGroup("/timeline");

        timelineGroup.MapGet("/patient/{patientId:guid}", GetPatientDiagnosticTimelineAsync)
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterView)
            .WithName("GetPatientDiagnosticTimeline")
            .Produces<PatientDiagnosticTimelineResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        diagnosticsGroup.MapGet("/results/my", GetMyPatientPortalResultsAsync)
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn)
            .WithName("GetMyPatientPortalResults")
            .Produces<List<PatientPortalResultSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        labCatalogGroup.MapGet(string.Empty, SearchLabCatalogAsync)
            .WithName("SearchLabCatalog")
            .Produces<List<LabCatalogSummaryResponse>>();

        labCatalogGroup.MapGet("/{id:guid}", GetLabCatalogItemByIdAsync)
            .WithName("GetLabCatalogItemById")
            .Produces<LabCatalogItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        labCatalogGroup.MapPost("/import", ImportLabCatalogAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("ImportLabCatalog")
            .Produces<ImportLabCatalogResponse>()
            .ProducesValidationProblem();

        orderGroup.MapPost(string.Empty, CreateDiagnosticOrderDraftAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("CreateDiagnosticOrderDraft")
            .Produces<DiagnosticOrderDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        orderGroup.MapGet("/{id:guid}", GetDiagnosticOrderByIdAsync)
            .WithName("GetDiagnosticOrderById")
            .Produces<DiagnosticOrderDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        orderGroup.MapGet("/by-encounter/{encounterId:guid}", GetDiagnosticOrdersByEncounterAsync)
            .WithName("GetDiagnosticOrdersByEncounter")
            .Produces<List<DiagnosticOrderSummaryResponse>>()
            .ProducesValidationProblem();

        orderGroup.MapGet("/by-patient/{patientId:guid}", GetDiagnosticOrdersByPatientAsync)
            .WithName("GetDiagnosticOrdersByPatient")
            .Produces<List<DiagnosticOrderSummaryResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        orderGroup.MapGet("/worklist", GetDiagnosticOrderWorklistAsync)
            .WithName("GetDiagnosticOrderWorklist")
            .Produces<List<DiagnosticOrderSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        orderGroup.MapPut("/{id:guid}", UpdateDiagnosticOrderDraftAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("UpdateDiagnosticOrderDraft")
            .Produces<DiagnosticOrderDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orderGroup.MapPost("/{id:guid}/place", PlaceDiagnosticOrderAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("PlaceDiagnosticOrder")
            .Produces<DiagnosticOrderDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orderGroup.MapPost("/{id:guid}/cancel", CancelDiagnosticOrderAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("CancelDiagnosticOrder")
            .Produces<DiagnosticOrderDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orderGroup.MapPost("/{id:guid}/entered-in-error", MarkDiagnosticOrderEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)
            .WithName("MarkDiagnosticOrderEnteredInError")
            .Produces<DiagnosticOrderDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateDiagnosticOrderDraftAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateDiagnosticOrderDraftRequest request,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<DiagnosticOrderType>(request.OrderType, true, out var orderType))
        {
            orderType = DiagnosticOrderType.Laboratory;
        }

        if (!Enum.TryParse<DiagnosticOrderPriority>(request.Priority, true, out var priority))
        {
            priority = DiagnosticOrderPriority.Routine;
        }

        var items = request.Items?.Select(i => new CreateDiagnosticOrderItemCommand(
            i.CatalogCode,
            i.CatalogItemName,
            i.Category,
            i.SpecialInstructions)).ToList() ?? [];

        var command = new CreateDiagnosticOrderDraftCommand(
            request.PatientId,
            request.EncounterId,
            request.DepartmentId,
            Guid.Empty,
            orderType,
            priority,
            request.ClinicalIndication,
            request.OrderNotes,
            items);

        var result = await service.CreateDraftAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Created($"/api/v1/diagnostics/orders/{r.Id}", MapToDetailResponse(r)));
    }

    private static async Task<IResult> UpdateDiagnosticOrderDraftAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateDiagnosticOrderDraftRequest request,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        DiagnosticOrderPriority? priority = null;
        if (!string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<DiagnosticOrderPriority>(request.Priority, true, out var parsedPriority))
        {
            priority = parsedPriority;
        }

        var items = request.Items?.Select(i => new CreateDiagnosticOrderItemCommand(
            i.CatalogCode,
            i.CatalogItemName,
            i.Category,
            i.SpecialInstructions)).ToList() ?? [];

        var command = new UpdateDiagnosticOrderDraftCommand(
            id,
            Guid.Empty,
            priority,
            request.ClinicalIndication,
            request.OrderNotes,
            items);

        var result = await service.UpdateDraftAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> PlaceDiagnosticOrderAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] PlaceDiagnosticOrderRequest? request,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        var command = new PlaceDiagnosticOrderCommand(id, Guid.Empty, request?.Notes);
        var result = await service.PlaceAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> CancelDiagnosticOrderAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelDiagnosticOrderRequest request,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["İptal gerekçesi zorunludur."],
            });
        }

        var command = new CancelDiagnosticOrderCommand(id, Guid.Empty, request.Reason);
        var result = await service.CancelAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> MarkDiagnosticOrderEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkDiagnosticOrderEnteredInErrorRequest request,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Hatalı giriş gerekçesi zorunludur."],
            });
        }

        var command = new MarkDiagnosticOrderEnteredInErrorCommand(id, Guid.Empty, request.Reason);
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> GetDiagnosticOrderByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> GetDiagnosticOrdersByEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByEncounterAsync(actor, encounterId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetDiagnosticOrdersByPatientAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IDiagnosticOrderService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByPatientAsync(actor, patientId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetDiagnosticOrderWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? orderType,
        [FromQuery] string? status,
        [FromQuery] string? orderNumber,
        [FromQuery] Guid? patientId,
        [FromQuery] int maxResults = 50,
        [FromServices] IDiagnosticOrderService service = null!,
        CancellationToken cancellationToken = default)
    {
        DiagnosticOrderType? parsedOrderType = null;
        if (!string.IsNullOrWhiteSpace(orderType) && Enum.TryParse<DiagnosticOrderType>(orderType, true, out var ot))
        {
            parsedOrderType = ot;
        }

        DiagnosticOrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DiagnosticOrderStatus>(status, true, out var st))
        {
            parsedStatus = st;
        }

        var result = await service.GetWorklistAsync(
            actor,
            parsedOrderType,
            parsedStatus,
            orderNumber,
            patientId,
            maxResults,
            cancellationToken);

        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static IResult ToHttpResult<T>(
        DiagnosticOrderOperationResult<T> result,
        Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            DiagnosticOperationStatus.Success => onSuccess(result.Value!),
            DiagnosticOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Bulunamadı",
                detail: result.ErrorMessage ?? "İstenen kaynak bulunamadı."),
            DiagnosticOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors ?? new Dictionary<string, string[]>
                {
                    ["general"] = [result.ErrorMessage ?? "Doğrulama hatası."],
                }),
            DiagnosticOperationStatus.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Yetkisiz Erişim",
                detail: result.ErrorMessage ?? "Bu işlem için yetkiniz bulunmamaktadır."),
            DiagnosticOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage ?? "İşlem mevcut durum ile çelişmektedir."),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static DiagnosticOrderDetailResponse MapToDetailResponse(DiagnosticOrderDetailDto d) =>
        new(
            d.Id,
            d.OrderNumber,
            d.PatientId,
            d.EncounterId,
            d.PlacingDoctorId,
            d.DepartmentId,
            d.OrderType.ToString(),
            d.Priority.ToString(),
            d.Status.ToString(),
            d.ClinicalIndication,
            d.OrderNotes,
            d.CancellationReason,
            d.EnteredInErrorReason,
            d.PlacedAtUtc,
            d.CompletedAtUtc,
            d.CancelledAtUtc,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Version,
            d.Items.Select(i => new DiagnosticOrderItemResponse(
                i.Id,
                i.DiagnosticOrderId,
                i.CatalogCode,
                i.CatalogItemName,
                i.Category,
                i.Status.ToString(),
                i.SpecialInstructions,
                i.CreatedAtUtc,
                i.UpdatedAtUtc)).ToList());

    private static DiagnosticOrderSummaryResponse MapToSummaryResponse(DiagnosticOrderSummaryDto s) =>
        new(
            s.Id,
            s.OrderNumber,
            s.PatientId,
            s.EncounterId,
            s.PlacingDoctorId,
            s.DepartmentId,
            s.OrderType.ToString(),
            s.Priority.ToString(),
            s.Status.ToString(),
            s.ItemCount,
            s.PlacedAtUtc,
            s.CreatedAtUtc);
    private static async Task<IResult> SearchLabCatalogAsync(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int maxResults = 50,
        [FromServices] ILabCatalogService service = null!,
        CancellationToken cancellationToken = default)
    {
        var items = await service.SearchAsync(query, category, isActive, maxResults, cancellationToken);
        var response = items.Select(i => new LabCatalogSummaryResponse(
            i.Id,
            i.Code,
            i.Name,
            i.Category,
            i.SpecimenType,
            i.ContainerType,
            i.IsPanel,
            i.TurnaroundMinutes,
            i.IsActive,
            i.ParameterCount)).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetLabCatalogItemByIdAsync(
        [FromRoute] Guid id,
        [FromServices] ILabCatalogService service,
        CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Bulunamadı", detail: "Laboratuvar test kalemi bulunamadı.");
        }

        var response = new LabCatalogItemResponse(
            item.Id,
            item.Code,
            item.Name,
            item.Category,
            item.SpecimenType,
            item.ContainerType,
            item.IsPanel,
            item.TurnaroundMinutes,
            item.IsActive,
            item.CatalogVersion,
            item.Description,
            item.CreatedAtUtc,
            item.Parameters.Select(p => new LabCatalogParameterResponse(
                p.Id,
                p.LabCatalogItemId,
                p.Code,
                p.Name,
                p.Unit,
                p.ReferenceRangeLow,
                p.ReferenceRangeHigh,
                p.CriticalLow,
                p.CriticalHigh,
                p.ValueType,
                p.SortOrder)).ToList());

        return Results.Ok(response);
    }

    private static async Task<IResult> ImportLabCatalogAsync(
        ClaimsPrincipal actor,
        [FromBody] ImportLabCatalogRequest request,
        [FromServices] ILabCatalogService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CatalogVersion))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["catalogVersion"] = ["Katalog sürümü zorunludur."],
            });
        }

        var command = new ImportLabCatalogCommand(
            request.CatalogVersion,
            request.Items.Select(i => new ImportLabCatalogItemCommand(
                i.Code,
                i.Name,
                i.Category,
                i.SpecimenType,
                i.ContainerType,
                i.IsPanel,
                i.TurnaroundMinutes,
                i.Description,
                i.Parameters.Select(p => new ImportLabCatalogParameterCommand(
                    p.Code,
                    p.Name,
                    p.Unit,
                    p.ReferenceRangeLow,
                    p.ReferenceRangeHigh,
                    p.CriticalLow,
                    p.CriticalHigh,
                    p.ValueType,
                    p.SortOrder)).ToList())).ToList());

        var result = await service.ImportCatalogAsync(actor, command, cancellationToken);
        return Results.Ok(new ImportLabCatalogResponse(
            result.CatalogVersion,
            result.TotalProcessed,
            result.TotalAdded,
            result.TotalUpdated,
            result.ImportedAtUtc));
    }

    private static async Task<IResult> CollectSpecimenAsync(
        ClaimsPrincipal actor,
        [FromBody] CollectSpecimenRequest request,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        if (request is null || request.DiagnosticOrderId == Guid.Empty || request.PatientId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["Geçerli istem ve hasta kimliği gereklidir."],
            });
        }

        var command = new CollectSpecimenCommand(
            request.DiagnosticOrderId,
            request.PatientId,
            request.SpecimenType,
            request.ContainerType,
            request.CollectionLocation,
            request.CollectionNotes);

        var result = await service.CollectAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Created($"/api/v1/diagnostics/specimens/{r.Id}", MapToDetailResponse(r)));
    }

    private static async Task<IResult> TransitSpecimenAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] TransitSpecimenRequest? request,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        var command = new TransitSpecimenCommand(request?.Location, request?.Notes);
        var result = await service.MarkInTransitAsync(actor, id, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> ReceiveSpecimenAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] ReceiveSpecimenRequest? request,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        var command = new ReceiveSpecimenCommand(request?.Location, request?.Notes);
        var result = await service.ReceiveAtLabAsync(actor, id, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> RejectSpecimenAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] RejectSpecimenRequest request,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["rejectionReason"] = ["Numune red gerekçesi zorunludur."],
            });
        }

        var command = new RejectSpecimenCommand(request.RejectionReason, request.Location, request.Notes);
        var result = await service.RejectAsync(actor, id, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> GetSpecimenByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, specimen => Results.Ok(MapToDetailResponse(specimen)));
    }

    private static async Task<IResult> GetSpecimenByBarcodeAsync(
        ClaimsPrincipal actor,
        [FromRoute] string barcode,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByBarcodeAsync(actor, barcode, cancellationToken);
        return ToHttpResult(result, specimen => Results.Ok(MapToDetailResponse(specimen)));
    }

    private static async Task<IResult> GetSpecimensByOrderIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid orderId,
        [FromServices] ISpecimenService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByOrderIdAsync(actor, orderId, cancellationToken);
        return ToHttpResult(result, specimens => Results.Ok(specimens.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetSpecimenWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? status,
        [FromQuery] string? barcode,
        [FromQuery] Guid? patientId,
        [FromQuery] int maxResults = 50,
        [FromServices] ISpecimenService service = null!,
        CancellationToken cancellationToken = default)
    {
        SpecimenStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SpecimenStatus>(status, true, out var st))
        {
            parsedStatus = st;
        }

        var result = await service.GetWorklistAsync(actor, parsedStatus, barcode, patientId, maxResults, cancellationToken);
        return ToHttpResult(result, specimens => Results.Ok(specimens.Select(MapToSummaryResponse).ToList()));
    }

    private static SpecimenDetailResponse MapToDetailResponse(SpecimenDetailDto s) =>
        new(
            s.Id,
            s.Barcode,
            s.DiagnosticOrderId,
            s.PatientId,
            s.SpecimenType,
            s.ContainerType,
            s.Status.ToString(),
            s.CollectionNotes,
            s.RejectionReason,
            s.CollectedAtUtc,
            s.CollectedByUserId,
            s.ReceivedAtUtc,
            s.ReceivedByUserId,
            s.RejectedAtUtc,
            s.RejectedByUserId,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.Version,
            s.Transitions.Select(t => new SpecimenTransitionEventResponse(
                t.Id,
                t.SpecimenId,
                t.FromStatus.ToString(),
                t.ToStatus.ToString(),
                t.TransitionedAtUtc,
                t.ActorUserId,
                t.ActorRole,
                t.Location,
                t.Notes)).ToList());

    private static SpecimenSummaryResponse MapToSummaryResponse(SpecimenSummaryDto s) =>
        new(
            s.Id,
            s.Barcode,
            s.DiagnosticOrderId,
            s.PatientId,
            s.SpecimenType,
            s.ContainerType,
            s.Status.ToString(),
            s.CollectedAtUtc,
            s.ReceivedAtUtc,
            s.CreatedAtUtc);

    private static async Task<IResult> CreateDraftLabResultAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateDraftLabResultRequest request,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var values = request.Items?.Select(i => new ParameterValueCommand(
            i.ParameterCode,
            i.NumericValue,
            i.StringValue,
            i.Notes)).ToList();

        var command = new CreateDraftLabResultCommand(
            request.DiagnosticOrderId,
            request.DiagnosticOrderItemId,
            request.SpecimenId,
            request.PatientId,
            request.CatalogCode,
            request.CatalogItemName,
            request.ClinicalNotes,
            values);

        var result = await service.CreateDraftAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Created($"/api/v1/diagnostics/lab-results/{r.Id}", MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> UpdateLabResultItemsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateLabResultItemsRequest request,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Items is null || request.Items.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["En az bir parametre değeri girilmelidir."],
            });
        }

        var values = request.Items.Select(i => new ParameterValueCommand(
            i.ParameterCode,
            i.NumericValue,
            i.StringValue,
            i.Notes)).ToList();

        var command = new UpdateLabResultItemsCommand(values, request.ClinicalNotes);
        var result = await service.UpdateItemsAsync(actor, id, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> ApproveLabResultTechnicallyAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveTechnicallyAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> ApproveLabResultClinicallyAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveClinicallyAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> CorrectLabResultAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CorrectLabResultRequest request,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CorrectionReason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["correctionReason"] = ["Sonuç düzeltmesi için zorunlu gerekçe girilmelidir."],
            });
        }

        var values = request.CorrectedItems.Select(i => new ParameterValueCommand(
            i.ParameterCode,
            i.NumericValue,
            i.StringValue,
            i.Notes)).ToList();

        var command = new CorrectLabResultCommand(request.CorrectionReason, values);
        var result = await service.CorrectAsync(actor, id, command, cancellationToken);
        return ToHttpResult(result, r => Results.Created($"/api/v1/diagnostics/lab-results/{r.Id}", MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> GetLabResultByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToLabResultDetailResponse(r)));
    }

    private static async Task<IResult> GetLabResultsByOrderIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid orderId,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByOrderIdAsync(actor, orderId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToLabResultDetailResponse).ToList()));
    }

    private static async Task<IResult> GetLabResultWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        [FromServices] ILabResultService service,
        CancellationToken cancellationToken)
    {
        LabResultStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LabResultStatus>(status, true, out var st))
        {
            parsedStatus = st;
        }

        var result = await service.GetWorklistAsync(actor, parsedStatus, patientId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(r => new LabResultSummaryResponse(
            r.Id,
            r.DiagnosticOrderId,
            r.DiagnosticOrderItemId,
            r.PatientId,
            r.CatalogCode,
            r.CatalogItemName,
            r.Status.ToString(),
            r.HasCriticalFlag,
            r.HasAbnormalFlag,
            r.CreatedAtUtc,
            r.ClinicallyApprovedAtUtc)).ToList()));
    }

    private static LabResultDetailResponse MapToLabResultDetailResponse(LabResultDetailDto r) =>
        new(
            r.Id,
            r.DiagnosticOrderId,
            r.DiagnosticOrderItemId,
            r.SpecimenId,
            r.PatientId,
            r.CatalogCode,
            r.CatalogItemName,
            r.Status.ToString(),
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
            r.Items.Select(i => new LabResultItemResponse(
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
                i.Flag.ToString(),
                i.Notes)).ToList());

    private static async Task<IResult> GetActiveCriticalNotificationsAsync(
        ClaimsPrincipal actor,
        [FromQuery] Guid? patientId,
        [FromServices] ICriticalResultNotificationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetActiveNotificationsAsync(actor, patientId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToCriticalNotificationResponse).ToList()));
    }

    private static async Task<IResult> GetCriticalNotificationByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] ICriticalResultNotificationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToCriticalNotificationResponse(r)));
    }

    private static async Task<IResult> AcknowledgeCriticalNotificationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] AcknowledgeCriticalResultRequest request,
        [FromServices] ICriticalResultNotificationService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.AcknowledgmentNotes))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["acknowledgmentNotes"] = ["Kritik değer teslim teyidi ve alındı notu zorunludur."],
            });
        }

        var result = await service.AcknowledgeAsync(actor, id, request.AcknowledgmentNotes, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToCriticalNotificationResponse(r)));
    }

    private static async Task<IResult> EscalateCriticalNotificationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] EscalateCriticalResultRequest request,
        [FromServices] ICriticalResultNotificationService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.EscalationReason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["escalationReason"] = ["Eskalasyon gerekçesi zorunludur."],
            });
        }

        var result = await service.EscalateAsync(actor, id, request.EscalationReason, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToCriticalNotificationResponse(r)));
    }

    private static CriticalResultNotificationResponse MapToCriticalNotificationResponse(CriticalResultNotificationDto n) =>
        new(
            n.Id,
            n.LabResultId,
            n.DiagnosticOrderId,
            n.DiagnosticOrderItemId,
            n.PatientId,
            n.ParameterCode,
            n.ParameterName,
            n.NumericValue,
            n.StringValue,
            n.Unit,
            n.Flag.ToString(),
            n.Status.ToString(),
            n.EscalationLevel,
            n.ResponsibleDoctorUserId,
            n.AcknowledgedByUserId,
            n.AcknowledgedAtUtc,
            n.AcknowledgmentNotes,
            n.EscalatedAtUtc,
            n.EscalationReason,
            n.CreatedAtUtc);

    private static async Task<IResult> GetRadiologyCatalogAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? modality,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        RadiologyModality? parsedModality = null;
        if (!string.IsNullOrWhiteSpace(modality) && Enum.TryParse<RadiologyModality>(modality, true, out var mod))
        {
            parsedModality = mod;
        }

        var result = await service.GetCatalogItemsAsync(actor, parsedModality, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToRadiologyCatalogResponse).ToList()));
    }

    private static async Task<IResult> GetRadiologyWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? modality,
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        RadiologyModality? parsedModality = null;
        if (!string.IsNullOrWhiteSpace(modality) && Enum.TryParse<RadiologyModality>(modality, true, out var mod))
        {
            parsedModality = mod;
        }

        RadiologyStudyStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RadiologyStudyStatus>(status, true, out var st))
        {
            parsedStatus = st;
        }

        var result = await service.GetWorklistAsync(actor, parsedModality, parsedStatus, patientId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToRadiologySummaryResponse).ToList()));
    }

    private static async Task<IResult> GetRadiologyStudyByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> EnsureRadiologyStudyAsync(
        ClaimsPrincipal actor,
        [FromQuery] Guid orderId,
        [FromQuery] Guid orderItemId,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.EnsureStudyForOrderItemAsync(actor, orderId, orderItemId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> ScheduleRadiologyStudyAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] ScheduleRadiologyStudyRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ScheduleAsync(actor, id, request.ScheduledAtUtc, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> CompleteRadiologyAcquisitionAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CompleteAcquisitionRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAcquisitionAsync(actor, id, request?.TechnicianNotes, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> DraftRadiologyReportAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] DraftRadiologyReportRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ReportText))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reportText"] = ["Rapor metni zorunludur."],
            });
        }

        var result = await service.DraftReportAsync(actor, id, request.ReportText, request.Impression, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> FinalizeRadiologyReportAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] FinalizeRadiologyReportRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ReportText) || string.IsNullOrWhiteSpace(request.Impression))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reportText"] = ["Rapor metni ve kanaat (Impression) zorunludur."],
            });
        }

        var result = await service.FinalizeReportAsync(actor, id, request.ReportText, request.Impression, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> AddRadiologyAddendumAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] AddRadiologyAddendumRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.AddendumText))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["addendumText"] = ["Ek rapor metni zorunludur."],
            });
        }

        var result = await service.AddAddendumAsync(actor, id, request.AddendumText, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static async Task<IResult> CancelRadiologyStudyAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelRadiologyStudyRequest request,
        [FromServices] IRadiologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["İptal gerekçesi zorunludur."],
            });
        }

        var result = await service.CancelAsync(actor, id, request.Reason, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToRadiologyDetailResponse(r)));
    }

    private static RadiologyCatalogItemResponse MapToRadiologyCatalogResponse(RadiologyCatalogItemDto c) =>
        new(
            c.Id,
            c.Code,
            c.Name,
            c.Modality.ToString(),
            c.BodySite,
            c.Description,
            c.PreparationInstructions,
            c.ContrastRequired,
            c.EstimatedDurationMinutes,
            c.IsActive);

    private static RadiologyStudyDetailResponse MapToRadiologyDetailResponse(RadiologyStudyDetailDto s) =>
        new(
            s.Id,
            s.DiagnosticOrderId,
            s.DiagnosticOrderItemId,
            s.PatientId,
            s.AccessionNumber,
            s.Modality.ToString(),
            s.ProcedureCode,
            s.ProcedureName,
            s.BodySite,
            s.Status.ToString(),
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

    private static RadiologyStudySummaryResponse MapToRadiologySummaryResponse(RadiologyStudySummaryDto s) =>
        new(
            s.Id,
            s.DiagnosticOrderId,
            s.PatientId,
            s.AccessionNumber,
            s.Modality.ToString(),
            s.ProcedureCode,
            s.ProcedureName,
            s.BodySite,
            s.Status.ToString(),
            s.ScheduledAtUtc,
            s.PerformedAtUtc,
            s.ReportFinalizedAtUtc,
            s.CreatedAtUtc);

    private static async Task<IResult> GetDicomStudyMetadataAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IDicomSimulationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetStudyMetadataAsync(actor, id, cancellationToken);
        return ToHttpResult(result, m => Results.Ok(MapToDicomStudyResponse(m)));
    }

    private static async Task<IResult> GenerateDicomPreviewTokenAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] GenerateDicomPreviewTokenRequest request,
        [FromServices] IDicomSimulationService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.SopInstanceUid))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sopInstanceUid"] = ["SOP Instance UID zorunludur."],
            });
        }

        var result = await service.GeneratePreviewTokenAsync(actor, id, request.SopInstanceUid, cancellationToken);
        return ToHttpResult(result, t => Results.Ok(new DicomPreviewTokenResponse(t.Token, t.ExpiresAtUtc)));
    }

    private static async Task<IResult> GetDicomPreviewImageAsync(
        ClaimsPrincipal actor,
        [FromBody] DicomPreviewImageRequest request,
        [FromServices] IDicomSimulationService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["token"] = ["Önizleme belirteci (token) zorunludur."],
            });
        }

        var result = await service.RenderPreviewImageAsync(actor, request.Token, cancellationToken);
        return ToHttpResult(result, img => Results.Bytes(img.Content, img.ContentType));
    }

    private static DicomStudyMetadataResponse MapToDicomStudyResponse(DicomStudyMetadataDto m) =>
        new(
            m.StudyId,
            m.AccessionNumber,
            m.StudyInstanceUid,
            m.StudyDateUtc,
            m.Modality,
            m.StudyDescription,
            m.PatientId,
            m.IsMockSimulation,
            m.Series.Select(s => new DicomSeriesResponse(
                s.SeriesInstanceUid,
                s.SeriesNumber,
                s.Modality,
                s.SeriesDescription,
                s.NumberOfInstances,
                s.Instances.Select(i => new DicomInstanceResponse(
                    i.SopInstanceUid,
                    i.InstanceNumber,
                    i.Modality,
                    i.ContentType,
                    i.FileSizeBytes,
                    i.ViewToken)).ToList())).ToList());

    private static async Task<IResult> GetPathologyWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        PathologyCaseStatus? parsedStatus = Enum.TryParse<PathologyCaseStatus>(status, true, out var st) ? st : null;
        var result = await service.GetWorklistAsync(actor, parsedStatus, patientId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToPathologySummaryResponse).ToList()));
    }

    private static async Task<IResult> GetPathologyCaseByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> EnsurePathologyCaseAsync(
        ClaimsPrincipal actor,
        [FromQuery] Guid orderId,
        [FromQuery] Guid orderItemId,
        [FromQuery] string? specimenType,
        [FromQuery] string? anatomicSite,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        var type = Enum.TryParse<PathologySpecimenType>(specimenType, true, out var t) ? t : PathologySpecimenType.Biopsy;
        var site = anatomicSite ?? "Doku / Biyopsi Materyali";

        var result = await service.EnsureCaseForOrderItemAsync(actor, orderId, orderItemId, type, site, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> ReceivePathologySpecimenAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] ReceivePathologySpecimenRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FixativeUsed))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fixativeUsed"] = ["Tespit solüsyonu / fiksatif bilgisi zorunludur."],
            });
        }

        var result = await service.ReceiveSpecimenAsync(actor, id, request.FixativeUsed, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> RecordPathologyGrossExamAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] RecordGrossExamRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.GrossDescription))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["grossDescription"] = ["Makroskopi inceleme bulguları zorunludur."],
            });
        }

        var result = await service.RecordGrossExamAsync(actor, id, request.GrossDescription, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> RecordPathologyMicroscopicExamAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] RecordMicroscopicExamRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.MicroscopicDescription))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["microscopicDescription"] = ["Mikroskopi inceleme bulguları zorunludur."],
            });
        }

        var result = await service.RecordMicroscopicExamAsync(actor, id, request.MicroscopicDescription, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> DraftPathologyReportAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] DraftPathologyReportRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PathologicalDiagnosis))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pathologicalDiagnosis"] = ["Patolojik tanı metni zorunludur."],
            });
        }

        var result = await service.DraftReportAsync(actor, id, request.PathologicalDiagnosis, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> FinalizePathologyReportAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] FinalizePathologyReportRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PathologicalDiagnosis))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pathologicalDiagnosis"] = ["Patolojik tanı metni zorunludur."],
            });
        }

        var result = await service.FinalizeReportAsync(actor, id, request.PathologicalDiagnosis, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> CorrectPathologyReportAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CorrectPathologyReportRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CorrectionReason) || string.IsNullOrWhiteSpace(request.NewDiagnosis))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["correction"] = ["Düzeltme gerekçesi ve yeni patolojik tanı metni zorunludur."],
            });
        }

        var result = await service.CorrectReportAsync(actor, id, request.CorrectionReason, request.NewDiagnosis, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static async Task<IResult> CancelPathologyCaseAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelPathologyCaseRequest request,
        [FromServices] IPathologyService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["İptal gerekçesi zorunludur."],
            });
        }

        var result = await service.CancelAsync(actor, id, request.Reason, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToPathologyDetailResponse(c)));
    }

    private static PathologyCaseDetailResponse MapToPathologyDetailResponse(PathologyCaseDetailDto c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.DiagnosticOrderItemId,
            c.PatientId,
            c.PathologyNumber,
            c.SpecimenType.ToString(),
            c.AnatomicSite,
            c.ClinicalHistoryAndDiagnosis,
            c.FixativeUsed,
            c.Status.ToString(),
            c.ReceivedAtUtc,
            c.ReceivedByUserId,
            c.GrossDescription,
            c.GrossExamAtUtc,
            c.GrossExamByUserId,
            c.MicroscopicDescription,
            c.MicroscopicExamAtUtc,
            c.MicroscopicExamByUserId,
            c.PathologicalDiagnosis,
            c.ReportDraftedAtUtc,
            c.ReportFinalizedAtUtc,
            c.PathologistUserId,
            c.CorrectionReason,
            c.PreviousCaseId,
            c.CancellationReason,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.Version);

    private static PathologyCaseSummaryResponse MapToPathologySummaryResponse(PathologyCaseSummaryDto c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.PatientId,
            c.PathologyNumber,
            c.SpecimenType.ToString(),
            c.AnatomicSite,
            c.Status.ToString(),
            c.ReceivedAtUtc,
            c.ReportFinalizedAtUtc,
            c.CreatedAtUtc);

    private static async Task<IResult> GetBloodInventoryAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        string? productType,
        string? bloodGroup,
        string? status,
        CancellationToken cancellationToken)
    {
        BloodProductType? parsedProductType = Enum.TryParse<BloodProductType>(productType, true, out var pt) ? pt : null;
        BloodGroup? parsedBloodGroup = Enum.TryParse<BloodGroup>(bloodGroup, true, out var bg) ? bg : null;
        BloodUnitStatus? parsedStatus = Enum.TryParse<BloodUnitStatus>(status, true, out var st) ? st : null;

        var result = await service.GetInventoryAsync(actor, parsedProductType, parsedBloodGroup, parsedStatus, cancellationToken);
        return ToHttpResult(result, units => Results.Ok(units.Select(MapToBloodUnitResponse).ToList()));
    }

    private static async Task<IResult> GetBloodInventorySummaryAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetInventorySummaryAsync(actor, cancellationToken);
        return ToHttpResult(result, s => Results.Ok(MapToBloodInventorySummaryResponse(s)));
    }

    private static async Task<IResult> GetCrossmatchWorklistAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        string? status,
        Guid? patientId,
        CancellationToken cancellationToken)
    {
        CrossmatchStatus? parsedStatus = Enum.TryParse<CrossmatchStatus>(status, true, out var st) ? st : null;
        var result = await service.GetCrossmatchWorklistAsync(actor, parsedStatus, patientId, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToCrossmatchSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetCrossmatchByIdAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetCrossmatchByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, c => Results.Ok(MapToCrossmatchDetailResponse(c)));
    }

    private static async Task<IResult> CreateCrossmatchRequestAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        CreateCrossmatchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<BloodGroup>(request.PatientBloodGroup, true, out var patientBloodGroup))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["patientBloodGroup"] = ["Geçerli bir kan grubu belirtilmelidir (ör. APositive, ONegative)."],
            });
        }

        if (!Enum.TryParse<BloodProductType>(request.RequestedProductType, true, out var requestedProductType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["requestedProductType"] = ["Geçerli bir kan ürünü tipi belirtilmelidir (ör. RedBloodCells, FreshFrozenPlasma)."],
            });
        }

        var result = await service.CreateCrossmatchRequestAsync(
            actor,
            request.DiagnosticOrderId,
            request.DiagnosticOrderItemId,
            patientBloodGroup,
            requestedProductType,
            request.UnitsRequested,
            request.RequiredByUtc,
            cancellationToken);

        return ToHttpResult(result, c => Results.Created($"/api/v1/diagnostics/blood-bank/crossmatch/{c.Id}", MapToCrossmatchDetailResponse(c)));
    }

    private static async Task<IResult> PerformCrossmatchTestAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        Guid id,
        PerformCrossmatchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.BloodUnitId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bloodUnitId"] = ["Test edilecek kan ünitesi seçilmelidir."],
            });
        }

        var result = await service.PerformCrossmatchTestAsync(
            actor,
            id,
            request.BloodUnitId,
            request.TechnicianNotes,
            cancellationToken);

        return ToHttpResult(result, c => Results.Ok(MapToCrossmatchDetailResponse(c)));
    }

    private static async Task<IResult> IssueBloodUnitAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.IssueBloodUnitAsync(actor, id, cancellationToken);
        return ToHttpResult(result, u => Results.Ok(MapToBloodUnitResponse(u)));
    }

    private static async Task<IResult> RecordTransfusionAsync(
        ClaimsPrincipal actor,
        IBloodBankService service,
        Guid id,
        RecordTransfusionRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await service.RecordTransfusionAsync(actor, id, request?.TransfusionNotes, cancellationToken);
        return ToHttpResult(result, u => Results.Ok(MapToBloodUnitResponse(u)));
    }

    private static BloodUnitResponse MapToBloodUnitResponse(BloodUnitDto u) =>
        new(
            u.Id,
            u.UnitNumber,
            u.ProductType.ToString(),
            u.BloodGroup.ToString(),
            u.VolumeMl,
            u.DonationDateUtc,
            u.ExpiryDateUtc,
            u.StorageLocation,
            u.Status.ToString(),
            u.ReservedForPatientId,
            u.ReservedUntilUtc,
            u.CreatedAtUtc,
            u.UpdatedAtUtc,
            u.Version);

    private static CrossmatchDetailResponse MapToCrossmatchDetailResponse(CrossmatchDetailDto c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.DiagnosticOrderItemId,
            c.PatientId,
            c.PatientBloodGroup.ToString(),
            c.RequestedProductType.ToString(),
            c.UnitsRequested,
            c.RequiredByUtc,
            c.Status.ToString(),
            c.CompatibilityResult.ToString(),
            c.TechnicianNotes,
            c.TestedAtUtc,
            c.TestedByUserId,
            c.AllocatedBloodUnitId,
            c.CancellationReason,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.Version);

    private static CrossmatchSummaryResponse MapToCrossmatchSummaryResponse(CrossmatchSummaryDto c) =>
        new(
            c.Id,
            c.DiagnosticOrderId,
            c.PatientId,
            c.PatientBloodGroup.ToString(),
            c.RequestedProductType.ToString(),
            c.UnitsRequested,
            c.Status.ToString(),
            c.CompatibilityResult.ToString(),
            c.AllocatedBloodUnitId,
            c.CreatedAtUtc);

    private static BloodInventorySummaryResponse MapToBloodInventorySummaryResponse(BloodInventorySummaryDto s) =>
        new(
            s.TotalUnits,
            s.AvailableUnits,
            s.ReservedUnits,
            s.IssuedUnits,
            s.TransfusedUnits,
            s.DiscardedUnits,
            s.UnitsByGroup,
            s.UnitsByType);

    private static async Task<IResult> GetPatientDiagnosticTimelineAsync(
        ClaimsPrincipal actor,
        IDiagnosticTimelineService service,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientTimelineAsync(actor, patientId, cancellationToken);
        return ToHttpResult(result, t => Results.Ok(MapToPatientDiagnosticTimelineResponse(t)));
    }

    private static async Task<IResult> GetMyPatientPortalResultsAsync(
        ClaimsPrincipal actor,
        IDiagnosticTimelineService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientPortalResultsAsync(actor, cancellationToken);
        return ToHttpResult(result, list => Results.Ok(list.Select(MapToPatientPortalResultSummaryResponse).ToList()));
    }

    private static PatientDiagnosticTimelineResponse MapToPatientDiagnosticTimelineResponse(PatientDiagnosticTimelineDto t) =>
        new(
            t.PatientId,
            t.TotalEntries,
            t.Entries.Select(MapToTimelineEntryResponse).ToList());

    private static TimelineEntryResponse MapToTimelineEntryResponse(TimelineEntryDto e) =>
        new(
            e.Id,
            e.Category,
            e.Title,
            e.Status,
            e.EventDateUtc,
            e.PerformedBy,
            e.HasCriticalFlag,
            e.SummaryText,
            e.ReferenceId,
            e.Parameters?.Select(MapToTimelineParameterResponse).ToList());

    private static TimelineParameterResponse MapToTimelineParameterResponse(TimelineParameterDto p) =>
        new(
            p.Name,
            p.Value,
            p.Unit,
            p.ReferenceRange,
            p.Interpretation,
            p.IsCritical);

    private static PatientPortalResultSummaryResponse MapToPatientPortalResultSummaryResponse(PatientPortalResultSummaryDto r) =>
        new(
            r.Id,
            r.Category,
            r.TestOrStudyName,
            r.Status,
            r.ResultDateUtc,
            r.DoctorOrDepartment,
            r.IsPendingDoctorReview,
            r.FinalReportDiagnosis,
            r.Parameters?.Select(MapToTimelineParameterResponse).ToList());
}
