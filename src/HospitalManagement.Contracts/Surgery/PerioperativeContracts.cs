namespace HospitalManagement.Contracts.Surgery;

public sealed class SavePerioperativeRecordRequest
{
    public Guid SurgeryBookingId
    {
        get; set;
    }
    public DateTime? RoomEntryTimeUtc
    {
        get; set;
    }
    public DateTime? AnesthesiaStartTimeUtc
    {
        get; set;
    }
    public DateTime? IncisionTimeUtc
    {
        get; set;
    }
    public DateTime? ClosureTimeUtc
    {
        get; set;
    }
    public DateTime? AnesthesiaEndTimeUtc
    {
        get; set;
    }
    public DateTime? RoomExitTimeUtc
    {
        get; set;
    }
    public string AnesthesiaType { get; set; } = "General";
    public string? AnesthesiaNotes
    {
        get; set;
    }
    public string? IntraoperativeFindings
    {
        get; set;
    }
    public string? IntraoperativeComplications
    {
        get; set;
    }
    public int? EstimatedBloodLossMl
    {
        get; set;
    }
    public string? SpecimensCollected
    {
        get; set;
    }
    public bool CountsConfirmed
    {
        get; set;
    }
    public string PostOpDisposition { get; set; } = "PACU";
    public string? PostOpInstructions
    {
        get; set;
    }

    public SavePerioperativeRecordRequest()
    {
    }

    public SavePerioperativeRecordRequest(
        Guid surgeryBookingId,
        DateTime? roomEntryTimeUtc,
        DateTime? anesthesiaStartTimeUtc,
        DateTime? incisionTimeUtc,
        DateTime? closureTimeUtc,
        DateTime? anesthesiaEndTimeUtc,
        DateTime? roomExitTimeUtc,
        string anesthesiaType,
        string? anesthesiaNotes,
        string? intraoperativeFindings,
        string? intraoperativeComplications,
        int? estimatedBloodLossMl,
        string? specimensCollected,
        bool countsConfirmed,
        string postOpDisposition,
        string? postOpInstructions)
    {
        SurgeryBookingId = surgeryBookingId;
        RoomEntryTimeUtc = roomEntryTimeUtc;
        AnesthesiaStartTimeUtc = anesthesiaStartTimeUtc;
        IncisionTimeUtc = incisionTimeUtc;
        ClosureTimeUtc = closureTimeUtc;
        AnesthesiaEndTimeUtc = anesthesiaEndTimeUtc;
        RoomExitTimeUtc = roomExitTimeUtc;
        AnesthesiaType = anesthesiaType;
        AnesthesiaNotes = anesthesiaNotes;
        IntraoperativeFindings = intraoperativeFindings;
        IntraoperativeComplications = intraoperativeComplications;
        EstimatedBloodLossMl = estimatedBloodLossMl;
        SpecimensCollected = specimensCollected;
        CountsConfirmed = countsConfirmed;
        PostOpDisposition = postOpDisposition;
        PostOpInstructions = postOpInstructions;
    }
}

public sealed class SignPerioperativeRecordRequest
{
    public SignPerioperativeRecordRequest()
    {
    }
}

public sealed class AddPerioperativeCorrectionRequest
{
    public string ReasonForCorrection { get; set; } = string.Empty;
    public string CorrectionNote { get; set; } = string.Empty;

    public AddPerioperativeCorrectionRequest()
    {
    }

    public AddPerioperativeCorrectionRequest(string reasonForCorrection, string correctionNote)
    {
        ReasonForCorrection = reasonForCorrection;
        CorrectionNote = correctionNote;
    }
}

public sealed record PerioperativeCorrectionResponse(
    Guid Id,
    Guid PerioperativeRecordId,
    Guid CorrectedByDoctorId,
    DateTime CorrectedAtUtc,
    string ReasonForCorrection,
    string CorrectionNote);

public sealed record PerioperativeRecordResponse(
    Guid Id,
    Guid SurgeryBookingId,
    Guid PatientId,
    Guid OperatingRoomId,
    DateTime? RoomEntryTimeUtc,
    DateTime? AnesthesiaStartTimeUtc,
    DateTime? IncisionTimeUtc,
    DateTime? ClosureTimeUtc,
    DateTime? AnesthesiaEndTimeUtc,
    DateTime? RoomExitTimeUtc,
    string AnesthesiaType,
    string? AnesthesiaNotes,
    string? IntraoperativeFindings,
    string? IntraoperativeComplications,
    int? EstimatedBloodLossMl,
    string? SpecimensCollected,
    bool CountsConfirmed,
    string PostOpDisposition,
    string? PostOpInstructions,
    bool IsSigned,
    Guid? SignedByDoctorId,
    DateTime? SignedAtUtc,
    List<PerioperativeCorrectionResponse> Corrections,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
