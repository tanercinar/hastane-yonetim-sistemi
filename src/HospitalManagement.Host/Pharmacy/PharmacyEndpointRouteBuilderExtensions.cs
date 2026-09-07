using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Pharmacy;

public static class PharmacyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPharmacyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var pharmacyGroup = endpoints.MapGroup("/api/v1/pharmacy")
            .RequireAuthorization();

        // 1. Medication Catalog Endpoints
        var medicationGroup = pharmacyGroup.MapGroup("/medications");

        medicationGroup.MapGet(string.Empty, SearchMedicationsAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.MedicationCatalogView)
            .WithName("SearchMedicationCatalog")
            .Produces<List<MedicationCatalogItemResponse>>()
            .ProducesValidationProblem();

        medicationGroup.MapGet("/{id:guid}", GetMedicationByIdAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.MedicationCatalogView)
            .WithName("GetMedicationCatalogItemById")
            .Produces<MedicationCatalogItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        medicationGroup.MapPost("/import", ImportMedicationsAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.MedicationCatalogManage)
            .WithName("ImportMedicationCatalog")
            .Produces<MedicationCatalogImportResultResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 2. Prescription Endpoints
        var prescriptionGroup = pharmacyGroup.MapGroup("/prescriptions");

        prescriptionGroup.MapPost(string.Empty, CreatePrescriptionDraftAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionCreate)
            .WithName("CreatePrescriptionDraft")
            .Produces<PrescriptionDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        prescriptionGroup.MapGet("/{id:guid}", GetPrescriptionByIdAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionView)
            .WithName("GetPrescriptionById")
            .Produces<PrescriptionDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        prescriptionGroup.MapGet("/by-encounter/{encounterId:guid}", GetPrescriptionsByEncounterAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionView)
            .WithName("GetPrescriptionsByEncounter")
            .Produces<List<PrescriptionSummaryResponse>>()
            .ProducesValidationProblem();

        prescriptionGroup.MapGet("/worklist", GetPrescriptionWorklistAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionView)
            .WithName("GetPrescriptionWorklist")
            .Produces<List<PrescriptionSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        prescriptionGroup.MapPost("/safety-check", CheckMedicationSafetyAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionCreate)
            .WithName("CheckMedicationSafety")
            .Produces<MedicationSafetyCheckResponse>()
            .ProducesValidationProblem();

        prescriptionGroup.MapGet("/by-patient/{patientId:guid}", GetPrescriptionsByPatientAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionView)
            .WithName("GetPrescriptionsByPatient")
            .Produces<List<PrescriptionSummaryResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        prescriptionGroup.MapPut("/{id:guid}", UpdatePrescriptionDraftAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionCreate)
            .WithName("UpdatePrescriptionDraft")
            .Produces<PrescriptionDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        prescriptionGroup.MapPost("/{id:guid}/sign", SignPrescriptionAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionSign)
            .WithName("SignPrescription")
            .Produces<PrescriptionDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        prescriptionGroup.MapPost("/{id:guid}/cancel", CancelPrescriptionAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionCancel)
            .WithName("CancelPrescription")
            .Produces<PrescriptionDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        prescriptionGroup.MapPost("/{id:guid}/entered-in-error", MarkPrescriptionEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionCancel)
            .WithName("MarkPrescriptionEnteredInError")
            .Produces<PrescriptionDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        prescriptionGroup.MapPost("/{id:guid}/dispense", DispensePrescriptionAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.PrescriptionDispense)
            .WithName("DispensePrescription")
            .Produces<PrescriptionDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 3. Inventory Stock Endpoints
        var inventoryGroup = pharmacyGroup.MapGroup("/inventory");

        inventoryGroup.MapGet("/stock", GetStockOverviewAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.InventoryPharmacyView)
            .WithName("GetPharmacyStockOverview")
            .Produces<List<MedicationStockItemResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        inventoryGroup.MapGet("/fefo-candidates/{medicationCatalogItemId:guid}", GetFefoCandidatesAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.InventoryPharmacyView)
            .WithName("GetFefoCandidates")
            .Produces<List<FefoCandidateStockResponse>>()
            .ProducesValidationProblem();

        inventoryGroup.MapPost("/adjust", AdjustStockAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Pharmacy.InventoryPharmacyAdjust)
            .WithName("AdjustPharmacyStock")
            .Produces<MedicationStockItemResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        inventoryGroup.MapGet("/stock/{stockItemId:guid}/transactions", GetStockTransactionsAsync)
            .RequirePermission(HospitalPermissions.Pharmacy.InventoryPharmacyView)
            .WithName("GetStockTransactions")
            .Produces<List<MedicationStockTransactionResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> SearchMedicationsAsync(
        [FromQuery] string? query,
        [FromQuery] string? route,
        [FromQuery] string? form,
        [FromQuery] bool? isActive,
        [FromQuery] int? maxResults,
        [FromServices] IMedicationCatalogService service,
        CancellationToken cancellationToken)
    {
        MedicationRoute? routeEnum = null;
        if (!string.IsNullOrWhiteSpace(route))
        {
            if (!Enum.TryParse<MedicationRoute>(route, ignoreCase: true, out var parsedRoute))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["route"] = [$"Geçersiz uygulama yolu: '{route}'."],
                });
            }

            routeEnum = parsedRoute;
        }

        MedicationForm? formEnum = null;
        if (!string.IsNullOrWhiteSpace(form))
        {
            if (!Enum.TryParse<MedicationForm>(form, ignoreCase: true, out var parsedForm))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["form"] = [$"Geçersiz ilaç formu: '{form}'."],
                });
            }

            formEnum = parsedForm;
        }

        var results = await service.SearchMedicationsAsync(
            query,
            routeEnum,
            formEnum,
            isActive ?? true,
            maxResults ?? 50,
            cancellationToken);

        var responses = results.Select(MapToMedicationResponse).ToList();
        return Results.Ok(responses);
    }

    private static async Task<IResult> GetMedicationByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IMedicationCatalogService service,
        CancellationToken cancellationToken)
    {
        var item = await service.GetMedicationByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return Results.Problem(
                title: "İlaç Bulunamadı",
                detail: $"'{id}' kimlikli ilaç kataloğu kaydı bulunamadı.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(MapToMedicationResponse(item));
    }

    private static async Task<IResult> ImportMedicationsAsync(
        ClaimsPrincipal actor,
        [FromBody] ImportMedicationCatalogRequest request,
        [FromServices] IMedicationCatalogService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.CatalogVersion))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["catalogVersion"] = ["Katalog sürümü zorunludur."],
            });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["İçe aktarılacak en az bir ilaç kaydı bulunmalıdır."],
            });
        }

        var importDtos = new List<MedicationCatalogItemImportDto>(request.Items.Count);
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var prefix = $"items[{i}]";

            if (string.IsNullOrWhiteSpace(item.Code))
            {
                errors[$"{prefix}.code"] = ["İlaç kodu zorunludur."];
            }

            if (string.IsNullOrWhiteSpace(item.BrandName))
            {
                errors[$"{prefix}.brandName"] = ["İlaç ticari adı zorunludur."];
            }

            if (string.IsNullOrWhiteSpace(item.GenericName))
            {
                errors[$"{prefix}.genericName"] = ["İlaç etken madde adı zorunludur."];
            }

            if (!Enum.TryParse<MedicationForm>(item.Form, ignoreCase: true, out var parsedForm))
            {
                errors[$"{prefix}.form"] = [$"Geçersiz form: '{item.Form}'."];
            }

            if (item.StrengthValue <= 0)
            {
                errors[$"{prefix}.strengthValue"] = ["Doz değeri 0'dan büyük olmalıdır."];
            }

            if (string.IsNullOrWhiteSpace(item.StrengthUnit))
            {
                errors[$"{prefix}.strengthUnit"] = ["Doz birimi zorunludur."];
            }

            if (!Enum.TryParse<MedicationRoute>(item.Route, ignoreCase: true, out var parsedRoute))
            {
                errors[$"{prefix}.route"] = [$"Geçersiz uygulama yolu: '{item.Route}'."];
            }

            if (errors.Count == 0)
            {
                importDtos.Add(new MedicationCatalogItemImportDto(
                    item.Code,
                    item.BrandName,
                    item.GenericName,
                    parsedForm,
                    item.StrengthValue,
                    item.StrengthUnit,
                    parsedRoute,
                    item.AtcCode,
                    item.Description));
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await service.ImportCatalogAsync(
            actor,
            importDtos,
            request.CatalogVersion,
            cancellationToken);

        return Results.Ok(new MedicationCatalogImportResultResponse(
            result.CatalogVersion,
            result.TotalItems,
            result.InsertedCount,
            result.UpdatedCount));
    }

    private static async Task<IResult> CreatePrescriptionDraftAsync(
        ClaimsPrincipal actor,
        [FromBody] CreatePrescriptionDraftRequest request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var items = request.Items.Select(i => new CreatePrescriptionItemCommand(
            i.MedicationCatalogItemId,
            i.Dose,
            i.DoseUnit,
            i.Frequency,
            i.DurationDays,
            i.Quantity,
            i.QuantityUnit,
            i.Instructions)).ToList();

        var command = new CreatePrescriptionDraftCommand(
            request.EncounterId,
            request.PatientId,
            Guid.Empty,
            request.DepartmentId,
            request.DiagnosisSummary,
            request.GeneralInstructions,
            items);

        var result = await service.CreateDraftAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Created($"/api/v1/pharmacy/prescriptions/{r.Id}", MapToDetailResponse(r)));
    }

    private static async Task<IResult> GetPrescriptionByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(actor, id, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> GetPrescriptionsByEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByEncounterAsync(actor, encounterId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetPrescriptionsByPatientAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByPatientAsync(actor, patientId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetPrescriptionWorklistAsync(
        ClaimsPrincipal actor,
        [FromQuery] string? status,
        [FromQuery] string? prescriptionNumber,
        [FromQuery] Guid? patientId,
        [FromQuery] int? maxResults,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetWorklistAsync(
            actor,
            status,
            prescriptionNumber,
            patientId,
            maxResults ?? 50,
            cancellationToken);

        return ToHttpResult(result, r => Results.Ok(r.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> UpdatePrescriptionDraftAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdatePrescriptionDraftRequest request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var items = request.Items.Select(i => new CreatePrescriptionItemCommand(
            i.MedicationCatalogItemId,
            i.Dose,
            i.DoseUnit,
            i.Frequency,
            i.DurationDays,
            i.Quantity,
            i.QuantityUnit,
            i.Instructions)).ToList();

        var command = new UpdatePrescriptionDraftCommand(
            id,
            Guid.Empty,
            request.ExpectedVersion,
            request.DiagnosisSummary,
            request.GeneralInstructions,
            items);

        var result = await service.UpdateDraftAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> CheckMedicationSafetyAsync(
        ClaimsPrincipal actor,
        [FromBody] MedicationSafetyCheckRequest request,
        [FromServices] IMedicationSafetyChecker checker,
        [FromServices] IPrescriptionAccessContext accessContext,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!request.EncounterId.HasValue || request.EncounterId.Value == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["encounterId"] = ["Karşılaşma kimliği zorunludur."],
            });
        }

        var encounter = await accessContext.FindEncounterAsync(
            request.EncounterId.Value,
            cancellationToken);
        if (encounter is null)
        {
            return Results.Problem(
                title: "Kayıt Bulunamadı",
                detail: "Karşılaşma bulunamadı.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (encounter.PatientId != request.PatientId)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["patientId"] = ["Hasta kimliği karşılaşma ile eşleşmiyor."],
            });
        }

        if (!encounter.AllowsClinicalEntry)
        {
            return Results.Problem(
                title: "Geçersiz Karşılaşma Durumu",
                detail: "Güvenlik kontrolü yalnızca klinik girişe açık karşılaşmalarda çalıştırılabilir.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (!await accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Pharmacy.PrescriptionCreate,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return Results.Problem(
                title: "Yetkisiz Erişim",
                detail: "Bu karşılaşma için ilaç güvenliği kontrolü yapma yetkiniz bulunmamaktadır.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var candidates = request.Items.Select(i => new PrescriptionItemSafetyCandidate(
            i.MedicationCatalogItemId,
            i.Dose,
            i.DoseUnit,
            i.Frequency,
            i.DurationDays)).ToList();

        var result = await checker.CheckSafetyAsync(
            request.PatientId,
            candidates,
            null,
            cancellationToken);

        var warnings = result.Warnings.Select(w => new MedicationSafetyWarningResponse(
            w.WarningCode,
            w.Type.ToString(),
            w.Severity.ToString(),
            w.Title,
            w.Message,
            w.OffendingMedicationName,
            w.ConflictingItemName,
            w.RequiresOverrideReason)).ToList();

        return Results.Ok(new MedicationSafetyCheckResponse(
            result.HasWarnings,
            result.HasCriticalWarnings,
            warnings,
            result.Disclaimer));
    }

    private static async Task<IResult> SignPrescriptionAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] SignPrescriptionRequest? request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        var command = new SignPrescriptionCommand(
            id,
            Guid.Empty,
            request?.ExpectedVersion ?? 0,
            request?.ValidDays,
            request?.OverrideReason,
            request?.AcknowledgedWarningCodes);

        var result = await service.SignAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> CancelPrescriptionAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelPrescriptionRequest request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["İptal gerekçesi zorunludur."],
            });
        }

        var command = new CancelPrescriptionCommand(
            id,
            Guid.Empty,
            request.ExpectedVersion,
            request.Reason);

        var result = await service.CancelAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> MarkPrescriptionEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkPrescriptionEnteredInErrorRequest request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Hatalı giriş gerekçesi zorunludur."],
            });
        }

        var command = new MarkPrescriptionEnteredInErrorCommand(
            id,
            Guid.Empty,
            request.ExpectedVersion,
            request.Reason);

        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static async Task<IResult> DispensePrescriptionAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] DispensePrescriptionRequest request,
        [FromServices] IPrescriptionService service,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Items == null || request.Items.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["En az bir teslim kalemi belirtilmelidir."],
            });
        }

        var command = new DispensePrescriptionCommand(
            id,
            request.IdempotencyKey,
            request.ExpectedVersion,
            request.Items.Select(i => new DispenseItemCommand(
                i.ItemId,
                i.StockItemId,
                i.ExpectedStockVersion,
                i.Quantity,
                i.Notes)).ToList());

        var result = await service.DispenseAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToDetailResponse(r)));
    }

    private static IResult ToHttpResult<T>(
        PrescriptionOperationResult<T> result,
        Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            PrescriptionOperationStatus.Success => onSuccess(result.Value!),
            PrescriptionOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value)),
            PrescriptionOperationStatus.SafetyWarningOverrideRequired => Results.ValidationProblem(
                result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value),
                title: "Güvenlik Uyarısı Geçersiz Kılma Gerekçesi Gerekli",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status422UnprocessableEntity),
            PrescriptionOperationStatus.NotFound => Results.Problem(
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status404NotFound),
            PrescriptionOperationStatus.Conflict => Results.Problem(
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status409Conflict),
            PrescriptionOperationStatus.Forbidden => Results.Problem(
                title: "Yetkisiz Erişim",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.Problem(
                title: "İşlem Başarısız",
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static MedicationCatalogItemResponse MapToMedicationResponse(MedicationCatalogItemDto dto) =>
        new(
            dto.Id,
            dto.Code,
            dto.BrandName,
            dto.GenericName,
            dto.Form.ToString(),
            dto.StrengthValue,
            dto.StrengthUnit,
            dto.Route.ToString(),
            dto.AtcCode,
            dto.Description,
            dto.CatalogVersion,
            dto.IsActive,
            dto.CreatedAtUtc);

    private static PrescriptionDetailResponse MapToDetailResponse(PrescriptionDetailDto dto) =>
        new(
            dto.Id,
            dto.PrescriptionNumber,
            dto.PatientId,
            dto.EncounterId,
            dto.PrescribingDoctorId,
            dto.DepartmentId,
            dto.Status.ToString(),
            dto.ValidUntilUtc,
            dto.SignedAtUtc,
            dto.SignedByDoctorId,
            dto.CancelledAtUtc,
            dto.CancelledByDoctorId,
            dto.CancellationReason,
            dto.EnteredInErrorAtUtc,
            dto.EnteredInErrorByDoctorId,
            dto.EnteredInErrorReason,
            dto.DiagnosisSummary,
            dto.GeneralInstructions,
            dto.Version,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.Items.Select(MapItemToResponse).ToList());

    private static PrescriptionItemResponse MapItemToResponse(PrescriptionItemDto dto) =>
        new(
            dto.Id,
            dto.PrescriptionId,
            dto.MedicationCatalogItemId,
            dto.MedicationCode,
            dto.BrandName,
            dto.GenericName,
            dto.Form.ToString(),
            dto.Route.ToString(),
            dto.Dose,
            dto.DoseUnit,
            dto.Frequency,
            dto.DurationDays,
            dto.Quantity,
            dto.QuantityUnit,
            dto.DispensedQuantity,
            dto.IsFullyDispensed,
            dto.Instructions);

    private static PrescriptionSummaryResponse MapToSummaryResponse(PrescriptionSummaryDto dto) =>
        new(
            dto.Id,
            dto.PrescriptionNumber,
            dto.PatientId,
            dto.EncounterId,
            dto.PrescribingDoctorId,
            dto.DepartmentId,
            dto.Status.ToString(),
            dto.ValidUntilUtc,
            dto.SignedAtUtc,
            dto.ItemCount,
            dto.CreatedAtUtc);

    private static async Task<IResult> GetStockOverviewAsync(
        ClaimsPrincipal actor,
        [FromQuery] Guid? medicationCatalogItemId,
        [FromQuery] string? location,
        [FromQuery] bool? onlyLowStock,
        [FromServices] IMedicationStockService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetStockOverviewAsync(actor, medicationCatalogItemId, location, onlyLowStock, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToStockItemResponse).ToList()));
    }

    private static async Task<IResult> GetFefoCandidatesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid medicationCatalogItemId,
        [FromServices] IMedicationStockService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetFefoCandidatesAsync(actor, medicationCatalogItemId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToFefoResponse).ToList()));
    }

    private static async Task<IResult> AdjustStockAsync(
        ClaimsPrincipal actor,
        [FromBody] AdjustStockRequest request,
        [FromServices] IMedicationStockService service,
        CancellationToken cancellationToken)
    {
        var command = new AdjustStockCommand(
            request.StockItemId,
            request.ExpectedVersion,
            request.NewQuantity,
            request.Reason);
        var result = await service.AdjustStockAsync(actor, command, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(MapToStockItemResponse(r)));
    }

    private static async Task<IResult> GetStockTransactionsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid stockItemId,
        [FromServices] IMedicationStockService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetStockTransactionsAsync(actor, stockItemId, cancellationToken);
        return ToHttpResult(result, r => Results.Ok(r.Select(MapToStockTransactionResponse).ToList()));
    }

    private static MedicationStockItemResponse MapToStockItemResponse(MedicationStockItemDto dto) =>
        new(
            dto.Id,
            dto.DepartmentId,
            dto.Location,
            dto.MedicationCatalogItemId,
            dto.MedicationCode,
            dto.BrandName,
            dto.GenericName,
            dto.LotNumber,
            dto.ExpirationDateUtc,
            dto.QuantityOnHand,
            dto.QuantityReserved,
            dto.QuantityAvailable,
            dto.ReorderLevel,
            dto.IsExpired,
            dto.IsLowStock,
            dto.CreatedAtUtc,
            dto.Version);

    private static FefoCandidateStockResponse MapToFefoResponse(FefoCandidateDto dto) =>
        new(
            dto.StockItemId,
            dto.Location,
            dto.LotNumber,
            dto.ExpirationDateUtc,
            dto.QuantityAvailable,
            dto.DaysUntilExpiration,
            dto.Version);

    private static MedicationStockTransactionResponse MapToStockTransactionResponse(MedicationStockTransactionDto dto) =>
        new(
            dto.Id,
            dto.StockItemId,
            dto.TransactionType.ToString(),
            dto.Quantity,
            dto.PreviousQuantityOnHand,
            dto.NewQuantityOnHand,
            dto.ReferenceId,
            dto.Notes,
            dto.PerformedAtUtc);
}
