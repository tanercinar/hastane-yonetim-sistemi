using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Specialty;

public static class SpecialtyCareEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSpecialtyCareEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var specialtyGroup = endpoints.MapGroup("/api/v1/specialty")
            .RequireAuthorization();

        var pregnancyGroup = specialtyGroup.MapGroup("/pregnancy-episodes");

        pregnancyGroup.MapPost(string.Empty, CreatePregnancyEpisodeAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreatePregnancyEpisode")
            .Produces<PregnancyEpisodeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        pregnancyGroup.MapGet("/active", GetActivePregnancyEpisodesAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetActivePregnancyEpisodes")
            .Produces<List<PregnancyEpisodeResponse>>();

        pregnancyGroup.MapGet("/patient/{patientId:guid}", GetPregnancyEpisodesByPatientIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetPregnancyEpisodesByPatientId")
            .Produces<List<PregnancyEpisodeResponse>>();

        pregnancyGroup.MapGet("/{id:guid}", GetPregnancyEpisodeByIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetPregnancyEpisodeById")
            .Produces<PregnancyEpisodeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        pregnancyGroup.MapPost("/{id:guid}/antenatal-visits", RecordAntenatalVisitAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordAntenatalVisit")
            .Produces<AntenatalVisitResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pregnancyGroup.MapPost("/{id:guid}/risk-category", UpdateRiskCategoryAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdatePregnancyRiskCategory")
            .Produces<PregnancyEpisodeResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        pregnancyGroup.MapPost("/{id:guid}/complete", CompleteEpisodeAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompletePregnancyEpisode")
            .Produces<PregnancyEpisodeResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var deliveryGroup = specialtyGroup.MapGroup("/deliveries");

        deliveryGroup.MapPost(string.Empty, CreateDeliveryRecordAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateDeliveryRecord")
            .Produces<DeliveryRecordResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        deliveryGroup.MapGet("/{id:guid}", GetDeliveryRecordByIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetDeliveryRecordById")
            .Produces<DeliveryRecordResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        deliveryGroup.MapGet("/mother/{motherPatientId:guid}", GetDeliveryRecordsByMotherPatientIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetDeliveryRecordsByMotherPatientId")
            .Produces<List<DeliveryRecordResponse>>();

        deliveryGroup.MapPost("/{id:guid}/newborns", AddNewbornAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AddNewbornToDelivery")
            .Produces<NewbornResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var dentalGroup = specialtyGroup.MapGroup("/dental");

        dentalGroup.MapGet("/odontogram/{patientId:guid}", GetLatestOdontogramAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetLatestOdontogram")
            .Produces<List<ToothConditionResponse>>();

        dentalGroup.MapPost("/odontogram/{patientId:guid}/tooth", RecordToothConditionAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordToothCondition")
            .Produces<ToothConditionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        dentalGroup.MapGet("/odontogram/{patientId:guid}/tooth/{toothNumber:int}/history", GetToothHistoryAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetToothHistory")
            .Produces<List<ToothConditionResponse>>();

        dentalGroup.MapGet("/procedures/patient/{patientId:guid}", GetDentalProceduresAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetDentalProcedures")
            .Produces<List<DentalProcedureResponse>>();

        dentalGroup.MapPost("/procedures", PlanDentalProcedureAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("PlanDentalProcedure")
            .Produces<DentalProcedureResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        dentalGroup.MapPost("/procedures/{id:guid}/complete", CompleteDentalProcedureAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteDentalProcedure")
            .Produces<DentalProcedureResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        dentalGroup.MapGet("/examinations/patient/{patientId:guid}", GetDentalExaminationsAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetDentalExaminations")
            .Produces<List<DentalExaminationResponse>>();

        dentalGroup.MapPost("/examinations", CreateDentalExaminationAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateDentalExamination")
            .Produces<DentalExaminationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        var homeHealthGroup = specialtyGroup.MapGroup("/home-health");

        homeHealthGroup.MapPost("/visits", RequestHomeHealthVisitAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RequestHomeHealthVisit")
            .Produces<HomeHealthVisitResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        homeHealthGroup.MapGet("/visits/active", GetActiveHomeHealthVisitsAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetActiveHomeHealthVisits")
            .Produces<List<HomeHealthVisitResponse>>();

        homeHealthGroup.MapGet("/visits/{id:guid}", GetHomeHealthVisitByIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetHomeHealthVisitById")
            .Produces<HomeHealthVisitResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        homeHealthGroup.MapGet("/visits/patient/{patientId:guid}", GetHomeHealthVisitsByPatientIdAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareView)
            .WithName("GetHomeHealthVisitsByPatientId")
            .Produces<List<HomeHealthVisitResponse>>();

        homeHealthGroup.MapPost("/visits/{id:guid}/assign", AssignHomeHealthTeamAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AssignHomeHealthTeam")
            .Produces<HomeHealthVisitResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        homeHealthGroup.MapPost("/visits/{id:guid}/start", StartHomeHealthVisitAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("StartHomeHealthVisit")
            .Produces<HomeHealthVisitResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        homeHealthGroup.MapPost("/visits/{id:guid}/complete", CompleteHomeHealthVisitAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteHomeHealthVisit")
            .Produces<HomeHealthVisitResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        homeHealthGroup.MapPost("/visits/{id:guid}/cancel", CancelHomeHealthVisitAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelHomeHealthVisit")
            .Produces<HomeHealthVisitResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        specialtyGroup.MapGet("/patient-portal/my-records", GetMyPublishedSpecialtyRecordsAsync)
            .RequirePermission(HospitalPermissions.SpecialtyCare.SpecialtyCareViewOwn)
            .WithName("GetMyPublishedSpecialtyRecords")
            .Produces<PatientSpecialtyPortalResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var reportsGroup = specialtyGroup.MapGroup("/reports");

        reportsGroup.MapGet("/operational-summary", GetSpecialtyOperationalSummaryAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.ReportOperationsView)
            .WithName("GetSpecialtyOperationalSummary")
            .Produces<SpecialtyOperationalSummaryResponse>();

        return endpoints;
    }

    private static async Task<IResult> CreatePregnancyEpisodeAsync(
        IPregnancyTrackingService service,
        IEncounterReferenceLookup encounterLookup,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreatePregnancyEpisodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                request.PatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var encounterValidation = await ValidateEncounterReferenceAsync(
            encounterLookup,
            request.OpeningEncounterId,
            request.PatientId,
            "OpeningEncounterId",
            cancellationToken);
        if (encounterValidation is not null)
        {
            return encounterValidation;
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<PregnancyRiskCategory>(request.RiskCategory, out var risk))
        {
            return InvalidEnum("RiskCategory", request.RiskCategory);
        }

        if (!await accessControl.IsValidPregnancyTeamAsync(
                request.AssignedDoctorId,
                request.AssignedMidwifeId,
                cancellationToken))
        {
            return InvalidStaffAssignment("AssignedDoctorId/AssignedMidwifeId");
        }

        var dto = new CreatePregnancyEpisodeDto(
            request.PatientId,
            request.OpeningEncounterId,
            request.Gravida,
            request.Para,
            request.Abortus,
            request.LivingChildren,
            request.LastMenstrualPeriodUtc,
            request.EstimatedDeliveryDateUtc,
            request.BloodGroupAndRh,
            risk,
            request.RiskFactorsNotes,
            request.AssignedDoctorId,
            request.AssignedMidwifeId);

        var result = await service.CreateEpisodeAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, e => Results.Created($"/api/v1/specialty/pregnancy-episodes/{e.Id}", MapEpisodeToResponse(e)));
    }

    private static async Task<IResult> RecordAntenatalVisitAsync(
        IPregnancyTrackingService service,
        IEncounterReferenceLookup encounterLookup,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RecordAntenatalVisitRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPregnancyEpisodeAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var episode = await service.GetEpisodeByIdAsync(id, cancellationToken);
        if (episode is null)
        {
            return Results.NotFound();
        }

        var encounterValidation = await ValidateEncounterReferenceAsync(
            encounterLookup,
            request.EncounterId,
            episode.PatientId,
            "EncounterId",
            cancellationToken);
        if (encounterValidation is not null)
        {
            return encounterValidation;
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<FetalPresentation>(request.FetalPresentation, out var presentation))
        {
            return InvalidEnum("FetalPresentation", request.FetalPresentation);
        }

        if (!TryParseDefinedEnum<EdemaLevel>(request.EdemaLevel, out var edema))
        {
            return InvalidEnum("EdemaLevel", request.EdemaLevel);
        }

        var dto = new RecordAntenatalVisitDto(
            id,
            request.EncounterId,
            request.VisitDateUtc,
            request.GestationalAgeWeeks,
            request.GestationalAgeDays,
            request.MaternalWeightKg,
            request.SystolicBpMmHg,
            request.DiastolicBpMmHg,
            request.FundalHeightCm,
            request.FetalHeartRateBpm,
            presentation,
            edema,
            request.UrineProteinPresent,
            request.UrineGlucosePresent,
            request.ClinicalNotes,
            request.NextVisitRecommendedDateUtc);

        var result = await service.RecordAntenatalVisitAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Created($"/api/v1/specialty/pregnancy-episodes/{id}/antenatal-visits/{v.Id}", MapVisitToResponse(v)));
    }

    private static async Task<IResult> UpdateRiskCategoryAsync(
        IPregnancyTrackingService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdatePregnancyRiskCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPregnancyEpisodeAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<PregnancyRiskCategory>(request.RiskCategory, out var risk))
        {
            return InvalidEnum("RiskCategory", request.RiskCategory);
        }

        var result = await service.UpdateRiskCategoryAsync(id, risk, request.RiskFactorsNotes, staffId, cancellationToken);
        return ToHttpResult(result, e => Results.Ok(MapEpisodeToResponse(e)));
    }

    private static async Task<IResult> CompleteEpisodeAsync(
        IPregnancyTrackingService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CompletePregnancyEpisodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPregnancyEpisodeAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<PregnancyEpisodeStatus>(request.OutcomeStatus, out var status)
            || status == PregnancyEpisodeStatus.Active)
        {
            return InvalidEnum("OutcomeStatus", request.OutcomeStatus);
        }

        var result = await service.CompleteEpisodeAsync(id, status, staffId, cancellationToken);
        return ToHttpResult(result, e => Results.Ok(MapEpisodeToResponse(e)));
    }

    private static async Task<IResult> GetActivePregnancyEpisodesAsync(
        IPregnancyTrackingService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var list = await service.GetActiveEpisodesAsync(cancellationToken);
        var visible = new List<PregnancyEpisodeResponse>();
        foreach (var episode in list)
        {
            if (await accessControl.CanAccessPatientAsync(
                    user,
                    episode.PatientId,
                    HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                    cancellationToken,
                    episode.AssignedDoctorId,
                    episode.AssignedMidwifeId))
            {
                visible.Add(MapEpisodeToResponse(episode));
            }
        }

        return Results.Ok(visible);
    }

    private static async Task<IResult> GetPregnancyEpisodesByPatientIdAsync(
        IPregnancyTrackingService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var list = await service.GetEpisodesByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(list.Select(MapEpisodeToResponse).ToList());
    }

    private static async Task<IResult> GetPregnancyEpisodeByIdAsync(
        IPregnancyTrackingService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPregnancyEpisodeAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var episode = await service.GetEpisodeByIdAsync(id, cancellationToken);
        return episode is null ? Results.NotFound() : Results.Ok(MapEpisodeToResponse(episode));
    }

    private static PregnancyEpisodeResponse MapEpisodeToResponse(PregnancyEpisodeDto e) =>
        new(
            e.Id,
            e.PatientId,
            e.OpeningEncounterId,
            e.EpisodeProtocolNumber,
            e.Gravida,
            e.Para,
            e.Abortus,
            e.LivingChildren,
            e.LastMenstrualPeriodUtc,
            e.EstimatedDeliveryDateUtc,
            e.BloodGroupAndRh,
            e.RiskCategory.ToString(),
            e.RiskFactorsNotes,
            e.Status.ToString(),
            e.AssignedDoctorId,
            e.AssignedMidwifeId,
            e.CreatedAtUtc,
            e.UpdatedAtUtc,
            e.AntenatalVisits.Select(MapVisitToResponse).ToList());

    private static async Task<IResult> CreateDeliveryRecordAsync(
        IDeliveryRecordService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateDeliveryRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                request.MotherPatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<DeliveryMode>(request.DeliveryMode, out var mode))
        {
            return InvalidEnum("DeliveryMode", request.DeliveryMode);
        }

        if (!TryParseDefinedEnum<PerinealTearDegree>(request.PerinealTear, out var tear))
        {
            return InvalidEnum("PerinealTear", request.PerinealTear);
        }

        if (!await accessControl.IsValidDeliveryTeamAsync(
                request.AttendingDoctorId,
                request.AssistingMidwifeId,
                request.PediatricianDoctorId,
                cancellationToken))
        {
            return InvalidStaffAssignment("DeliveryTeam");
        }

        if (!await accessControl.AreValidNewbornPatientsAsync(
                request.MotherPatientId,
                request.Newborns.Select(newborn => newborn.NewbornPatientId),
                cancellationToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Newborns.NewbornPatientId"] = ["Her yenidoğan için aktif, anneden farklı, benzersiz ve daha önce bağlanmamış bir Patient kimliği zorunludur."],
            });
        }

        var newborns = new List<AddNewbornDto>(request.Newborns.Count);
        foreach (var newborn in request.Newborns)
        {
            if (!TryParseDefinedEnum<NewbornGender>(newborn.Gender, out var gender))
            {
                return InvalidEnum("Newborns.Gender", newborn.Gender);
            }

            if (!TryParseDefinedEnum<ResuscitationIntervention>(newborn.ResuscitationGiven, out var resuscitation))
            {
                return InvalidEnum("Newborns.ResuscitationGiven", newborn.ResuscitationGiven);
            }

            newborns.Add(new AddNewbornDto(
                newborn.NewbornPatientId,
                newborn.BirthOrder,
                newborn.BirthTimeUtc,
                gender,
                newborn.BirthWeightGrams,
                newborn.BirthLengthCm,
                newborn.HeadCircumferenceCm,
                newborn.ApgarScore1Min,
                newborn.ApgarScore5Min,
                newborn.ApgarScore10Min,
                resuscitation,
                newborn.CordBloodPh,
                newborn.ComplicationsNotes));
        }

        var dto = new CreateDeliveryRecordDto(
            request.PregnancyEpisodeId,
            request.MotherPatientId,
            request.EncounterId,
            mode,
            request.DeliveryTimeUtc,
            request.GestationalAgeWeeks,
            request.GestationalAgeDays,
            tear,
            request.EstimatedBloodLossMl,
            request.AttendingDoctorId,
            request.AssistingMidwifeId,
            request.PediatricianDoctorId,
            request.MaternalComplicationsNotes,
            request.DeliverySummaryNotes,
            newborns);

        var result = await service.CreateDeliveryRecordAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, d => Results.Created($"/api/v1/specialty/deliveries/{d.Id}", MapDeliveryToResponse(d)));
    }

    private static async Task<IResult> GetDeliveryRecordByIdAsync(
        IDeliveryRecordService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessDeliveryAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var delivery = await service.GetDeliveryRecordByIdAsync(id, cancellationToken);
        return delivery is null
            ? Results.NotFound()
            : Results.Ok(MapDeliveryToResponse(delivery));
    }

    private static async Task<IResult> GetDeliveryRecordsByMotherPatientIdAsync(
        IDeliveryRecordService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid motherPatientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                motherPatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var deliveries = await service.GetDeliveryRecordsByMotherPatientIdAsync(motherPatientId, cancellationToken);
        return Results.Ok(deliveries.Select(MapDeliveryToResponse).ToList());
    }

    private static async Task<IResult> AddNewbornAsync(
        IDeliveryRecordService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AddNewbornRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessDeliveryAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<NewbornGender>(request.Gender, out var gender))
        {
            return InvalidEnum("Gender", request.Gender);
        }

        if (!TryParseDefinedEnum<ResuscitationIntervention>(request.ResuscitationGiven, out var resus))
        {
            return InvalidEnum("ResuscitationGiven", request.ResuscitationGiven);
        }

        if (!await accessControl.IsValidNewbornPatientForDeliveryAsync(
                id,
                request.NewbornPatientId,
                cancellationToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["NewbornPatientId"] = ["Aktif, anneden farklı ve daha önce bağlanmamış bir yenidoğan Patient kimliği zorunludur."],
            });
        }

        var dto = new AddNewbornDto(
            request.NewbornPatientId,
            request.BirthOrder,
            request.BirthTimeUtc,
            gender,
            request.BirthWeightGrams,
            request.BirthLengthCm,
            request.HeadCircumferenceCm,
            request.ApgarScore1Min,
            request.ApgarScore5Min,
            request.ApgarScore10Min,
            resus,
            request.CordBloodPh,
            request.ComplicationsNotes);

        var result = await service.AddNewbornAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, nb => Results.Created($"/api/v1/specialty/deliveries/{id}/newborns/{nb.Id}", MapNewbornToResponse(nb)));
    }

    private static DeliveryRecordResponse MapDeliveryToResponse(DeliveryRecordDto d) =>
        new(
            d.Id,
            d.PregnancyEpisodeId,
            d.MotherPatientId,
            d.EncounterId,
            d.DeliveryProtocolNumber,
            d.DeliveryMode.ToString(),
            d.DeliveryTimeUtc,
            d.GestationalAgeWeeks,
            d.GestationalAgeDays,
            d.PerinealTear.ToString(),
            d.EstimatedBloodLossMl,
            d.AttendingDoctorId,
            d.AssistingMidwifeId,
            d.PediatricianDoctorId,
            d.MaternalComplicationsNotes,
            d.DeliverySummaryNotes,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Newborns.Select(MapNewbornToResponse).ToList());

    private static NewbornResponse MapNewbornToResponse(NewbornDto n) =>
        new(
            n.Id,
            n.DeliveryRecordId,
            n.NewbornPatientId,
            n.BirthOrder,
            n.BirthTimeUtc,
            n.Gender.ToString(),
            n.BirthWeightGrams,
            n.BirthLengthCm,
            n.HeadCircumferenceCm,
            n.ApgarScore1Min,
            n.ApgarScore5Min,
            n.ApgarScore10Min,
            n.ResuscitationGiven.ToString(),
            n.CordBloodPh,
            n.ComplicationsNotes,
            n.CreatedAtUtc);

    private static async Task<IResult> GetLatestOdontogramAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var teeth = await service.GetLatestOdontogramByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(teeth.Select(MapToothConditionToResponse).ToList());
    }

    private static async Task<IResult> RecordToothConditionAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        [FromBody] RecordToothConditionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<ToothCondition>(request.Condition, out var condition))
        {
            return InvalidEnum("Condition", request.Condition);
        }

        if (!TryParseToothSurfaces(request.AffectedSurfaces, out var surfaces))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["AffectedSurfaces"] = ["Geçersiz diş yüzeyi bit maskesi."],
            });
        }

        var dto = new RecordToothConditionDto(
            patientId,
            request.ToothNumber,
            condition,
            surfaces,
            request.Notes);

        var result = await service.RecordToothConditionAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, t => Results.Created($"/api/v1/specialty/dental/odontogram/{patientId}/tooth/{t.ToothNumber}", MapToothConditionToResponse(t)));
    }

    private static async Task<IResult> GetToothHistoryAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        int toothNumber,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var history = await service.GetToothHistoryAsync(patientId, toothNumber, cancellationToken);
        return Results.Ok(history.Select(MapToothConditionToResponse).ToList());
    }

    private static async Task<IResult> GetDentalProceduresAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var procedures = await service.GetProceduresByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(procedures.Select(MapDentalProcedureToResponse).ToList());
    }

    private static async Task<IResult> PlanDentalProcedureAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] PlanDentalProcedureRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                request.PatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        if (!TryParseToothSurfaces(request.Surfaces, out var surfaces))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Surfaces"] = ["Geçersiz diş yüzeyi bit maskesi."],
            });
        }

        if (!await accessControl.IsValidDentistAsync(
                request.PerformedByDoctorId,
                cancellationToken))
        {
            return InvalidStaffAssignment("PerformedByDoctorId");
        }

        var dto = new PlanDentalProcedureDto(
            request.PatientId,
            request.EncounterId,
            request.ToothNumber,
            surfaces,
            request.ProcedureCode,
            request.ProcedureName,
            request.EstimatedCost,
            request.PerformedByDoctorId,
            request.ScheduledDateUtc,
            request.ClinicalNotes);

        var result = await service.PlanProcedureAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, p => Results.Created($"/api/v1/specialty/dental/procedures/{p.Id}", MapDentalProcedureToResponse(p)));
    }

    private static async Task<IResult> CompleteDentalProcedureAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CompleteDentalProcedureRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessDentalProcedureAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.CompleteProcedureAsync(id, request.CompletedDateUtc, request.CompletionNotes, staffId, cancellationToken);
        return ToHttpResult(result, p => Results.Ok(MapDentalProcedureToResponse(p)));
    }

    private static async Task<IResult> GetDentalExaminationsAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var exams = await service.GetExaminationsByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(exams.Select(MapDentalExaminationToResponse).ToList());
    }

    private static async Task<IResult> CreateDentalExaminationAsync(
        IDentalCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateDentalExaminationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                request.PatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!await accessControl.IsValidDentistAsync(request.DentistId, cancellationToken))
        {
            return InvalidStaffAssignment("DentistId");
        }

        var dto = new CreateDentalExaminationDto(
            request.PatientId,
            request.EncounterId,
            request.DentistId,
            request.ExaminationDateUtc,
            request.ChiefComplaint,
            request.DiagnosisNotes,
            request.TreatmentPlanSummary);

        var result = await service.CreateExaminationAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, e => Results.Created($"/api/v1/specialty/dental/examinations/{e.Id}", MapDentalExaminationToResponse(e)));
    }

    private static ToothConditionResponse MapToothConditionToResponse(ToothConditionDto t) =>
        new(
            t.Id,
            t.PatientId,
            t.ToothNumber,
            t.Condition.ToString(),
            (int)t.AffectedSurfaces,
            t.Notes,
            t.RecordedAtUtc,
            t.RecordedByStaffId,
            t.Version);

    private static DentalProcedureResponse MapDentalProcedureToResponse(DentalProcedureDto p) =>
        new(
            p.Id,
            p.PatientId,
            p.EncounterId,
            p.ProcedureProtocolNumber,
            p.ToothNumber,
            (int)p.Surfaces,
            p.ProcedureCode,
            p.ProcedureName,
            p.Status.ToString(),
            p.EstimatedCost,
            p.PerformedByDoctorId,
            p.ScheduledDateUtc,
            p.CompletedDateUtc,
            p.ClinicalNotes,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);

    private static DentalExaminationResponse MapDentalExaminationToResponse(DentalExaminationDto e) =>
        new(
            e.Id,
            e.PatientId,
            e.EncounterId,
            e.ExaminationProtocolNumber,
            e.DentistId,
            e.ExaminationDateUtc,
            e.ChiefComplaint,
            e.DiagnosisNotes,
            e.TreatmentPlanSummary,
            e.CreatedAtUtc);

    private static async Task<IResult> RequestHomeHealthVisitAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] RequestHomeHealthVisitRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                request.PatientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!TryParseDefinedEnum<HomeCareServiceType>(request.ServiceType, out var serviceType))
        {
            return InvalidEnum("ServiceType", request.ServiceType);
        }

        if (!TryParseDefinedEnum<HomeVisitPriority>(request.Priority, out var priority))
        {
            return InvalidEnum("Priority", request.Priority);
        }

        var dto = new RequestHomeHealthVisitDto(
            request.PatientId,
            serviceType,
            priority,
            request.City,
            request.District,
            request.AddressDetail,
            request.ContactPhone,
            request.InitialNotes);

        var result = await service.RequestVisitAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Created(
            $"/api/v1/specialty/home-health/visits/{v.Id}",
            MapHomeHealthVisitToResponse(v, SpecialtyCareAccessControl.CanViewHomeAddress(user, v))));
    }

    private static async Task<IResult> GetActiveHomeHealthVisitsAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var visits = await service.GetActiveVisitsAsync(cancellationToken);
        var visible = new List<HomeHealthVisitResponse>();
        foreach (var visit in visits)
        {
            if (await accessControl.CanAccessPatientAsync(
                    user,
                    visit.PatientId,
                    HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                    cancellationToken,
                    visit.RequestedByStaffId,
                    visit.AssignedStaffId))
            {
                visible.Add(MapHomeHealthVisitToResponse(
                    visit,
                    SpecialtyCareAccessControl.CanViewHomeAddress(user, visit)));
            }
        }

        return Results.Ok(visible);
    }

    private static async Task<IResult> GetHomeHealthVisitByIdAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessHomeHealthVisitAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var visit = await service.GetVisitByIdAsync(id, cancellationToken);
        return visit is null
            ? Results.NotFound()
            : Results.Ok(MapHomeHealthVisitToResponse(
                visit,
                SpecialtyCareAccessControl.CanViewHomeAddress(user, visit)));
    }

    private static async Task<IResult> GetHomeHealthVisitsByPatientIdAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPatientAsync(
                user,
                patientId,
                HospitalPermissions.SpecialtyCare.SpecialtyCareView,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var visits = await service.GetVisitsByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(visits
            .Select(visit => MapHomeHealthVisitToResponse(
                visit,
                SpecialtyCareAccessControl.CanViewHomeAddress(user, visit)))
            .ToList());
    }

    private static async Task<IResult> AssignHomeHealthTeamAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AssignHomeHealthTeamRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessHomeHealthVisitAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        if (!await accessControl.IsValidHomeHealthAssigneeAsync(
                request.AssignedStaffId,
                cancellationToken))
        {
            return InvalidStaffAssignment("AssignedStaffId");
        }

        var result = await service.AssignTeamAsync(id, request.AssignedStaffId, request.ScheduledDateUtc, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Ok(MapHomeHealthVisitToResponse(
            v,
            SpecialtyCareAccessControl.CanViewHomeAddress(user, v))));
    }

    private static async Task<IResult> StartHomeHealthVisitAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.IsAssignedHomeHealthStaffAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.StartVisitAsync(id, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Ok(MapHomeHealthVisitToResponse(v, includeAddress: true)));
    }

    private static async Task<IResult> CompleteHomeHealthVisitAsync(
        IHomeHealthCareService service,
        IEncounterReferenceLookup encounterLookup,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CompleteHomeHealthVisitRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.IsAssignedHomeHealthStaffAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var visit = await service.GetVisitByIdAsync(id, cancellationToken);
        if (visit is null)
        {
            return Results.NotFound();
        }

        var encounterValidation = await ValidateHomeHealthEncounterReferenceAsync(
            encounterLookup,
            request.EncounterId,
            visit.PatientId,
            cancellationToken);
        if (encounterValidation is not null)
        {
            return encounterValidation;
        }

        if (!await accessControl.IsHomeHealthEncounterAvailableAsync(
                id,
                request.EncounterId,
                cancellationToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["EncounterId"] = ["Karşılaşma başka bir evde sağlık ziyaretine bağlanmıştır."],
            });
        }

        var staffId = ExtractActorId(user);
        var result = await service.CompleteVisitAsync(id, request.ClinicalNotes, request.VitalsSummaryNotes, request.EncounterId, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Ok(MapHomeHealthVisitToResponse(v, includeAddress: true)));
    }

    private static async Task<IResult> CancelHomeHealthVisitAsync(
        IHomeHealthCareService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CancelHomeHealthVisitRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessHomeHealthVisitAsync(
                user,
                id,
                HospitalPermissions.SpecialtyCare.SpecialtyCareRecord,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.CancelVisitAsync(id, request.Reason, staffId, cancellationToken);
        return ToHttpResult(result, v => Results.Ok(MapHomeHealthVisitToResponse(
            v,
            SpecialtyCareAccessControl.CanViewHomeAddress(user, v))));
    }

    private static HomeHealthVisitResponse MapHomeHealthVisitToResponse(
        HomeHealthVisitDto v,
        bool includeAddress) =>
        new(
            v.Id,
            v.PatientId,
            v.EncounterId,
            v.ProtocolNumber,
            v.ServiceType.ToString(),
            v.Priority.ToString(),
            v.Status.ToString(),
            v.RequestedDateUtc,
            v.ScheduledDateUtc,
            v.VisitStartedAtUtc,
            v.VisitCompletedAtUtc,
            includeAddress ? v.City : string.Empty,
            includeAddress ? v.District : string.Empty,
            includeAddress ? v.AddressDetail : string.Empty,
            includeAddress ? v.ContactPhone : string.Empty,
            v.RequestedByStaffId,
            v.AssignedStaffId,
            v.ClinicalNotes,
            v.VitalsSummaryNotes,
            v.CreatedAtUtc,
            v.UpdatedAtUtc);

    private static async Task<IResult> GetSpecialtyOperationalSummaryAsync(
        ISpecialtyReportingService service,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var s = await service.GetOperationalSummaryAsync(startDate, endDate, cancellationToken);
        var response = new SpecialtyOperationalSummaryResponse(
            s.ActivePregnanciesCount,
            s.HighRiskPregnanciesCount,
            s.TotalDeliveriesCount,
            s.CesareanDeliveriesCount,
            s.NormalDeliveriesCount,
            s.TotalDentalProceduresCount,
            s.CompletedDentalProceduresCount,
            s.PlannedDentalProceduresCount,
            s.TotalDentalExaminationsCount,
            s.ActiveHomeVisitsCount,
            s.PendingHomeVisitRequestsCount,
            s.AssignedHomeVisitsCount,
            s.CompletedHomeVisitsCount,
            s.UrgentHomeVisitsCount,
            s.GeneratedAtUtc);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetMyPublishedSpecialtyRecordsAsync(
        IPatientSpecialtyPortalService service,
        SpecialtyCareAccessControl accessControl,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken)
    {
        var patientId = await accessControl.GetOwnPatientIdAsync(
            actor,
            HospitalPermissions.SpecialtyCare.SpecialtyCareViewOwn,
            cancellationToken);

        if (!patientId.HasValue)
        {
            return Results.Forbid();
        }

        var summary = await service.GetPublishedRecordsAsync(
            patientId.Value,
            ExtractActorId(actor),
            cancellationToken);

        return Results.Ok(MapPatientSpecialtyPortalResponse(summary));
    }

    private static PatientSpecialtyPortalResponse MapPatientSpecialtyPortalResponse(
        PatientSpecialtyPortalDto summary) =>
        new(
            summary.Pregnancies.Select(item => new PatientPregnancySummaryResponse(
                item.Id, item.ProtocolNumber, item.Status, item.EstimatedDeliveryDateUtc,
                item.VisitCount, item.LastVisitDateUtc)).ToList(),
            summary.Deliveries.Select(item => new PatientDeliverySummaryResponse(
                item.Id, item.ProtocolNumber, item.DeliveryMode, item.DeliveryTimeUtc,
                item.GestationalAgeWeeks, item.GestationalAgeDays, item.NewbornCount)).ToList(),
            summary.DentalExaminations.Select(item => new PatientDentalExaminationSummaryResponse(
                item.Id, item.ProtocolNumber, item.ExaminationDateUtc)).ToList(),
            summary.DentalProcedures.Select(item => new PatientDentalProcedureSummaryResponse(
                item.Id, item.ProtocolNumber, item.ToothNumber, item.ProcedureName,
                item.Status, item.CompletedDateUtc)).ToList(),
            summary.HomeHealthVisits.Select(item => new PatientHomeHealthVisitSummaryResponse(
                item.Id, item.ProtocolNumber, item.ServiceType, item.Priority, item.Status,
                item.RequestedDateUtc, item.ScheduledDateUtc, item.VisitCompletedAtUtc,
                item.City, item.District)).ToList(),
            summary.GeneratedAtUtc);

    private static AntenatalVisitResponse MapVisitToResponse(AntenatalVisitDto v) =>
        new(
            v.Id,
            v.PregnancyEpisodeId,
            v.EncounterId,
            v.VisitDateUtc,
            v.GestationalAgeWeeks,
            v.GestationalAgeDays,
            v.MaternalWeightKg,
            v.SystolicBpMmHg,
            v.DiastolicBpMmHg,
            v.FundalHeightCm,
            v.FetalHeartRateBpm,
            v.FetalPresentation.ToString(),
            v.EdemaLevel.ToString(),
            v.UrineProteinPresent,
            v.UrineGlucosePresent,
            v.StaffId,
            v.ClinicalNotes,
            v.NextVisitRecommendedDateUtc,
            v.CreatedAtUtc);

    private static async Task<IResult?> ValidateEncounterReferenceAsync(
        IEncounterReferenceLookup encounterLookup,
        Guid encounterId,
        Guid expectedPatientId,
        string propertyName,
        CancellationToken cancellationToken)
    {
        if (encounterId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [propertyName] = ["Randevuya bağlı klinik karşılaşma seçilmelidir."],
            });
        }

        var encounter = await encounterLookup.FindAsync(encounterId, cancellationToken);
        if (encounter is null
            || encounter.PatientId != expectedPatientId
            || !encounter.AppointmentId.HasValue
            || encounter.AppointmentId.Value == Guid.Empty
            || encounter.Status is EncounterStatus.Cancelled or EncounterStatus.EnteredInError)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [propertyName] = ["Karşılaşma geçerli, aynı hastaya ait ve bir randevuya bağlı olmalıdır."],
            });
        }

        return null;
    }

    private static async Task<IResult?> ValidateHomeHealthEncounterReferenceAsync(
        IEncounterReferenceLookup encounterLookup,
        Guid encounterId,
        Guid expectedPatientId,
        CancellationToken cancellationToken)
    {
        if (encounterId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["EncounterId"] = ["Evde sağlık klinik karşılaşması seçilmelidir."],
            });
        }

        var encounter = await encounterLookup.FindAsync(encounterId, cancellationToken);
        if (encounter is null
            || encounter.PatientId != expectedPatientId
            || encounter.EncounterType != EncounterType.HomeHealth
            || encounter.Status is not (EncounterStatus.InProgress or EncounterStatus.Completed or EncounterStatus.Amended))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["EncounterId"] = ["Karşılaşma aynı hastaya ait, evde sağlık türünde ve başlatılmış veya tamamlanmış olmalıdır."],
            });
        }

        return null;
    }

    private static bool TryParseDefinedEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out parsed)
            && Enum.IsDefined(parsed);
    }

    private static bool TryParseToothSurfaces(int value, out ToothSurface surfaces)
    {
        const int allowedMask = (int)(ToothSurface.Mesial
            | ToothSurface.Distal
            | ToothSurface.Occlusal
            | ToothSurface.Buccal
            | ToothSurface.Lingual);

        surfaces = (ToothSurface)value;
        return value >= 0 && (value & ~allowedMask) == 0;
    }

    private static IResult InvalidEnum(string field, string? value) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [$"'{value ?? string.Empty}' geçerli bir {field} değeri değildir."],
        });

    private static IResult InvalidStaffAssignment(string field) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = ["Atanan personel aktif değildir veya bu klinik sorumluluk için uygun meslekte değildir."],
        });

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

    private static IResult ToHttpResult<T>(SpecialtyOperationResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            SpecialtyOperationStatus.Success => onSuccess(result.Value!),
            SpecialtyOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage,
                extensions: new Dictionary<string, object?> { ["errorDetail"] = result.ErrorMessage }),
            SpecialtyOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage,
                extensions: new Dictionary<string, object?> { ["errorDetail"] = result.ErrorMessage }),
            SpecialtyOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors?.Count > 0
                    ? result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value)
                    : new Dictionary<string, string[]> { ["General"] = [result.ErrorMessage ?? "Doğrulama hatası."] }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
