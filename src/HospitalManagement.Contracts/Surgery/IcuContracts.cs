namespace HospitalManagement.Contracts.Surgery;

public sealed record IcuBedResponse(
    Guid Id,
    string BedCode,
    string BedName,
    string UnitName,
    bool IsActive,
    bool IsOccupied,
    Guid? CurrentAdmissionId,
    string? CurrentPatientProtocolNumber);

public sealed class CreateIcuAdmissionRequest
{
    public Guid InpatientStayId
    {
        get; set;
    }
    public Guid PatientId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public Guid IcuBedId
    {
        get; set;
    }
    public Guid AttendingDoctorId
    {
        get; set;
    }
    public Guid? PrimaryNurseId
    {
        get; set;
    }
    public string AdmissionReason { get; set; } = string.Empty;
    public string AcuityLevel { get; set; } = "Level2IntensiveMonitoring";
    public int MonitoringFrequencyMinutes { get; set; } = 60;
    public string VentilationMode { get; set; } = "NoneSpontaneous";
    public string? CarePlanNotes
    {
        get; set;
    }

    public CreateIcuAdmissionRequest()
    {
    }

    public CreateIcuAdmissionRequest(
        Guid inpatientStayId,
        Guid patientId,
        Guid? encounterId,
        Guid icuBedId,
        Guid attendingDoctorId,
        Guid? primaryNurseId,
        string admissionReason,
        string acuityLevel,
        int monitoringFrequencyMinutes,
        string ventilationMode,
        string? carePlanNotes)
    {
        InpatientStayId = inpatientStayId;
        PatientId = patientId;
        EncounterId = encounterId;
        IcuBedId = icuBedId;
        AttendingDoctorId = attendingDoctorId;
        PrimaryNurseId = primaryNurseId;
        AdmissionReason = admissionReason;
        AcuityLevel = acuityLevel;
        MonitoringFrequencyMinutes = monitoringFrequencyMinutes;
        VentilationMode = ventilationMode;
        CarePlanNotes = carePlanNotes;
    }
}

public sealed class UpdateIcuCarePlanRequest
{
    public string AcuityLevel { get; set; } = "Level2IntensiveMonitoring";
    public int MonitoringFrequencyMinutes { get; set; } = 60;
    public string VentilationMode { get; set; } = "NoneSpontaneous";
    public Guid? PrimaryNurseId
    {
        get; set;
    }
    public string? CarePlanNotes
    {
        get; set;
    }

    public UpdateIcuCarePlanRequest()
    {
    }

    public UpdateIcuCarePlanRequest(
        string acuityLevel,
        int monitoringFrequencyMinutes,
        string ventilationMode,
        Guid? primaryNurseId,
        string? carePlanNotes)
    {
        AcuityLevel = acuityLevel;
        MonitoringFrequencyMinutes = monitoringFrequencyMinutes;
        VentilationMode = ventilationMode;
        PrimaryNurseId = primaryNurseId;
        CarePlanNotes = carePlanNotes;
    }
}

public sealed class IcuDischargeOrTransferRequest
{
    public string DestinationStatus { get; set; } = "TransferredToWard";
    public string DischargeNotes { get; set; } = string.Empty;

    public IcuDischargeOrTransferRequest()
    {
    }

    public IcuDischargeOrTransferRequest(string destinationStatus, string dischargeNotes)
    {
        DestinationStatus = destinationStatus;
        DischargeNotes = dischargeNotes;
    }
}

public sealed record IcuAdmissionResponse(
    Guid Id,
    string AdmissionProtocolNumber,
    Guid InpatientStayId,
    Guid PatientId,
    Guid? EncounterId,
    Guid IcuBedId,
    string IcuBedCode,
    Guid AttendingDoctorId,
    Guid? PrimaryNurseId,
    string AdmissionReason,
    string AcuityLevel,
    int MonitoringFrequencyMinutes,
    string VentilationMode,
    string Status,
    string? CarePlanNotes,
    DateTime AdmittedAtUtc,
    DateTime? DischargedAtUtc,
    string? DischargeNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
