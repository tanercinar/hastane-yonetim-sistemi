using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Patients;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Domain;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Patients;

public static class PatientEndpointRouteBuilderExtensions
{
    public static WebApplication MapPatientEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/patients")
            .WithTags("Patients");

        group.MapPost("", RegisterPatientAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Patient.DemographicsCreate)
            .WithName("CreatePatientRecord")
            .Produces<PatientDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetPatientByIdAsync)
            .RequireAuthorization()
            .WithName("GetPatientRecordById")
            .Produces<PatientDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/by-person/{personId:guid}", GetPatientByPersonIdAsync)
            .RequireAuthorization()
            .WithName("GetPatientRecordByPersonId")
            .Produces<PatientDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", UpdatePatientAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Patient.DemographicsEdit)
            .WithName("UpdatePatientRecord")
            .Produces<PatientDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("", SearchPatientsAsync)
            .RequirePermission(HospitalPermissions.Patient.Search)
            .WithName("SearchPatientRecords")
            .Produces<PatientListResponse>();

        group.MapPost("/duplicate-check", CheckDuplicateAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Patient.DemographicsCreate)
            .WithName("CheckDuplicatePatientRecord")
            .Produces<DuplicateCheckResponse>();

        return app;
    }

    private static async Task<IResult> RegisterPatientAsync(
        [FromBody] PatientRegistrationRequest request,
        HttpContext context,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var gender = Enum.TryParse<Gender>(request.Gender, true, out var parsedGender) ? parsedGender : Gender.Unspecified;

        var command = new RegisterPatientCommand(
            request.PersonId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            gender,
            request.NationalIdSynthetic,
            request.PhoneNumber,
            request.Email,
            request.Address is null ? null : new AddressValue(
                request.Address.City,
                request.Address.District,
                request.Address.Line1,
                request.Address.PostalCode),
            request.EmergencyContact is null ? null : new EmergencyContactValue(
                request.EmergencyContact.FullName,
                request.EmergencyContact.Relationship,
                request.EmergencyContact.PhoneNumber),
            request.CommunicationPreferences is null ? null : new CommunicationPreferencesValue(
                request.CommunicationPreferences.AllowSms,
                request.CommunicationPreferences.AllowEmail,
                request.CommunicationPreferences.PreferredLanguage));

        var result = await service.RegisterPatientAsync(context.User, command, cancellationToken);
        return MapResult(result, dto => MapDetail(dto), isCreate: true);
    }

    private static async Task<IResult> UpdatePatientAsync(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatchHeader,
        [FromBody] PatientUpdateRequest request,
        HttpContext context,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var expectedVersion = 1L;
        if (!string.IsNullOrWhiteSpace(ifMatchHeader) && long.TryParse(ifMatchHeader.Trim('"'), out var parsedVersion))
        {
            expectedVersion = parsedVersion;
        }

        var gender = Enum.TryParse<Gender>(request.Gender, true, out var parsedGender) ? parsedGender : Gender.Unspecified;

        var command = new UpdatePatientCommand(
            id,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            gender,
            request.NationalIdSynthetic,
            request.PhoneNumber,
            request.Email,
            request.Address is null ? null : new AddressValue(
                request.Address.City,
                request.Address.District,
                request.Address.Line1,
                request.Address.PostalCode),
            request.EmergencyContact is null ? null : new EmergencyContactValue(
                request.EmergencyContact.FullName,
                request.EmergencyContact.Relationship,
                request.EmergencyContact.PhoneNumber),
            request.CommunicationPreferences is null ? null : new CommunicationPreferencesValue(
                request.CommunicationPreferences.AllowSms,
                request.CommunicationPreferences.AllowEmail,
                request.CommunicationPreferences.PreferredLanguage),
            expectedVersion);

        var result = await service.UpdatePatientAsync(context.User, command, cancellationToken);
        return MapResult(result, dto => MapDetail(dto));
    }

    private static async Task<IResult> GetPatientByIdAsync(
        Guid id,
        HttpContext context,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientByIdAsync(context.User, id, cancellationToken);
        return MapResult(result, dto => MapDetail(dto));
    }

    private static async Task<IResult> GetPatientByPersonIdAsync(
        Guid personId,
        HttpContext context,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientByPersonIdAsync(context.User, personId, cancellationToken);
        return MapResult(result, dto => MapDetail(dto));
    }

    private static async Task<IResult> SearchPatientsAsync(
        string? query,
        int? page,
        int? pageSize,
        HttpContext context,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var effectivePage = page is null or <= 0 ? 1 : page.Value;
        var effectivePageSize = pageSize is null or <= 0 ? 50 : pageSize.Value;

        var result = await service.SearchPatientsAsync(
            context.User,
            new PatientSearchQuery(query, effectivePage, effectivePageSize),
            cancellationToken);

        return MapResult(result, res => new PatientListResponse(
            res.Items.Select(i => new PatientSummaryResponse(
                i.Id,
                i.PersonId,
                i.MedicalRecordNumber,
                i.FirstName,
                i.LastName,
                i.DateOfBirth,
                i.Gender,
                i.MaskedNationalId,
                i.MaskedPhoneNumber,
                i.Email,
                i.IsActive,
                i.CreatedAtUtc)).ToList(),
            res.TotalCount,
            res.Page,
            res.PageSize));
    }

    private static async Task<IResult> CheckDuplicateAsync(
        [FromBody] DuplicatePatientCheckRequest request,
        IPatientService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CheckDuplicateAsync(
            new DuplicateCheckQuery(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.NationalIdSynthetic),
            cancellationToken);

        return MapResult(result, res => new DuplicateCheckResponse(
            res.HasPotentialDuplicate,
            res.Candidates.Select(c => new Contracts.Patients.DuplicateCandidateDto(
                c.PatientId,
                c.MedicalRecordNumber,
                c.FullName,
                c.DateOfBirth,
                c.MatchReason)).ToList()));
    }

    private static PatientDetailResponse MapDetail(PatientDetailDto dto) =>
        new(
            dto.Id,
            dto.PersonId,
            dto.MedicalRecordNumber,
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.NationalIdSynthetic,
            dto.PhoneNumber,
            dto.Email,
            dto.Address is null ? null : new Contracts.Patients.AddressDto(
                dto.Address.City,
                dto.Address.District,
                dto.Address.Line1,
                dto.Address.PostalCode),
            dto.EmergencyContact is null ? null : new Contracts.Patients.EmergencyContactDto(
                dto.EmergencyContact.FullName,
                dto.EmergencyContact.Relationship,
                dto.EmergencyContact.PhoneNumber),
            new Contracts.Patients.CommunicationPreferencesDto(
                dto.CommunicationPreferences.AllowSms,
                dto.CommunicationPreferences.AllowEmail,
                dto.CommunicationPreferences.PreferredLanguage),
            dto.IsActive,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.Version);

    private static IResult MapResult<TIn, TOut>(
        PatientOperationResult<TIn> result,
        Func<TIn, TOut> mapper,
        bool isCreate = false) =>
        result.Status switch
        {
            PatientOperationStatus.Succeeded when isCreate => Results.Created(string.Empty, mapper(result.Value!)),
            PatientOperationStatus.Succeeded => Results.Ok(mapper(result.Value!)),
            PatientOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt bulunamadı.",
                detail: result.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? "İstenen hasta kaydı bulunamadı."),
            PatientOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string[]>()),
            PatientOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma veya yinelenen kayıt.",
                detail: result.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? "Eşzamanlılık veya mükerrer kayıt çakışması."),
            PatientOperationStatus.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Erişim engellendi.",
                detail: "Bu hasta kaydına erişim yetkiniz bulunmamaktadır."),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
}
