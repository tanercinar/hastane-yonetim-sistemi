using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Reporting;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Reporting.Application;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Reporting;

public static class ReportingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reportingGroup = endpoints.MapGroup("/api/v1/reporting")
            .RequireAuthorization();

        var projectionsGroup = reportingGroup.MapGroup("/projections")
            .RequireAuthorization();

        projectionsGroup.MapPost("/rebuild", RebuildProjectionsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ProjectionManage)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RebuildReportingProjections")
            .Produces<RebuildProjectionsResponse>();

        projectionsGroup.MapGet("/checkpoints", GetCheckpointsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ProjectionManage)
            .WithName("GetReportingCheckpoints")
            .Produces<List<ProjectionCheckpointResponse>>();

        projectionsGroup.MapGet("/lag", GetProjectionLagAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ProjectionManage)
            .WithName("GetReportingProjectionLag")
            .Produces<List<ProjectionLagInfo>>();

        var metricsGroup = reportingGroup.MapGroup("/metrics")
            .RequireAuthorization();

        metricsGroup.MapGet("/outpatient", GetOutpatientMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .AddEndpointFilter(new ReportingAreaEndpointFilter(OutpatientRoles))
            .WithName("GetDailyOutpatientMetrics")
            .Produces<List<DailyOutpatientMetricResponse>>();

        metricsGroup.MapGet("/diagnostics", GetDiagnosticMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .AddEndpointFilter(new ReportingAreaEndpointFilter(DiagnosticRoles))
            .WithName("GetDiagnosticWorkloadMetrics")
            .Produces<List<DiagnosticWorkloadMetricResponse>>();

        metricsGroup.MapGet("/occupancy", GetBedOccupancyMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .AddEndpointFilter(new ReportingAreaEndpointFilter(InpatientRoles))
            .WithName("GetBedOccupancyMetrics")
            .Produces<List<BedOccupancyMetricResponse>>();

        metricsGroup.MapGet("/pharmacy", GetPharmacyMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .AddEndpointFilter(new ReportingAreaEndpointFilter(PharmacyRoles))
            .WithName("GetPharmacyDispensingMetrics")
            .Produces<List<PharmacyDispensingMetricResponse>>();

        var dashboardGroup = reportingGroup.MapGroup("/dashboards/outpatient")
            .RequireAuthorization()
            .AddEndpointFilter(new ReportingAreaEndpointFilter(OutpatientRoles));

        dashboardGroup.MapGet("/summary", GetOutpatientDashboardSummaryAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetOutpatientDashboardSummary")
            .Produces<OutpatientDashboardSummaryResponse>();

        dashboardGroup.MapGet("/departments", GetOutpatientDepartmentMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetOutpatientDepartmentMetrics")
            .Produces<List<OutpatientDepartmentMetricResponse>>();

        dashboardGroup.MapGet("/doctors", GetOutpatientDoctorMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetOutpatientDoctorMetrics")
            .Produces<List<OutpatientDoctorMetricResponse>>();

        var diagDashboardGroup = reportingGroup.MapGroup("/dashboards/diagnostics")
            .RequireAuthorization()
            .AddEndpointFilter(new ReportingAreaEndpointFilter(DiagnosticRoles));

        diagDashboardGroup.MapGet("/summary", GetDiagnosticDashboardSummaryAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetDiagnosticDashboardSummary")
            .Produces<DiagnosticDashboardSummaryResponse>();

        diagDashboardGroup.MapGet("/modalities", GetDiagnosticModalityMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetDiagnosticModalityMetrics")
            .Produces<List<DiagnosticModalityMetricResponse>>();

        diagDashboardGroup.MapGet("/critical-alerts", GetDiagnosticCriticalAlertMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetDiagnosticCriticalAlertMetrics")
            .Produces<List<DiagnosticCriticalAlertMetricResponse>>();

        var inpatientDashboardGroup = reportingGroup.MapGroup("/dashboards/inpatient-operations")
            .RequireAuthorization()
            .AddEndpointFilter(new ReportingAreaEndpointFilter(InpatientRoles));

        inpatientDashboardGroup.MapGet("/summary", GetInpatientOperationsSummaryAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetInpatientOperationsSummary")
            .Produces<InpatientOperationsSummaryResponse>();

        inpatientDashboardGroup.MapGet("/wards", GetWardOccupancyAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetWardOccupancy")
            .Produces<List<WardOccupancyDetailResponse>>();

        inpatientDashboardGroup.MapGet("/emergency-triage", GetEmergencyTriageMetricsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetEmergencyTriageMetrics")
            .Produces<List<EmergencyTriageQueueMetricResponse>>();

        var pharmacyDashboardGroup = reportingGroup.MapGroup("/dashboards/pharmacy")
            .RequireAuthorization()
            .AddEndpointFilter(new ReportingAreaEndpointFilter(PharmacyRoles));

        pharmacyDashboardGroup.MapGet("/summary", GetPharmacyDashboardSummaryAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetPharmacyDashboardSummary")
            .Produces<PharmacyDashboardSummaryResponse>();

        pharmacyDashboardGroup.MapGet("/stock-alerts", GetPharmacyStockAlertsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetPharmacyStockAlerts")
            .Produces<List<PharmacyStockAlertMetricResponse>>();

        var exportsGroup = reportingGroup.MapGroup("/exports")
            .RequireAuthorization();

        exportsGroup.MapGet("/csv", ExportCsvGetAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsExport)
            .WithName("ExportCsvGet")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        exportsGroup.MapPost("/csv", ExportCsvPostAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsExport)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ExportCsvPost")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        return endpoints;
    }

    private static async Task<IResult> RebuildProjectionsAsync(
        IProjectionRebuilder rebuilder,
        [FromBody] RebuildProjectionsRequest? request,
        CancellationToken cancellationToken)
    {
        RebuildSummaryDto summary;

        if (string.IsNullOrWhiteSpace(request?.ProjectionName))
        {
            summary = await rebuilder.RebuildAllProjectionsAsync(cancellationToken);
        }
        else
        {
            summary = await rebuilder.RebuildProjectionAsync(request.ProjectionName, cancellationToken);
        }

        return Results.Ok(new RebuildProjectionsResponse(
            summary.Success,
            summary.TotalProjectionsRebuilt,
            summary.RebuiltProjections,
            summary.CompletedAtUtc,
            summary.Message));
    }

    private static async Task<IResult> GetCheckpointsAsync(
        IReportingReadModelService readModelService,
        CancellationToken cancellationToken)
    {
        var checkpoints = await readModelService.GetCheckpointsAsync(cancellationToken);
        var response = checkpoints.Select(c => new ProjectionCheckpointResponse(
            c.ProjectionName,
            c.LastProcessedPosition,
            c.LastProcessedTimestampUtc,
            c.Status,
            c.Version,
            c.LastError)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProjectionLagAsync(
        IReportingReadModelService readModelService,
        CancellationToken cancellationToken)
    {
        var lagList = await readModelService.GetProjectionLagAsync(cancellationToken);
        var response = lagList.Select(l => new ProjectionLagInfo(
            l.ProjectionName,
            l.LastProcessedPosition,
            l.LastProcessedTimestampUtc,
            l.LagSeconds,
            l.IsHealthy,
            l.CheckedAtUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetOutpatientMetricsAsync(
        IReportingReadModelService readModelService,
        ClaimsPrincipal user,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? doctorId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? start = DateOnly.TryParse(startDate, out var s) ? s : null;
        DateOnly? end = DateOnly.TryParse(endDate, out var e) ? e : null;

        var effectiveDoctorId = ResolveDoctorScope(user, doctorId);

        var metrics = await readModelService.GetOutpatientMetricsAsync(
            start,
            end,
            effectiveDepartmentId,
            effectiveDoctorId,
            cancellationToken);

        var response = metrics.Select(m => new DailyOutpatientMetricResponse(
            m.Id,
            m.Date,
            m.DepartmentId,
            m.DepartmentName,
            m.DoctorId,
            m.DoctorName,
            m.TotalAppointments,
            m.ScheduledCount,
            m.CheckedInCount,
            m.InProgressCount,
            m.CompletedCount,
            m.CancelledCount,
            m.NoShowCount,
            m.LastUpdatedUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDiagnosticMetricsAsync(
        IReportingReadModelService readModelService,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? modalityOrSection,
        CancellationToken cancellationToken)
    {
        DateOnly? start = DateOnly.TryParse(startDate, out var s) ? s : null;
        DateOnly? end = DateOnly.TryParse(endDate, out var e) ? e : null;

        var metrics = await readModelService.GetDiagnosticMetricsAsync(
            start,
            end,
            modalityOrSection,
            cancellationToken);

        var response = metrics.Select(m => new DiagnosticWorkloadMetricResponse(
            m.Id,
            m.Date,
            m.ModalityOrSection,
            m.TotalOrders,
            m.PendingSpecimenCount,
            m.ProcessingCount,
            m.FinalizedCount,
            m.CriticalCount,
            m.AvgTurnaroundMinutes,
            m.LastUpdatedUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetBedOccupancyMetricsAsync(
        IReportingReadModelService readModelService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? wardType,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var metrics = await readModelService.GetBedOccupancyMetricsAsync(
            d,
            effectiveDepartmentId,
            wardType,
            cancellationToken);

        var response = metrics.Select(m => new BedOccupancyMetricResponse(
            m.Id,
            m.Date,
            m.DepartmentId,
            m.DepartmentName,
            m.WardType,
            m.TotalBeds,
            m.OccupiedBeds,
            m.AvailableBeds,
            m.PendingTransferCount,
            m.OccupancyRatePercentage,
            m.LastUpdatedUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPharmacyMetricsAsync(
        IReportingReadModelService readModelService,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        CancellationToken cancellationToken)
    {
        DateOnly? start = DateOnly.TryParse(startDate, out var s) ? s : null;
        DateOnly? end = DateOnly.TryParse(endDate, out var e) ? e : null;

        var metrics = await readModelService.GetPharmacyMetricsAsync(
            start,
            end,
            cancellationToken);

        var response = metrics.Select(m => new PharmacyDispensingMetricResponse(
            m.Id,
            m.Date,
            m.TotalPrescriptions,
            m.PendingDispenseCount,
            m.DispensedCount,
            m.LowStockItemCount,
            m.NearExpiryLotCount,
            m.LastUpdatedUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetOutpatientDashboardSummaryAsync(
        IOutpatientDashboardService dashboardService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? doctorId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        // Resource scope resolution: If caller is a Doctor without management oversight, constrain to doctor's PersonId
        var effectiveDoctorId = doctorId;
        effectiveDoctorId = ResolveDoctorScope(user, doctorId);

        var summary = await dashboardService.GetSummaryAsync(
            d,
            effectiveDepartmentId,
            effectiveDoctorId,
            cancellationToken);

        return Results.Ok(new OutpatientDashboardSummaryResponse(
            summary.Date,
            summary.TotalAppointments,
            summary.ScheduledCount,
            summary.CheckedInCount,
            summary.InProgressCount,
            summary.CompletedCount,
            summary.CancelledCount,
            summary.NoShowCount,
            summary.WaitingQueueCount,
            summary.LastUpdatedUtc));
    }

    private static async Task<IResult> GetOutpatientDepartmentMetricsAsync(
        IOutpatientDashboardService dashboardService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        if (!TryResolveDepartmentScope(user, null, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        var list = await dashboardService.GetDepartmentMetricsAsync(d, cancellationToken);
        if (effectiveDepartmentId.HasValue)
        {
            list = list.Where(metric => metric.DepartmentId == effectiveDepartmentId.Value).ToList();
        }

        var response = list.Select(m => new OutpatientDepartmentMetricResponse(
            m.DepartmentId,
            m.DepartmentName,
            m.TotalAppointments,
            m.ScheduledCount,
            m.CheckedInCount,
            m.InProgressCount,
            m.CompletedCount,
            m.CancelledCount,
            m.NoShowCount)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetOutpatientDoctorMetricsAsync(
        IOutpatientDashboardService dashboardService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetDoctorMetricsAsync(d, effectiveDepartmentId, cancellationToken);
        var effectiveDoctorId = ResolveDoctorScope(user, null);
        if (effectiveDoctorId.HasValue)
        {
            list = list.Where(metric => metric.DoctorId == effectiveDoctorId.Value).ToList();
        }

        var response = list.Select(m => new OutpatientDoctorMetricResponse(
            m.DoctorId,
            m.DoctorName,
            m.DepartmentId,
            m.DepartmentName,
            m.TotalAppointments,
            m.CompletedCount,
            m.WaitingCount)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDiagnosticDashboardSummaryAsync(
        IDiagnosticDashboardService dashboardService,
        [FromQuery] string? date,
        [FromQuery] string? modalityOrSection,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var summary = await dashboardService.GetSummaryAsync(d, modalityOrSection, cancellationToken);

        return Results.Ok(new DiagnosticDashboardSummaryResponse(
            summary.Date,
            summary.TotalOrders,
            summary.PendingSpecimenCount,
            summary.ProcessingCount,
            summary.FinalizedCount,
            summary.CriticalCount,
            summary.AvgTurnaroundMinutes,
            summary.LastUpdatedUtc));
    }

    private static async Task<IResult> GetDiagnosticModalityMetricsAsync(
        IDiagnosticDashboardService dashboardService,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetModalityMetricsAsync(d, cancellationToken);

        var response = list.Select(m => new DiagnosticModalityMetricResponse(
            m.ModalityOrSection,
            m.TotalOrders,
            m.PendingSpecimenCount,
            m.ProcessingCount,
            m.FinalizedCount,
            m.CriticalCount,
            m.AvgTurnaroundMinutes)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDiagnosticCriticalAlertMetricsAsync(
        IDiagnosticDashboardService dashboardService,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetCriticalAlertMetricsAsync(d, cancellationToken);

        var response = list.Select(m => new DiagnosticCriticalAlertMetricResponse(
            m.MetricId,
            m.Date,
            m.ModalityOrSection,
            m.CriticalCount,
            m.LastUpdatedUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetInpatientOperationsSummaryAsync(
        IInpatientOperationsDashboardService dashboardService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var summary = await dashboardService.GetSummaryAsync(d, effectiveDepartmentId, cancellationToken);

        return Results.Ok(new InpatientOperationsSummaryResponse(
            summary.Date,
            summary.TotalBeds,
            summary.OccupiedBeds,
            summary.AvailableBeds,
            summary.OverallOccupancyRate,
            summary.PendingTransfers,
            summary.EmergencyWaitingCount,
            summary.OperatingRoomsInUse,
            summary.LastUpdatedUtc));
    }

    private static async Task<IResult> GetWardOccupancyAsync(
        IInpatientOperationsDashboardService dashboardService,
        ClaimsPrincipal user,
        [FromQuery] string? date,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDepartmentScope(user, departmentId, out var effectiveDepartmentId))
        {
            return Results.Forbid();
        }

        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetWardOccupancyAsync(d, effectiveDepartmentId, cancellationToken);

        var response = list.Select(m => new WardOccupancyDetailResponse(
            m.DepartmentId,
            m.DepartmentName,
            m.WardType,
            m.TotalBeds,
            m.OccupiedBeds,
            m.AvailableBeds,
            m.OccupancyRatePercentage,
            m.PendingTransfers)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetEmergencyTriageMetricsAsync(
        IInpatientOperationsDashboardService dashboardService,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetEmergencyTriageMetricsAsync(d, cancellationToken);

        var response = list.Select(m => new EmergencyTriageQueueMetricResponse(
            m.TriageCategory,
            m.WaitingCount,
            m.AvgWaitMinutes)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPharmacyDashboardSummaryAsync(
        IPharmacyDashboardService dashboardService,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var summary = await dashboardService.GetSummaryAsync(d, cancellationToken);

        return Results.Ok(new PharmacyDashboardSummaryResponse(
            summary.Date,
            summary.TotalPrescriptions,
            summary.PendingDispenseCount,
            summary.DispensedCount,
            summary.LowStockItemCount,
            summary.NearExpiryLotCount,
            summary.LastUpdatedUtc));
    }

    private static async Task<IResult> GetPharmacyStockAlertsAsync(
        IPharmacyDashboardService dashboardService,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        DateOnly? d = DateOnly.TryParse(date, out var parsedDate) ? parsedDate : null;

        var list = await dashboardService.GetStockAlertsAsync(d, cancellationToken);

        var response = list.Select(m => new PharmacyStockAlertMetricResponse(
            m.AlertType,
            m.ItemName,
            m.CurrentStock,
            m.MinimumThreshold,
            m.Unit)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> ExportCsvGetAsync(
        ISecureExportService exportService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        [FromQuery] string reportType,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        DateOnly? s = DateOnly.TryParse(startDate, out var parsedStart) ? parsedStart : null;
        DateOnly? e = DateOnly.TryParse(endDate, out var parsedEnd) ? parsedEnd : null;

        var request = new SecureExportRequestDto(reportType, s, e, departmentId);
        return await ExecuteExportAsync(exportService, request, user, httpContext, cancellationToken);
    }

    private static async Task<IResult> ExportCsvPostAsync(
        ISecureExportService exportService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        [FromBody] SecureExportRequestDto request,
        CancellationToken cancellationToken)
    {
        return await ExecuteExportAsync(exportService, request, user, httpContext, cancellationToken);
    }

    private static async Task<IResult> ExecuteExportAsync(
        ISecureExportService exportService,
        SecureExportRequestDto request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var personIdStr = user.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        var actorUserId = Guid.TryParse(userIdStr, out var u) ? u : (Guid?)null;
        var actorPersonId = Guid.TryParse(personIdStr, out var p) ? p : (Guid?)null;

        if (RequiresDepartmentScope(request.ReportType))
        {
            if (!TryResolveDepartmentScope(user, request.DepartmentId, out var effectiveDepartmentId))
            {
                return Results.Forbid();
            }

            request = request with { DepartmentId = effectiveDepartmentId };
        }

        try
        {
            var result = await exportService.ExportCsvAsync(
                request,
                actorUserId,
                actorPersonId,
                role,
                httpContext.TraceIdentifier,
                cancellationToken);

            return Results.File(result.Content, result.ContentType, result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Dışa aktarma sınırı aşıldı",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                title: "Geçersiz rapor isteği",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static readonly string[] OutpatientRoles =
    [
        HospitalRoles.Doctor,
        HospitalRoles.Nurse,
        HospitalRoles.ChiefMedicalOfficer,
        HospitalRoles.RegistrationStaff,
        HospitalRoles.HospitalManager,
    ];

    private static readonly string[] DiagnosticRoles =
    [
        HospitalRoles.Doctor,
        HospitalRoles.Nurse,
        HospitalRoles.ChiefMedicalOfficer,
        HospitalRoles.LaboratoryStaff,
        HospitalRoles.RadiologyStaff,
        HospitalRoles.HospitalManager,
    ];

    private static readonly string[] InpatientRoles =
    [
        HospitalRoles.Doctor,
        HospitalRoles.Nurse,
        HospitalRoles.ChiefMedicalOfficer,
        HospitalRoles.RegistrationStaff,
        HospitalRoles.HospitalManager,
    ];

    private static readonly string[] PharmacyRoles =
    [
        HospitalRoles.Doctor,
        HospitalRoles.Nurse,
        HospitalRoles.ChiefMedicalOfficer,
        HospitalRoles.Pharmacist,
        HospitalRoles.HospitalManager,
    ];

    private static bool TryResolveDepartmentScope(
        ClaimsPrincipal user,
        Guid? requestedDepartmentId,
        out Guid? effectiveDepartmentId)
    {
        if (user.IsInRole(HospitalRoles.HospitalManager))
        {
            effectiveDepartmentId = requestedDepartmentId;
            return true;
        }

        var departmentClaim = user.FindFirst(HospitalClaimTypes.DepartmentId)?.Value;
        if (!Guid.TryParse(departmentClaim, out var assignedDepartmentId)
            || assignedDepartmentId == Guid.Empty
            || (requestedDepartmentId.HasValue && requestedDepartmentId.Value != assignedDepartmentId))
        {
            effectiveDepartmentId = null;
            return false;
        }

        effectiveDepartmentId = assignedDepartmentId;
        return true;
    }

    private static Guid? ResolveDoctorScope(ClaimsPrincipal user, Guid? requestedDoctorId)
    {
        if (!user.IsInRole(HospitalRoles.Doctor))
        {
            return requestedDoctorId;
        }

        return Guid.TryParse(
            user.FindFirst(HospitalClaimTypes.PersonId)?.Value,
            out var doctorPersonId)
                ? doctorPersonId
                : Guid.Empty;
    }

    private static bool RequiresDepartmentScope(string reportType) =>
        reportType.Trim().ToLowerInvariant() is
            "outpatient-metrics" or "outpatient" or "poliklinik" or
            "bed-occupancy" or "occupancy" or "yatak";

    private sealed class ReportingAreaEndpointFilter(params string[] allowedRoles) : IEndpointFilter
    {
        private readonly HashSet<string> _allowedRoles = new(allowedRoles, StringComparer.Ordinal);

        public ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next) =>
            context.HttpContext.User.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role && _allowedRoles.Contains(claim.Value))
                    ? next(context)
                    : ValueTask.FromResult<object?>(Results.Forbid());
    }
}


