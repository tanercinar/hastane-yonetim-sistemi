using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record CreatePregnancyEpisodeDto(
    Guid PatientId,
    Guid OpeningEncounterId,
    int Gravida,
    int Para,
    int Abortus,
    int LivingChildren,
    DateTime LastMenstrualPeriodUtc,
    DateTime? EstimatedDeliveryDateUtc,
    string? BloodGroupAndRh,
    PregnancyRiskCategory RiskCategory,
    string? RiskFactorsNotes,
    Guid? AssignedDoctorId,
    Guid? AssignedMidwifeId);

public sealed record RecordAntenatalVisitDto(
    Guid PregnancyEpisodeId,
    Guid EncounterId,
    DateTime VisitDateUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    decimal? MaternalWeightKg,
    int? SystolicBpMmHg,
    int? DiastolicBpMmHg,
    decimal? FundalHeightCm,
    int? FetalHeartRateBpm,
    FetalPresentation FetalPresentation,
    EdemaLevel EdemaLevel,
    bool UrineProteinPresent,
    bool UrineGlucosePresent,
    string? ClinicalNotes,
    DateTime? NextVisitRecommendedDateUtc);

public sealed record AntenatalVisitDto(
    Guid Id,
    Guid PregnancyEpisodeId,
    Guid EncounterId,
    DateTime VisitDateUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    decimal? MaternalWeightKg,
    int? SystolicBpMmHg,
    int? DiastolicBpMmHg,
    decimal? FundalHeightCm,
    int? FetalHeartRateBpm,
    FetalPresentation FetalPresentation,
    EdemaLevel EdemaLevel,
    bool UrineProteinPresent,
    bool UrineGlucosePresent,
    Guid StaffId,
    string? ClinicalNotes,
    DateTime? NextVisitRecommendedDateUtc,
    DateTime CreatedAtUtc);

public sealed record PregnancyEpisodeDto(
    Guid Id,
    Guid PatientId,
    Guid OpeningEncounterId,
    string EpisodeProtocolNumber,
    int Gravida,
    int Para,
    int Abortus,
    int LivingChildren,
    DateTime LastMenstrualPeriodUtc,
    DateTime EstimatedDeliveryDateUtc,
    string? BloodGroupAndRh,
    PregnancyRiskCategory RiskCategory,
    string? RiskFactorsNotes,
    PregnancyEpisodeStatus Status,
    Guid? AssignedDoctorId,
    Guid? AssignedMidwifeId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<AntenatalVisitDto> AntenatalVisits);

public enum SpecialtyOperationStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationFailed,
}

public sealed class SpecialtyOperationResult<T>
{
    public SpecialtyOperationStatus Status
    {
        get; init;
    }
    public T? Value
    {
        get; init;
    }
    public string? ErrorMessage
    {
        get; init;
    }
    public Dictionary<string, string[]>? ValidationErrors
    {
        get; init;
    }

    public bool IsSuccess => Status == SpecialtyOperationStatus.Success;
}

public static class SpecialtyOperationResult
{
    public static SpecialtyOperationResult<T> Success<T>(T value) =>
        new()
        {
            Status = SpecialtyOperationStatus.Success,
            Value = value
        };

    public static SpecialtyOperationResult<T> NotFound<T>(string message) =>
        new()
        {
            Status = SpecialtyOperationStatus.NotFound,
            ErrorMessage = message
        };

    public static SpecialtyOperationResult<T> Conflict<T>(string message) =>
        new()
        {
            Status = SpecialtyOperationStatus.Conflict,
            ErrorMessage = message
        };

    public static SpecialtyOperationResult<T> Validation<T>(string propertyName, string error) =>
        new()
        {
            Status = SpecialtyOperationStatus.ValidationFailed,
            ErrorMessage = error,
            ValidationErrors = new Dictionary<string, string[]> { [propertyName] = [error] },
        };
}
