namespace HospitalManagement.UI.Components.StaffWorkspace;

public sealed class StaffAppointmentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId
    {
        get; set;
    }
    public string PatientNameMasked { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Planlandı";
    public bool IsCheckedIn
    {
        get; set;
    }
}

public sealed class PatientSearchItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NationalIdMasked { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public Guid? ActiveEncounterId
    {
        get; set;
    }
}

public sealed class EncounterSummaryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime EncounterDate { get; set; } = DateTime.UtcNow;
    public string PatientNameMasked { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    public string VitalSignsSummary { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string PrescriptionSummary { get; set; } = string.Empty;
    public string Status { get; set; } = "Tamamlandı";
}

public sealed class StaffAlertItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Critical, Warning, Info
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead
    {
        get; set;
    }
}
