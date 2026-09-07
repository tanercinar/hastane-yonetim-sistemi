namespace HospitalManagement.Contracts.Surgery;

public sealed class OperatingRoomResponse
{
    public Guid Id
    {
        get; set;
    }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
    public int Capacity
    {
        get; set;
    }
    public Guid? SpecialtyDepartmentId
    {
        get; set;
    }

    public OperatingRoomResponse()
    {
    }

    public OperatingRoomResponse(
        Guid id,
        string roomCode,
        string roomName,
        bool isActive,
        int capacity,
        Guid? specialtyDepartmentId)
    {
        Id = id;
        RoomCode = roomCode;
        RoomName = roomName;
        IsActive = isActive;
        Capacity = capacity;
        SpecialtyDepartmentId = specialtyDepartmentId;
    }
}

public sealed class CreateSurgeryBookingRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public Guid DepartmentId
    {
        get; set;
    }
    public string DepartmentName { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public string Urgency { get; set; } = "Elective";
    public Guid OperatingRoomId
    {
        get; set;
    }
    public Guid LeadSurgeonDoctorId
    {
        get; set;
    }
    public Guid AnesthesiologistDoctorId
    {
        get; set;
    }
    public Guid? OperatingNurseStaffId
    {
        get; set;
    }
    public DateTime ScheduledStartTimeUtc
    {
        get; set;
    }
    public DateTime ScheduledEndTimeUtc
    {
        get; set;
    }
    public string? ClinicalNotes
    {
        get; set;
    }

    public CreateSurgeryBookingRequest()
    {
    }

    public CreateSurgeryBookingRequest(
        Guid patientId,
        Guid? encounterId,
        Guid departmentId,
        string departmentName,
        string procedureName,
        string procedureCode,
        string urgency,
        Guid operatingRoomId,
        Guid leadSurgeonDoctorId,
        Guid anesthesiologistDoctorId,
        Guid? operatingNurseStaffId,
        DateTime scheduledStartTimeUtc,
        DateTime scheduledEndTimeUtc,
        string? clinicalNotes)
    {
        PatientId = patientId;
        EncounterId = encounterId;
        DepartmentId = departmentId;
        DepartmentName = departmentName;
        ProcedureName = procedureName;
        ProcedureCode = procedureCode;
        Urgency = urgency;
        OperatingRoomId = operatingRoomId;
        LeadSurgeonDoctorId = leadSurgeonDoctorId;
        AnesthesiologistDoctorId = anesthesiologistDoctorId;
        OperatingNurseStaffId = operatingNurseStaffId;
        ScheduledStartTimeUtc = scheduledStartTimeUtc;
        ScheduledEndTimeUtc = scheduledEndTimeUtc;
        ClinicalNotes = clinicalNotes;
    }
}

public sealed class RescheduleSurgeryBookingRequest
{
    public Guid OperatingRoomId
    {
        get; set;
    }
    public DateTime ScheduledStartTimeUtc
    {
        get; set;
    }
    public DateTime ScheduledEndTimeUtc
    {
        get; set;
    }

    public RescheduleSurgeryBookingRequest()
    {
    }

    public RescheduleSurgeryBookingRequest(
        Guid operatingRoomId,
        DateTime scheduledStartTimeUtc,
        DateTime scheduledEndTimeUtc)
    {
        OperatingRoomId = operatingRoomId;
        ScheduledStartTimeUtc = scheduledStartTimeUtc;
        ScheduledEndTimeUtc = scheduledEndTimeUtc;
    }
}

public sealed class RecordPreOpChecklistRequest
{
    public bool ConsentSigned
    {
        get; set;
    }
    public bool AnesthesiaClearance
    {
        get; set;
    }
    public bool NpoConfirmed
    {
        get; set;
    }
    public bool BloodProductsReserved
    {
        get; set;
    }
    public bool SiteMarked
    {
        get; set;
    }
    public bool AllergyChecked
    {
        get; set;
    }
    public string? Notes
    {
        get; set;
    }

    public RecordPreOpChecklistRequest()
    {
    }

    public RecordPreOpChecklistRequest(
        bool consentSigned,
        bool anesthesiaClearance,
        bool npoConfirmed,
        bool bloodProductsReserved,
        bool siteMarked,
        bool allergyChecked,
        string? notes)
    {
        ConsentSigned = consentSigned;
        AnesthesiaClearance = anesthesiaClearance;
        NpoConfirmed = npoConfirmed;
        BloodProductsReserved = bloodProductsReserved;
        SiteMarked = siteMarked;
        AllergyChecked = allergyChecked;
        Notes = notes;
    }
}

public sealed class CancelSurgeryBookingRequest
{
    public string Reason { get; set; } = string.Empty;

    public CancelSurgeryBookingRequest()
    {
    }

    public CancelSurgeryBookingRequest(string reason)
    {
        Reason = reason;
    }
}

public sealed record PreOpChecklistResponse(
    bool ConsentSigned,
    bool AnesthesiaClearance,
    bool NpoConfirmed,
    bool BloodProductsReserved,
    bool SiteMarked,
    bool AllergyChecked,
    bool IsFullyCleared,
    Guid CompletedByStaffId,
    DateTime CompletedAtUtc,
    string? Notes);

public sealed record SurgeryBookingResponse(
    Guid Id,
    string BookingProtocolNumber,
    Guid PatientId,
    Guid? EncounterId,
    Guid DepartmentId,
    string DepartmentName,
    string ProcedureName,
    string ProcedureCode,
    string Urgency,
    Guid OperatingRoomId,
    Guid LeadSurgeonDoctorId,
    Guid AnesthesiologistDoctorId,
    Guid? OperatingNurseStaffId,
    DateTime ScheduledStartTimeUtc,
    DateTime ScheduledEndTimeUtc,
    string Status,
    PreOpChecklistResponse? PreOpChecklist,
    string? ClinicalNotes,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
