using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Inpatient;

public static class InpatientEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInpatientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var inpatientGroup = endpoints.MapGroup("/api/v1/inpatient")
            .RequireAuthorization();

        var wardGroup = inpatientGroup.MapGroup("/wards");
        wardGroup.MapGet(string.Empty, GetWardsAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetWards")
            .Produces<List<WardResponse>>();

        wardGroup.MapGet("/transfer-destinations", GetTransferDestinationWardsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedTransfer)
            .WithName("GetInpatientTransferDestinationWards")
            .Produces<List<WardResponse>>();

        wardGroup.MapGet("/{id:guid}", GetWardByIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetWardById")
            .Produces<WardResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        wardGroup.MapGet("/{wardId:guid}/rooms", GetRoomsByWardIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetRoomsByWardId")
            .Produces<List<RoomResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var bedGroup = inpatientGroup.MapGroup("/beds");
        bedGroup.MapGet(string.Empty, GetBedsAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetBeds")
            .Produces<List<BedResponse>>();

        bedGroup.MapGet("/{id:guid}", GetBedByIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetBedById")
            .Produces<BedResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bedGroup.MapPost("/{id:guid}/status", UpdateBedStatusAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedAssign)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdateBedStatus")
            .Produces<BedResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var admissionGroup = inpatientGroup.MapGroup("/admissions");
        admissionGroup.MapPost(string.Empty, RequestAdmissionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.AdmissionRequest)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RequestAdmission")
            .Produces<AdmissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionGroup.MapPost("/{id:guid}/accept", AcceptAdmissionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.AdmissionAccept)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AcceptAdmission")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionGroup.MapPost("/{id:guid}/admit", AdmitPatientAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedAssign)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AdmitPatient")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionGroup.MapPost("/{id:guid}/cancel", CancelAdmissionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.AdmissionRequest)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelAdmission")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionGroup.MapPost("/{id:guid}/care-details", UpdateCareDetailsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdateCareDetails")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionGroup.MapGet(string.Empty, GetAdmissionsAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetAdmissions")
            .Produces<List<AdmissionSummaryResponse>>();

        admissionGroup.MapGet("/{id:guid}", GetAdmissionByIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetAdmissionById")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admissionGroup.MapGet("/active/by-patient/{patientId:guid}", GetActiveAdmissionByPatientIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetActiveAdmissionByPatientId")
            .Produces<AdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var transferGroup = inpatientGroup.MapGroup("/transfers");
        transferGroup.MapPost(string.Empty, RequestTransferAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedTransfer)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RequestTransfer")
            .Produces<TransferResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transferGroup.MapPost("/{id:guid}/accept", AcceptTransferAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedTransfer)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AcceptTransfer")
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transferGroup.MapPost("/{id:guid}/complete", CompleteTransferAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedTransfer)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteTransfer")
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transferGroup.MapPost("/{id:guid}/cancel", CancelTransferAsync)
            .RequirePermission(HospitalPermissions.Inpatient.BedTransfer)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelTransfer")
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        transferGroup.MapGet("/{id:guid}", GetTransferByIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetTransferById")
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        transferGroup.MapGet(string.Empty, GetTransfersAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetTransfers")
            .Produces<List<TransferSummaryResponse>>();

        var boardGroup = inpatientGroup.MapGroup("/board");
        boardGroup.MapGet(string.Empty, GetInpatientBoardAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetInpatientBoard")
            .Produces<List<InpatientBoardItemResponse>>();

        boardGroup.MapGet("/{admissionId:guid}/summary", GetPatientSummaryAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetInpatientPatientSummary")
            .Produces<InpatientPatientSummaryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var nursingGroup = inpatientGroup.MapGroup("/nursing");

        nursingGroup.MapPost("/observations", RecordObservationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordNursingObservation")
            .Produces<NursingObservationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        nursingGroup.MapPost("/observations/{id:guid}/correct", RecordObservationCorrectionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordNursingObservationCorrection")
            .Produces<NursingObservationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        nursingGroup.MapGet("/observations", GetNursingObservationsAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetNursingObservations")
            .Produces<List<NursingObservationResponse>>();

        nursingGroup.MapPost("/care-plans", CreateCarePlanAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateNursingCarePlan")
            .Produces<NursingCarePlanResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        nursingGroup.MapPost("/care-plans/{id:guid}/tasks", AddTaskToCarePlanAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AddNursingCareTask")
            .Produces<NursingCareTaskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        nursingGroup.MapPost("/tasks/{id:guid}/complete", CompleteCareTaskAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteNursingCareTask")
            .Produces<NursingCareTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        nursingGroup.MapPost("/tasks/{id:guid}/cancel", CancelCareTaskAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CarePlanManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelNursingCareTask")
            .Produces<NursingCareTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        nursingGroup.MapGet("/care-plans", GetNursingCarePlansAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetNursingCarePlans")
            .Produces<List<NursingCarePlanResponse>>();

        nursingGroup.MapGet("/tasks/overdue", GetOverdueNursingTasksAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetOverdueNursingTasks")
            .Produces<List<NursingCareTaskResponse>>();

        var emarGroup = inpatientGroup.MapGroup("/emar");

        emarGroup.MapPost("/schedule", ScheduleMedicationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ScheduleMedication")
            .Produces<MedicationAdministrationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        emarGroup.MapGet("/orders/admission/{admissionId:guid}", GetActiveMedicationOrdersAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .WithName("GetActiveInpatientMedicationOrders")
            .Produces<List<ActiveMedicationOrderResponse>>();

        emarGroup.MapPost("/{id:guid}/administer", AdministerMedicationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AdministerMedication")
            .Produces<MedicationAdministrationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        emarGroup.MapPost("/{id:guid}/skip", SkipMedicationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SkipMedication")
            .Produces<MedicationAdministrationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        emarGroup.MapPost("/{id:guid}/refuse", RefuseMedicationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RefuseMedication")
            .Produces<MedicationAdministrationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        emarGroup.MapPost("/{id:guid}/delay", DelayMedicationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.MedicationAdminister)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("DelayMedication")
            .Produces<MedicationAdministrationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        emarGroup.MapGet("/admission/{admissionId:guid}", GetMedicationsByAdmissionIdAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetMedicationsByAdmissionId")
            .Produces<List<MedicationAdministrationResponse>>();

        emarGroup.MapGet("/due", GetDueMedicationsAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetDueMedications")
            .Produces<List<MedicationAdministrationResponse>>();

        var dischargeGroup = inpatientGroup.MapGroup("/discharges");

        dischargeGroup.MapPost("", ProcessDischargeAsync)
            .RequirePermission(HospitalPermissions.Inpatient.DischargeComplete)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ProcessDischarge")
            .Produces<InpatientDischargeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        dischargeGroup.MapGet("/{admissionId:guid}", GetDischargeByAdmissionIdAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetDischargeByAdmissionId")
            .Produces<InpatientDischargeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        dischargeGroup.MapGet("", GetDischargesAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetDischarges")
            .Produces<List<InpatientDischargeResponse>>();

        var dashboardGroup = inpatientGroup.MapGroup("/dashboard");

        dashboardGroup.MapGet("", GetDashboardSummaryAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetInpatientDashboard")
            .Produces<InpatientDashboardResponse>();

        dashboardGroup.MapGet("/ward/{wardId:guid}", GetWardDashboardAsync)
            .RequireAnyPermission(ClinicalInpatientReadPermissions)
            .WithName("GetWardDashboard")
            .Produces<InpatientDashboardResponse>();

        inpatientGroup.MapGet("/occupancy-summary", GetOccupancySummaryAsync)
            .RequireAnyPermission(InpatientReadPermissions)
            .WithName("GetOccupancySummary")
            .Produces<BedOccupancySummaryResponse>();

        return endpoints;
    }

    private static readonly string[] InpatientReadPermissions =
    [
        HospitalPermissions.Patient.ViewOwn,
        HospitalPermissions.Inpatient.AdmissionRequest,
        HospitalPermissions.Inpatient.AdmissionAccept,
        HospitalPermissions.Inpatient.BedAssign,
        HospitalPermissions.Inpatient.BedTransfer,
        HospitalPermissions.Inpatient.CarePlanManage,
        HospitalPermissions.Inpatient.MedicationAdminister,
        HospitalPermissions.Inpatient.DischargeComplete,
    ];

    private static readonly string[] ClinicalInpatientReadPermissions =
    [
        HospitalPermissions.Inpatient.AdmissionRequest,
        HospitalPermissions.Inpatient.AdmissionAccept,
        HospitalPermissions.Inpatient.BedAssign,
        HospitalPermissions.Inpatient.BedTransfer,
        HospitalPermissions.Inpatient.CarePlanManage,
        HospitalPermissions.Inpatient.MedicationAdminister,
        HospitalPermissions.Inpatient.DischargeComplete,
    ];

    private static async Task<IResult> GetWardsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await service.GetWardsAsync(isActive, cancellationToken);
        var departmentIds = await accessControl.GetAccessibleDepartmentIdsAsync(
            actor,
            cancellationToken,
            InpatientReadPermissions);
        return ToHttpResult(result, wards => Results.Ok(wards
            .Where(ward => departmentIds.Contains(ward.DepartmentId))
            .Select(MapToWardResponse)
            .ToList()));
    }

    private static async Task<IResult> GetWardByIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessWardAsync(actor, id, cancellationToken, InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var result = await service.GetWardByIdAsync(id, cancellationToken);
        return ToHttpResult(result, w => Results.Ok(MapToWardResponse(w)));
    }

    private static async Task<IResult> GetTransferDestinationWardsAsync(
        IBedManagementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetWardsAsync(isActive: true, cancellationToken);
        return ToHttpResult(result, wards => Results.Ok(wards.Select(MapToWardResponse).ToList()));
    }

    private static async Task<IResult> GetRoomsByWardIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        Guid wardId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessWardAsync(actor, wardId, cancellationToken, InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var result = await service.GetRoomsByWardIdAsync(wardId, cancellationToken);
        return ToHttpResult(result, rooms => Results.Ok(rooms.Select(MapToRoomResponse).ToList()));
    }

    private static async Task<IResult> GetBedsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        [FromQuery] Guid? wardId,
        [FromQuery] Guid? roomId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        if (wardId.HasValue
            && !await accessControl.CanAccessWardAsync(actor, wardId.Value, cancellationToken, InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        BedStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BedStatus>(status, true, out var bs))
        {
            parsedStatus = bs;
        }

        var result = await service.GetBedsAsync(wardId, roomId, parsedStatus, cancellationToken);
        var wardIds = await accessControl.GetAccessibleWardIdsAsync(actor, cancellationToken, InpatientReadPermissions);
        return ToHttpResult(result, beds => Results.Ok(beds
            .Where(bed => wardIds.Contains(bed.WardId))
            .Select(MapToBedResponse)
            .ToList()));
    }

    private static async Task<IResult> GetBedByIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessBedAsync(actor, id, cancellationToken, InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var result = await service.GetBedByIdAsync(id, cancellationToken);
        return ToHttpResult(result, b => Results.Ok(MapToBedResponse(b)));
    }

    private static async Task<IResult> UpdateBedStatusAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        Guid id,
        UpdateBedStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessBedAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.BedAssign))
        {
            return ForbiddenResource();
        }

        if (!Enum.TryParse<BedStatus>(request.NewStatus, true, out var newStatus))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["NewStatus"] = [$"'{request.NewStatus}' geçerli bir yatak durumu değildir."],
            });
        }

        var result = await service.UpdateBedStatusAsync(actor, id, newStatus, request.Reason, cancellationToken);
        return ToHttpResult(result, b => Results.Ok(MapToBedResponse(b)));
    }

    private static async Task<IResult> GetOccupancySummaryAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IBedManagementService service,
        [FromQuery] Guid? wardId,
        CancellationToken cancellationToken)
    {
        if (wardId.HasValue)
        {
            if (!await accessControl.CanAccessWardAsync(actor, wardId.Value, cancellationToken, InpatientReadPermissions))
            {
                return ForbiddenResource();
            }

            var scopedResult = await service.GetOccupancySummaryAsync(wardId, cancellationToken);
            return ToHttpResult(scopedResult, MapOccupancySummary);
        }

        var accessibleWardIds = await accessControl.GetAccessibleWardIdsAsync(
            actor,
            cancellationToken,
            InpatientReadPermissions);
        var summaries = new List<BedOccupancySummaryDto>();
        foreach (var accessibleWardId in accessibleWardIds)
        {
            var result = await service.GetOccupancySummaryAsync(accessibleWardId, cancellationToken);
            if (result.Status == InpatientOperationStatus.Success && result.Value is not null)
            {
                summaries.Add(result.Value);
            }
        }

        var total = summaries.Sum(summary => summary.TotalBeds);
        var occupied = summaries.Sum(summary => summary.OccupiedBeds);
        return Results.Ok(new BedOccupancySummaryResponse(
            total,
            summaries.Sum(summary => summary.AvailableBeds),
            occupied,
            summaries.Sum(summary => summary.CleaningBeds),
            summaries.Sum(summary => summary.MaintenanceBeds),
            summaries.Sum(summary => summary.ReservedBeds),
            total == 0 ? 0 : Math.Round((double)occupied / total * 100, 2)));
    }

    private static async Task<IResult> RequestAdmissionAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        CreateAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanRequestAdmissionAsync(
                actor,
                request.PatientId,
                request.DepartmentId,
                request.AttendingDoctorId,
                cancellationToken)
            || !await accessControl.CanAccessWardAsync(
                actor,
                request.AdmittingWardId,
                cancellationToken,
                HospitalPermissions.Inpatient.AdmissionRequest))
        {
            return ForbiddenResource();
        }

        var orderingDoctorId = ExtractActorId(actor);
        var isolation = IsolationType.None;
        if (!string.IsNullOrWhiteSpace(request.IsolationRequired) &&
            Enum.TryParse<IsolationType>(request.IsolationRequired, true, out var parsedIsolation))
        {
            isolation = parsedIsolation;
        }

        var dto = new CreateAdmissionDto(
            request.PatientId,
            request.EncounterId,
            request.DepartmentId,
            request.AdmittingWardId,
            request.AttendingDoctorId,
            request.AdmissionReason,
            request.DiagnosisCode,
            request.DiagnosisDescription,
            request.DietType,
            request.FallRiskScore,
            isolation,
            request.EstimatedStayDays,
            request.InitialBedId);

        var result = await service.RequestAdmissionAsync(dto, orderingDoctorId, cancellationToken);
        return ToHttpResult(result, admission => Results.Created($"/api/v1/inpatient/admissions/{admission.Id}", MapToAdmissionResponse(admission)));
    }

    private static async Task<IResult> AcceptAdmissionAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid id,
        AcceptAdmissionRequest? request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.AdmissionAccept))
        {
            return ForbiddenResource();
        }

        var userId = ExtractActorId(actor);
        var dto = request is not null ? new AcceptAdmissionDto(request.Notes) : null;
        var result = await service.AcceptAdmissionAsync(id, userId, dto, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToAdmissionResponse(admission)));
    }

    private static async Task<IResult> AdmitPatientAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid id,
        AdmitPatientRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.BedAssign)
            || !await accessControl.CanAccessBedAsync(
                actor,
                request.BedId,
                cancellationToken,
                HospitalPermissions.Inpatient.BedAssign))
        {
            return ForbiddenResource();
        }

        var userId = ExtractActorId(actor);
        var dto = new AdmitPatientDto(request.BedId);
        var result = await service.AdmitPatientAsync(id, dto, userId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToAdmissionResponse(admission)));
    }

    private static async Task<IResult> CancelAdmissionAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid id,
        CancelAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.AdmissionRequest))
        {
            return ForbiddenResource();
        }

        var userId = ExtractActorId(actor);
        var dto = new CancelAdmissionDto(request.Reason);
        var result = await service.CancelAdmissionAsync(id, dto, userId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToAdmissionResponse(admission)));
    }

    private static async Task<IResult> UpdateCareDetailsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid id,
        UpdateCareDetailsRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage)
            || !await accessControl.IsPersonAssignedToAdmissionDepartmentAsync(
                request.AttendingDoctorId,
                id,
                cancellationToken))
        {
            return ForbiddenResource();
        }

        var userId = ExtractActorId(actor);
        var isolation = IsolationType.None;
        if (!string.IsNullOrWhiteSpace(request.IsolationRequired) &&
            Enum.TryParse<IsolationType>(request.IsolationRequired, true, out var parsedIsolation))
        {
            isolation = parsedIsolation;
        }

        var dto = new UpdateCareDetailsDto(
            request.AttendingDoctorId,
            request.DietType,
            request.FallRiskScore,
            isolation);

        var result = await service.UpdateCareDetailsAsync(id, dto, userId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToAdmissionResponse(admission)));
    }

    private static Guid ExtractActorId(ClaimsPrincipal actor)
    {
        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var personId) && personId != Guid.Empty)
        {
            return personId;
        }

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    private static async Task<IResult> GetAdmissionByIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                id,
                true,
                cancellationToken,
                InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var admission = await service.GetAdmissionByIdAsync(id, cancellationToken);
        return admission is not null ? Results.Ok(MapToAdmissionResponse(admission)) : Results.NotFound();
    }

    private static async Task<IResult> GetActiveAdmissionByPatientIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAdmissionAsync(
                actor,
                patientId,
                true,
                cancellationToken,
                InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var admission = await service.GetActiveAdmissionByPatientIdAsync(patientId, cancellationToken);
        return admission is not null ? Results.Ok(MapToAdmissionResponse(admission)) : Results.NotFound();
    }

    private static async Task<IResult> GetAdmissionsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientAdmissionService service,
        [FromQuery] Guid? wardId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        var admissions = await service.GetAdmissionsAsync(wardId, departmentId, status, patientId, cancellationToken);
        var visible = new List<AdmissionSummaryResponse>();
        foreach (var admission in admissions)
        {
            if (await accessControl.CanAccessAdmissionAsync(
                    actor,
                    admission.Id,
                    true,
                    cancellationToken,
                    InpatientReadPermissions))
            {
                visible.Add(MapToAdmissionSummaryResponse(admission));
            }
        }

        return Results.Ok(visible);
    }

    private static AdmissionResponse MapToAdmissionResponse(InpatientAdmissionDto a) =>
        new(
            a.Id,
            a.AdmissionNumber,
            a.PatientId,
            a.EncounterId,
            a.OrderingDoctorId,
            a.AttendingDoctorId,
            a.DepartmentId,
            a.AdmittingWardId,
            a.WardName,
            a.AssignedBedId,
            a.BedNumber,
            a.RoomNumber,
            a.Status.ToString(),
            a.AdmissionReason,
            a.DiagnosisCode,
            a.DiagnosisDescription,
            a.DietType,
            a.FallRiskScore,
            a.IsolationRequired.ToString(),
            a.EstimatedStayDays,
            a.RequestedAtUtc,
            a.AcceptedAtUtc,
            a.AdmittedAtUtc,
            a.DischargedAtUtc,
            a.DischargeSummary,
            a.CancelledAtUtc,
            a.CancellationReason,
            a.Version);

    private static AdmissionSummaryResponse MapToAdmissionSummaryResponse(InpatientAdmissionSummaryDto a) =>
        new(
            a.Id,
            a.AdmissionNumber,
            a.PatientId,
            a.OrderingDoctorId,
            a.AttendingDoctorId,
            a.DepartmentId,
            a.AdmittingWardId,
            a.WardName,
            a.AssignedBedId,
            a.BedNumber,
            a.RoomNumber,
            a.Status.ToString(),
            a.AdmissionReason,
            a.DietType,
            a.FallRiskScore,
            a.IsolationRequired.ToString(),
            a.RequestedAtUtc,
            a.AdmittedAtUtc);

    private static async Task<IResult> RequestTransferAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                request.AdmissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.BedTransfer))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CreateTransferDto(
            request.AdmissionId,
            request.TargetWardId,
            request.TargetBedId,
            request.TransferReason,
            request.ClinicalNotes);

        var result = await service.RequestTransferAsync(dto, actorId, cancellationToken);
        return ToHttpResult(result, transfer => Results.Created($"/api/v1/inpatient/transfers/{transfer.Id}", MapToTransferResponse(transfer)));
    }

    private static async Task<IResult> AcceptTransferAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        Guid id,
        AcceptTransferRequest? request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessTransferTargetWardAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.BedTransfer))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = request is not null ? new AcceptTransferDto(request.TargetBedId) : null;
        var result = await service.AcceptTransferAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, transfer => Results.Ok(MapToTransferResponse(transfer)));
    }

    private static async Task<IResult> CompleteTransferAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        Guid id,
        CompleteTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessTransferAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.BedTransfer)
            || !await accessControl.CanAccessBedAsync(
                actor,
                request.TargetBedId,
                cancellationToken,
                HospitalPermissions.Inpatient.BedTransfer))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CompleteTransferDto(request.TargetBedId);
        var result = await service.CompleteTransferAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, transfer => Results.Ok(MapToTransferResponse(transfer)));
    }

    private static async Task<IResult> CancelTransferAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        Guid id,
        CancelTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessTransferAsync(
                actor,
                id,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.BedTransfer))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CancelTransferDto(request.Reason);
        var result = await service.CancelTransferAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, transfer => Results.Ok(MapToTransferResponse(transfer)));
    }

    private static async Task<IResult> GetTransferByIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessTransferAsync(
                actor,
                id,
                true,
                cancellationToken,
                InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var transfer = await service.GetTransferByIdAsync(id, cancellationToken);
        return transfer is not null ? Results.Ok(MapToTransferResponse(transfer)) : Results.NotFound();
    }

    private static async Task<IResult> GetTransfersAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientTransferService service,
        [FromQuery] Guid? admissionId,
        [FromQuery] Guid? sourceWardId,
        [FromQuery] Guid? targetWardId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var transfers = await service.GetTransfersAsync(admissionId, sourceWardId, targetWardId, status, cancellationToken);
        var visible = new List<TransferSummaryResponse>();
        foreach (var transfer in transfers)
        {
            if (await accessControl.CanAccessTransferAsync(
                    actor,
                    transfer.Id,
                    true,
                    cancellationToken,
                    InpatientReadPermissions))
            {
                visible.Add(MapToTransferSummaryResponse(transfer));
            }
        }

        return Results.Ok(visible);
    }

    private static TransferResponse MapToTransferResponse(InpatientTransferDto t) =>
        new(
            t.Id,
            t.AdmissionId,
            t.PatientId,
            t.SourceWardId,
            t.SourceWardName,
            t.SourceBedId,
            t.SourceBedNumber,
            t.SourceRoomNumber,
            t.TargetWardId,
            t.TargetWardName,
            t.TargetBedId,
            t.TargetBedNumber,
            t.TargetRoomNumber,
            t.TransferReason,
            t.ClinicalNotes,
            t.Status.ToString(),
            t.RequestedByUserId,
            t.RequestedAtUtc,
            t.AcceptedByUserId,
            t.AcceptedAtUtc,
            t.CompletedByUserId,
            t.CompletedAtUtc,
            t.CancelledByUserId,
            t.CancelledAtUtc,
            t.CancellationReason,
            t.Version);

    private static TransferSummaryResponse MapToTransferSummaryResponse(InpatientTransferSummaryDto t) =>
        new(
            t.Id,
            t.AdmissionId,
            t.PatientId,
            t.SourceWardId,
            t.SourceWardName,
            t.SourceBedId,
            t.SourceBedNumber,
            t.TargetWardId,
            t.TargetWardName,
            t.TargetBedId,
            t.TargetBedNumber,
            t.TransferReason,
            t.Status.ToString(),
            t.RequestedAtUtc,
            t.CompletedAtUtc);

    private static async Task<IResult> RecordObservationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        RecordObservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                request.AdmissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var consciousness = Enum.TryParse<ConsciousnessLevel>(request.Consciousness, true, out var cl) ? cl : ConsciousnessLevel.Alert;
        var dto = new RecordObservationDto(
            request.AdmissionId,
            request.ObservedAtUtc,
            request.SystolicBp,
            request.DiastolicBp,
            request.HeartRate,
            request.RespiratoryRate,
            request.BodyTemperatureCelsius,
            request.OxygenSaturationPercent,
            request.PainScale,
            request.OralIntakeMl,
            request.IvIntakeMl,
            request.UrineOutputMl,
            request.DrainOutputMl,
            request.OtherOutputMl,
            consciousness,
            request.ClinicalNotes);

        var result = await service.RecordObservationAsync(dto, actorId, cancellationToken);
        return ToHttpResult(result, obs => Results.Created($"/api/v1/inpatient/nursing/observations/{obs.Id}", MapToObservationResponse(obs)));
    }

    private static async Task<IResult> RecordObservationCorrectionAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        Guid id,
        CorrectObservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessObservationAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var consciousness = Enum.TryParse<ConsciousnessLevel>(request.Consciousness, true, out var cl) ? cl : ConsciousnessLevel.Alert;
        var dto = new CorrectObservationDto(
            request.CorrectionReason,
            request.SystolicBp,
            request.DiastolicBp,
            request.HeartRate,
            request.RespiratoryRate,
            request.BodyTemperatureCelsius,
            request.OxygenSaturationPercent,
            request.PainScale,
            request.OralIntakeMl,
            request.IvIntakeMl,
            request.UrineOutputMl,
            request.DrainOutputMl,
            request.OtherOutputMl,
            consciousness,
            request.ClinicalNotes);

        var result = await service.RecordObservationCorrectionAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, obs => Results.Ok(MapToObservationResponse(obs)));
    }

    private static async Task<IResult> GetNursingObservationsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        [FromQuery] Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                false,
                cancellationToken,
                ClinicalInpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var observations = await service.GetObservationsByAdmissionIdAsync(admissionId, cancellationToken);
        return Results.Ok(observations.Select(MapToObservationResponse).ToList());
    }

    private static async Task<IResult> CreateCarePlanAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        CreateCarePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                request.AdmissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CreateCarePlanDto(request.AdmissionId, request.NursingDiagnosis, request.Goal);
        var result = await service.CreateCarePlanAsync(dto, actorId, cancellationToken);
        return ToHttpResult(result, plan => Results.Created($"/api/v1/inpatient/nursing/care-plans/{plan.Id}", MapToCarePlanResponse(plan)));
    }

    private static async Task<IResult> AddTaskToCarePlanAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        Guid id,
        AddCareTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessCarePlanAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new AddCareTaskDto(request.Title, request.Frequency, request.DueTimeUtc);
        var result = await service.AddTaskToCarePlanAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, task => Results.Created($"/api/v1/inpatient/nursing/tasks/{task.Id}", MapToCareTaskResponse(task)));
    }

    private static async Task<IResult> CompleteCareTaskAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        Guid id,
        CompleteCareTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessCareTaskAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CompleteCareTaskDto(request.Notes);
        var result = await service.CompleteTaskAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, task => Results.Ok(MapToCareTaskResponse(task)));
    }

    private static async Task<IResult> CancelCareTaskAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        Guid id,
        CancelCareTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessCareTaskAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.CarePlanManage))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new CancelCareTaskDto(request.Reason);
        var result = await service.CancelTaskAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, task => Results.Ok(MapToCareTaskResponse(task)));
    }

    private static async Task<IResult> GetNursingCarePlansAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        [FromQuery] Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                false,
                cancellationToken,
                ClinicalInpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var plans = await service.GetCarePlansByAdmissionIdAsync(admissionId, cancellationToken);
        return Results.Ok(plans.Select(MapToCarePlanResponse).ToList());
    }

    private static async Task<IResult> GetOverdueNursingTasksAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        INursingCareService service,
        [FromQuery] Guid? admissionId,
        [FromQuery] Guid? wardId,
        CancellationToken cancellationToken)
    {
        var tasks = await service.GetOverdueTasksAsync(admissionId, wardId, cancellationToken);
        var visible = new List<NursingCareTaskResponse>();
        foreach (var task in tasks)
        {
            if (await accessControl.CanAccessCareTaskAsync(
                    actor,
                    task.Id,
                    cancellationToken,
                    ClinicalInpatientReadPermissions))
            {
                visible.Add(MapToCareTaskResponse(task));
            }
        }

        return Results.Ok(visible);
    }

    private static NursingObservationResponse MapToObservationResponse(NursingObservationDto o) =>
        new(
            o.Id,
            o.AdmissionId,
            o.PatientId,
            o.RecordedByNurseId,
            o.ObservedAtUtc,
            o.SystolicBp,
            o.DiastolicBp,
            o.HeartRate,
            o.RespiratoryRate,
            o.BodyTemperatureCelsius,
            o.OxygenSaturationPercent,
            o.PainScale,
            o.OralIntakeMl,
            o.IvIntakeMl,
            o.UrineOutputMl,
            o.DrainOutputMl,
            o.OtherOutputMl,
            o.Consciousness.ToString(),
            o.ClinicalNotes,
            o.IsCorrection,
            o.CorrectedObservationId,
            o.CorrectionReason,
            o.CreatedAtUtc,
            o.Version);

    private static NursingCarePlanResponse MapToCarePlanResponse(NursingCarePlanDto p) =>
        new(
            p.Id,
            p.AdmissionId,
            p.PatientId,
            p.CreatedByNurseId,
            p.NursingDiagnosis,
            p.Goal,
            p.Status.ToString(),
            p.CreatedAtUtc,
            p.ResolvedAtUtc,
            p.ResolutionNotes,
            p.Version,
            p.Tasks.Select(MapToCareTaskResponse).ToList());

    private static NursingCareTaskResponse MapToCareTaskResponse(NursingCareTaskDto t) =>
        new(
            t.Id,
            t.CarePlanId,
            t.Title,
            t.Frequency,
            t.DueTimeUtc,
            t.Status.ToString(),
            t.CompletedByNurseId,
            t.CompletedAtUtc,
            t.CompletionNotes,
            t.CancelledByNurseId,
            t.CancelledAtUtc,
            t.CancellationReason,
            t.CreatedAtUtc,
            t.Version);

    private static async Task<IResult> GetInpatientBoardAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientBoardService service,
        [FromQuery] Guid? wardId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? riskLevel,
        [FromQuery] bool? isolationOnly,
        CancellationToken cancellationToken)
    {
        var items = await service.GetInpatientBoardAsync(wardId, departmentId, riskLevel, isolationOnly, cancellationToken);
        var visible = new List<InpatientBoardItemResponse>();
        foreach (var item in items)
        {
            if (await accessControl.CanAccessAdmissionAsync(
                    actor,
                    item.AdmissionId,
                    false,
                    cancellationToken,
                    ClinicalInpatientReadPermissions))
            {
                visible.Add(MapToBoardItemResponse(item));
            }
        }

        return Results.Ok(visible);
    }

    private static async Task<IResult> GetPatientSummaryAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientBoardService service,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                false,
                cancellationToken,
                ClinicalInpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var summary = await service.GetPatientSummaryAsync(admissionId, cancellationToken);
        return summary is not null ? Results.Ok(MapToPatientSummaryResponse(summary)) : Results.NotFound();
    }

    private static InpatientBoardItemResponse MapToBoardItemResponse(InpatientBoardItemDto i) =>
        new(
            i.AdmissionId,
            i.AdmissionNumber,
            i.PatientId,
            i.PatientIdentifier,
            i.PatientFullName,
            i.PatientAge,
            i.PatientGender,
            i.WardId,
            i.WardName,
            i.BedId,
            i.BedNumber,
            i.RoomNumber,
            i.AttendingDoctorId,
            i.AttendingDoctorName,
            i.DepartmentId,
            i.DiagnosisCode,
            i.DiagnosisDescription,
            i.DietType,
            i.FallRiskScore,
            i.FallRiskLevel,
            i.IsolationRequired.ToString(),
            i.HasPendingTransfer,
            i.AdmittedAtUtc,
            i.DaysInHospital,
            i.EstimatedStayDays,
            i.PendingTasksCount);

    private static InpatientPatientSummaryResponse MapToPatientSummaryResponse(InpatientPatientSummaryDto s) =>
        new(
            s.AdmissionId,
            s.AdmissionNumber,
            s.PatientId,
            s.PatientFullName,
            s.WardName,
            s.BedNumber,
            s.RoomNumber,
            s.AttendingDoctorName,
            s.DiagnosisDescription,
            s.DietType,
            s.FallRiskScore,
            s.FallRiskLevel,
            s.IsolationRequired.ToString(),
            s.AdmittedAtUtc,
            s.DaysInHospital,
            s.HasPendingTransfer);

    private static WardResponse MapToWardResponse(WardDto w) =>
        new(
            w.Id,
            w.Code,
            w.Name,
            w.DepartmentId,
            w.Building,
            w.Floor,
            w.WardType.ToString(),
            w.IsActive,
            w.RoomCount,
            w.TotalBeds,
            w.AvailableBeds);

    private static RoomResponse MapToRoomResponse(RoomDto r) =>
        new(
            r.Id,
            r.WardId,
            r.RoomNumber,
            r.GenderConstraint.ToString(),
            r.IsolationType.ToString(),
            r.IsNegativePressure,
            r.IsActive,
            r.Beds.Select(MapToBedResponse).ToList());

    private static BedResponse MapToBedResponse(BedDto b) =>
        new(
            b.Id,
            b.WardId,
            b.RoomId,
            b.BedNumber,
            b.Status.ToString(),
            b.CurrentAdmissionId,
            b.CurrentPatientId,
            b.GenderConstraint.ToString(),
            b.IsolationType.ToString(),
            b.HasTelemetry,
            b.HasOxygen,
            b.HasVentilator,
            b.MaintenanceReason,
            b.IsActive,
            b.Version);

    private static IResult MapOccupancySummary(BedOccupancySummaryDto summary) =>
        Results.Ok(new BedOccupancySummaryResponse(
            summary.TotalBeds,
            summary.AvailableBeds,
            summary.OccupiedBeds,
            summary.CleaningBeds,
            summary.MaintenanceBeds,
            summary.ReservedBeds,
            summary.OccupancyRatePercent));

    private static async Task<IResult> ScheduleMedicationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        ScheduleMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                request.AdmissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new ScheduleMedicationDto(
            request.AdmissionId,
            request.PrescriptionId,
            request.MedicationName,
            request.Dose,
            request.Route,
            request.ScheduledTimeUtc);

        var result = await service.ScheduleDoseAsync(dto, actorId, cancellationToken);
        return ToHttpResult(result, m => Results.Created($"/api/v1/inpatient/emar/{m.Id}", MapToMedicationResponse(m)));
    }

    private static async Task<IResult> GetActiveMedicationOrdersAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        InpatientMedicationOrderValidator medicationOrderValidator,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var patientId = await accessControl.FindAdmissionPatientIdAsync(admissionId, cancellationToken);
        if (!patientId.HasValue)
        {
            return Results.NotFound();
        }

        var orders = await medicationOrderValidator.GetActiveOrdersAsync(patientId.Value, cancellationToken);
        return Results.Ok(orders);
    }

    private static async Task<IResult> AdministerMedicationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        Guid id,
        AdministerMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessMedicationAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new AdministerMedicationDto(request.Verified5Rights, request.Notes);
        var result = await service.AdministerMedicationAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, m => Results.Ok(MapToMedicationResponse(m)));
    }

    private static async Task<IResult> SkipMedicationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        Guid id,
        SkipMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessMedicationAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new SkipMedicationDto(request.Reason);
        var result = await service.SkipMedicationAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, m => Results.Ok(MapToMedicationResponse(m)));
    }

    private static async Task<IResult> RefuseMedicationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        Guid id,
        RefuseMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessMedicationAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new RefuseMedicationDto(request.Reason);
        var result = await service.RefuseMedicationAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, m => Results.Ok(MapToMedicationResponse(m)));
    }

    private static async Task<IResult> DelayMedicationAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        Guid id,
        DelayMedicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessMedicationAsync(
                actor,
                id,
                cancellationToken,
                HospitalPermissions.Inpatient.MedicationAdminister))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        var dto = new DelayMedicationDto(request.NewScheduledTimeUtc, request.Reason);
        var result = await service.DelayMedicationAsync(id, dto, actorId, cancellationToken);
        return ToHttpResult(result, m => Results.Ok(MapToMedicationResponse(m)));
    }

    private static async Task<IResult> GetMedicationsByAdmissionIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                false,
                cancellationToken,
                ClinicalInpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var items = await service.GetAdministrationsByAdmissionIdAsync(admissionId, cancellationToken);
        return Results.Ok(items.Select(MapToMedicationResponse).ToList());
    }

    private static async Task<IResult> GetDueMedicationsAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IMedicationAdministrationService service,
        [FromQuery] Guid? admissionId,
        [FromQuery] Guid? wardId,
        CancellationToken cancellationToken)
    {
        var items = await service.GetDueAdministrationsAsync(admissionId, wardId, cancellationToken);
        var visible = new List<MedicationAdministrationResponse>();
        foreach (var item in items)
        {
            if (await accessControl.CanAccessAdmissionAsync(
                    actor,
                    item.AdmissionId,
                    false,
                    cancellationToken,
                    ClinicalInpatientReadPermissions))
            {
                visible.Add(MapToMedicationResponse(item));
            }
        }

        return Results.Ok(visible);
    }

    private static MedicationAdministrationResponse MapToMedicationResponse(MedicationAdministrationDto m) =>
        new(
            m.Id,
            m.AdmissionId,
            m.PatientId,
            m.PrescriptionId,
            m.MedicationName,
            m.Dose,
            m.Route,
            m.ScheduledTimeUtc,
            m.Status.ToString(),
            m.AdministeredByNurseId,
            m.AdministeredAtUtc,
            m.Verified5Rights,
            m.Reason,
            m.Notes,
            m.CreatedAtUtc,
            m.Version);

    private static async Task<IResult> ProcessDischargeAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientDischargeService service,
        DischargeAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                request.AdmissionId,
                false,
                cancellationToken,
                HospitalPermissions.Inpatient.DischargeComplete))
        {
            return ForbiddenResource();
        }

        var actorId = ExtractActorId(actor);
        if (!Enum.TryParse<DischargeType>(request.DischargeType, true, out var dischargeType))
        {
            dischargeType = DischargeType.Home;
        }

        var dto = new DischargeAdmissionDto(
            request.AdmissionId,
            dischargeType,
            request.DischargeSummary,
            request.FinalDiagnosisCode,
            request.FinalDiagnosisDescription,
            request.DischargeRecommendations,
            request.DischargePrescriptionSummary,
            request.FollowUpAppointmentDateUtc,
            request.FollowUpDepartmentId,
            request.TransferFacilityName,
            request.TransferReason);

        var result = await service.ProcessDischargeAsync(dto, actorId, cancellationToken);
        return ToHttpResult(result, d => Results.Created($"/api/v1/inpatient/discharges/{d.AdmissionId}", MapToDischargeResponse(d)));
    }

    private static async Task<IResult> GetDischargeByAdmissionIdAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientDischargeService service,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessAdmissionAsync(
                actor,
                admissionId,
                true,
                cancellationToken,
                InpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var discharge = await service.GetDischargeByAdmissionIdAsync(admissionId, cancellationToken);
        return discharge is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Taburculuk Bulunamadı", detail: "Yatış için taburculuk kaydı bulunamadı.")
            : Results.Ok(MapToDischargeResponse(discharge));
    }

    private static async Task<IResult> GetDischargesAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientDischargeService service,
        [FromQuery] Guid? patientId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var list = await service.GetDischargesAsync(patientId, fromDate, toDate, cancellationToken);
        var visible = new List<InpatientDischargeResponse>();
        foreach (var discharge in list)
        {
            if (await accessControl.CanAccessAdmissionAsync(
                    actor,
                    discharge.AdmissionId,
                    true,
                    cancellationToken,
                    InpatientReadPermissions))
            {
                visible.Add(MapToDischargeResponse(discharge));
            }
        }

        return Results.Ok(visible);
    }

    private static InpatientDischargeResponse MapToDischargeResponse(InpatientDischargeDto d) =>
        new(
            d.Id,
            d.AdmissionId,
            d.PatientId,
            d.DischargingDoctorId,
            d.DischargeType.ToString(),
            d.DischargeSummary,
            d.FinalDiagnosisCode,
            d.FinalDiagnosisDescription,
            d.DischargeRecommendations,
            d.DischargePrescriptionSummary,
            d.FollowUpAppointmentDateUtc,
            d.FollowUpDepartmentId,
            d.TransferFacilityName,
            d.TransferReason,
            d.DischargedAtUtc,
            d.CreatedAtUtc,
            d.Version);

    private static async Task<IResult> GetDashboardSummaryAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientDashboardService service,
        [FromQuery] Guid? wardId,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (wardId.HasValue)
        {
            if (!await accessControl.CanAccessWardAsync(
                    actor,
                    wardId.Value,
                    cancellationToken,
                    ClinicalInpatientReadPermissions))
            {
                return ForbiddenResource();
            }

            var scoped = await service.GetDashboardSummaryAsync(wardId, null, cancellationToken);
            return Results.Ok(MapToDashboardResponse(scoped));
        }

        var departmentIds = await accessControl.GetAccessibleDepartmentIdsAsync(
            actor,
            cancellationToken,
            ClinicalInpatientReadPermissions);
        if (departmentId.HasValue)
        {
            if (!departmentIds.Contains(departmentId.Value))
            {
                return ForbiddenResource();
            }

            var scoped = await service.GetDashboardSummaryAsync(null, departmentId, cancellationToken);
            return Results.Ok(MapToDashboardResponse(scoped));
        }

        var summaries = new List<InpatientDashboardDto>();
        foreach (var accessibleDepartmentId in departmentIds)
        {
            summaries.Add(await service.GetDashboardSummaryAsync(
                departmentId: accessibleDepartmentId,
                cancellationToken: cancellationToken));
        }

        return Results.Ok(MapToDashboardResponse(CombineDashboardSummaries(summaries)));
    }

    private static async Task<IResult> GetWardDashboardAsync(
        ClaimsPrincipal actor,
        InpatientAccessControl accessControl,
        IInpatientDashboardService service,
        Guid wardId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessWardAsync(
                actor,
                wardId,
                cancellationToken,
                ClinicalInpatientReadPermissions))
        {
            return ForbiddenResource();
        }

        var result = await service.GetDashboardSummaryAsync(wardId: wardId, cancellationToken: cancellationToken);
        return Results.Ok(MapToDashboardResponse(result));
    }

    private static InpatientDashboardDto CombineDashboardSummaries(
        IReadOnlyCollection<InpatientDashboardDto> summaries)
    {
        var wards = summaries.SelectMany(summary => summary.Wards).ToList();
        var totalBeds = summaries.Sum(summary => summary.TotalBeds);
        var occupiedBeds = summaries.Sum(summary => summary.OccupiedBeds);
        return new InpatientDashboardDto(
            totalBeds,
            occupiedBeds,
            summaries.Sum(summary => summary.AvailableBeds),
            summaries.Sum(summary => summary.CleaningBeds),
            summaries.Sum(summary => summary.MaintenanceBeds),
            totalBeds == 0 ? 0 : Math.Round((double)occupiedBeds / totalBeds * 100, 2),
            summaries.Sum(summary => summary.PendingAdmissionsCount),
            summaries.Sum(summary => summary.PendingTransfersCount),
            summaries.Sum(summary => summary.TodayDischargesCount),
            wards);
    }

    private static InpatientDashboardResponse MapToDashboardResponse(InpatientDashboardDto dto) =>
        new(
            dto.TotalBeds,
            dto.OccupiedBeds,
            dto.AvailableBeds,
            dto.CleaningBeds,
            dto.MaintenanceBeds,
            dto.OverallOccupancyPercentage,
            dto.PendingAdmissionsCount,
            dto.PendingTransfersCount,
            dto.TodayDischargesCount,
            dto.Wards.Select(w => new WardOccupancySummaryItem(
                w.WardId,
                w.WardName,
                w.WardCode,
                w.DepartmentId,
                w.TotalBeds,
                w.OccupiedBeds,
                w.AvailableBeds,
                w.CleaningBeds,
                w.MaintenanceBeds,
                w.OccupancyPercentage,
                w.ActivePatientsCount)).ToList());

    private static IResult ToHttpResult<T>(InpatientOperationResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            InpatientOperationStatus.Success => onSuccess(result.Value!),
            InpatientOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage),
            InpatientOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma / Concurrency Hatası",
                detail: result.ErrorMessage),
            InpatientOperationStatus.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Yetkisiz Erişim",
                detail: result.ErrorMessage),
            InpatientOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors ?? new Dictionary<string, string[]>
                {
                    ["Error"] = [result.ErrorMessage ?? "Doğrulama hatası."],
                }),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Sunucu Hatası",
                detail: result.ErrorMessage),
        };
    }

    private static IResult ForbiddenResource(string? detail = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Yetkisiz Erişim",
            detail: detail ?? "Bu yatan hasta kaynağı için gerekli bakım ilişkisi veya organizasyon kapsamı bulunmuyor.");
}
