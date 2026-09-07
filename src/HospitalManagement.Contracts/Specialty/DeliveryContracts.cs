namespace HospitalManagement.Contracts.Specialty;

public sealed record CreateDeliveryRecordRequest
{
    public Guid? PregnancyEpisodeId
    {
        get; set;
    }
    public Guid MotherPatientId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public string DeliveryMode { get; set; } = "SpontaneousVaginal";
    public DateTime DeliveryTimeUtc
    {
        get; set;
    }
    public int GestationalAgeWeeks
    {
        get; set;
    }
    public int GestationalAgeDays
    {
        get; set;
    }
    public string PerinealTear { get; set; } = "None";
    public decimal EstimatedBloodLossMl
    {
        get; set;
    }
    public Guid AttendingDoctorId
    {
        get; set;
    }
    public Guid? AssistingMidwifeId
    {
        get; set;
    }
    public Guid? PediatricianDoctorId
    {
        get; set;
    }
    public string? MaternalComplicationsNotes
    {
        get; set;
    }
    public string? DeliverySummaryNotes
    {
        get; set;
    }
    public List<AddNewbornRequest> Newborns { get; set; } = [];
}

public sealed record AddNewbornRequest
{
    public Guid NewbornPatientId
    {
        get; set;
    }
    public int BirthOrder { get; set; } = 1;
    public DateTime BirthTimeUtc
    {
        get; set;
    }
    public string Gender { get; set; } = "Undetermined";
    public decimal BirthWeightGrams
    {
        get; set;
    }
    public decimal BirthLengthCm
    {
        get; set;
    }
    public decimal HeadCircumferenceCm
    {
        get; set;
    }
    public int ApgarScore1Min
    {
        get; set;
    }
    public int ApgarScore5Min
    {
        get; set;
    }
    public int? ApgarScore10Min
    {
        get; set;
    }
    public string ResuscitationGiven { get; set; } = "None";
    public string? CordBloodPh
    {
        get; set;
    }
    public string? ComplicationsNotes
    {
        get; set;
    }
}

public sealed record NewbornResponse(
    Guid Id,
    Guid DeliveryRecordId,
    Guid NewbornPatientId,
    int BirthOrder,
    DateTime BirthTimeUtc,
    string Gender,
    decimal BirthWeightGrams,
    decimal BirthLengthCm,
    decimal HeadCircumferenceCm,
    int ApgarScore1Min,
    int ApgarScore5Min,
    int? ApgarScore10Min,
    string ResuscitationGiven,
    string? CordBloodPh,
    string? ComplicationsNotes,
    DateTime CreatedAtUtc);

public sealed record DeliveryRecordResponse(
    Guid Id,
    Guid? PregnancyEpisodeId,
    Guid MotherPatientId,
    Guid? EncounterId,
    string DeliveryProtocolNumber,
    string DeliveryMode,
    DateTime DeliveryTimeUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    string PerinealTear,
    decimal EstimatedBloodLossMl,
    Guid AttendingDoctorId,
    Guid? AssistingMidwifeId,
    Guid? PediatricianDoctorId,
    string? MaternalComplicationsNotes,
    string? DeliverySummaryNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<NewbornResponse> Newborns);
