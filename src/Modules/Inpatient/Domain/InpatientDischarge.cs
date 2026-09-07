namespace HospitalManagement.Modules.Inpatient.Domain;

public enum DischargeType
{
    Home,
    TransferToOtherFacility,
    AgainstMedicalAdvice,
    Deceased,
}

public sealed class InpatientDischarge
{
    public Guid Id
    {
        get; private set;
    }
    public Guid AdmissionId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public Guid DischargingDoctorId
    {
        get; private set;
    }
    public DischargeType DischargeType
    {
        get; private set;
    }
    public string DischargeSummary { get; private set; } = string.Empty;
    public string FinalDiagnosisCode { get; private set; } = string.Empty;
    public string FinalDiagnosisDescription { get; private set; } = string.Empty;
    public string DischargeRecommendations { get; private set; } = string.Empty;
    public string? DischargePrescriptionSummary
    {
        get; private set;
    }
    public DateTime? FollowUpAppointmentDateUtc
    {
        get; private set;
    }
    public Guid? FollowUpDepartmentId
    {
        get; private set;
    }
    public string? TransferFacilityName
    {
        get; private set;
    }
    public string? TransferReason
    {
        get; private set;
    }
    public DateTime DischargedAtUtc
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    private InpatientDischarge()
    {
    }

    public static InpatientDischarge Create(
        Guid id,
        Guid admissionId,
        Guid patientId,
        Guid dischargingDoctorId,
        DischargeType dischargeType,
        string dischargeSummary,
        string finalDiagnosisCode,
        string finalDiagnosisDescription,
        string dischargeRecommendations,
        string? dischargePrescriptionSummary,
        DateTime? followUpAppointmentDateUtc,
        Guid? followUpDepartmentId,
        string? transferFacilityName,
        string? transferReason,
        DateTime dischargedAtUtc,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Taburculuk ID boş olamaz.", nameof(id));
        if (admissionId == Guid.Empty)
            throw new ArgumentException("Yatış ID boş olamaz.", nameof(admissionId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        if (dischargingDoctorId == Guid.Empty)
            throw new ArgumentException("Taburcu eden doktor ID boş olamaz.", nameof(dischargingDoctorId));

        if (string.IsNullOrWhiteSpace(dischargeSummary) || dischargeSummary.Trim().Length < 20)
        {
            throw new ArgumentException("Taburculuk özeti en az 20 karakter uzunluğunda olmalıdır.", nameof(dischargeSummary));
        }

        if (string.IsNullOrWhiteSpace(finalDiagnosisDescription))
        {
            throw new ArgumentException("Kesin tanı açıklaması boş olamaz.", nameof(finalDiagnosisDescription));
        }

        if (dischargeType == DischargeType.TransferToOtherFacility)
        {
            if (string.IsNullOrWhiteSpace(transferFacilityName))
            {
                throw new ArgumentException("Dış kurum sevki için hedef sağlık kuruluşu adı belirtilmelidir.", nameof(transferFacilityName));
            }

            if (string.IsNullOrWhiteSpace(transferReason))
            {
                throw new ArgumentException("Dış kurum sevki için sevk gerekçesi belirtilmelidir.", nameof(transferReason));
            }
        }

        return new InpatientDischarge
        {
            Id = id,
            AdmissionId = admissionId,
            PatientId = patientId,
            DischargingDoctorId = dischargingDoctorId,
            DischargeType = dischargeType,
            DischargeSummary = dischargeSummary.Trim(),
            FinalDiagnosisCode = finalDiagnosisCode?.Trim() ?? string.Empty,
            FinalDiagnosisDescription = finalDiagnosisDescription.Trim(),
            DischargeRecommendations = dischargeRecommendations?.Trim() ?? string.Empty,
            DischargePrescriptionSummary = string.IsNullOrWhiteSpace(dischargePrescriptionSummary) ? null : dischargePrescriptionSummary.Trim(),
            FollowUpAppointmentDateUtc = followUpAppointmentDateUtc,
            FollowUpDepartmentId = followUpDepartmentId,
            TransferFacilityName = string.IsNullOrWhiteSpace(transferFacilityName) ? null : transferFacilityName.Trim(),
            TransferReason = string.IsNullOrWhiteSpace(transferReason) ? null : transferReason.Trim(),
            DischargedAtUtc = dischargedAtUtc,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }
}
