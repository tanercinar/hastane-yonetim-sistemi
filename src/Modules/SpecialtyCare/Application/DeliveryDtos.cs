using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record CreateDeliveryRecordDto(
    Guid? PregnancyEpisodeId,
    Guid MotherPatientId,
    Guid? EncounterId,
    DeliveryMode DeliveryMode,
    DateTime DeliveryTimeUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    PerinealTearDegree PerinealTear,
    decimal EstimatedBloodLossMl,
    Guid AttendingDoctorId,
    Guid? AssistingMidwifeId,
    Guid? PediatricianDoctorId,
    string? MaternalComplicationsNotes,
    string? DeliverySummaryNotes,
    List<AddNewbornDto> Newborns);

public sealed record AddNewbornDto(
    Guid NewbornPatientId,
    int BirthOrder,
    DateTime BirthTimeUtc,
    NewbornGender Gender,
    decimal BirthWeightGrams,
    decimal BirthLengthCm,
    decimal HeadCircumferenceCm,
    int ApgarScore1Min,
    int ApgarScore5Min,
    int? ApgarScore10Min,
    ResuscitationIntervention ResuscitationGiven,
    string? CordBloodPh,
    string? ComplicationsNotes);

public sealed record NewbornDto(
    Guid Id,
    Guid DeliveryRecordId,
    Guid NewbornPatientId,
    int BirthOrder,
    DateTime BirthTimeUtc,
    NewbornGender Gender,
    decimal BirthWeightGrams,
    decimal BirthLengthCm,
    decimal HeadCircumferenceCm,
    int ApgarScore1Min,
    int ApgarScore5Min,
    int? ApgarScore10Min,
    ResuscitationIntervention ResuscitationGiven,
    string? CordBloodPh,
    string? ComplicationsNotes,
    DateTime CreatedAtUtc);

public sealed record DeliveryRecordDto(
    Guid Id,
    Guid? PregnancyEpisodeId,
    Guid MotherPatientId,
    Guid? EncounterId,
    string DeliveryProtocolNumber,
    DeliveryMode DeliveryMode,
    DateTime DeliveryTimeUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    PerinealTearDegree PerinealTear,
    decimal EstimatedBloodLossMl,
    Guid AttendingDoctorId,
    Guid? AssistingMidwifeId,
    Guid? PediatricianDoctorId,
    string? MaternalComplicationsNotes,
    string? DeliverySummaryNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<NewbornDto> Newborns);
