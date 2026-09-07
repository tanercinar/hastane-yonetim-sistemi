namespace HospitalManagement.UI.Components.PatientMobile;

public record PatientMobileAppointmentDto
{
    public Guid Id
    {
        get;
        init;
    }

    public string AppointmentNumber
    {
        get;
        init;
    } = string.Empty;

    public string DepartmentName
    {
        get;
        init;
    } = string.Empty;

    public string DoctorName
    {
        get;
        init;
    } = string.Empty;

    public DateTime ScheduledDate
    {
        get;
        init;
    }

    public string TimeSlot
    {
        get;
        init;
    } = string.Empty;

    public string Reason
    {
        get;
        init;
    } = string.Empty;

    public string Status
    {
        get;
        init;
    } = "Scheduled";

    public bool CanCancel => Status == "Scheduled" && ScheduledDate > DateTime.UtcNow;
}

public record PatientMobilePrescriptionDto
{
    public Guid Id
    {
        get;
        init;
    }

    public string PrescriptionNumber
    {
        get;
        init;
    } = string.Empty;

    public DateTime IssuedAt
    {
        get;
        init;
    }

    public string DoctorName
    {
        get;
        init;
    } = string.Empty;

    public string Diagnosis
    {
        get;
        init;
    } = string.Empty;

    public string DispenseStatus
    {
        get;
        init;
    } = "Pending";

    public List<PatientMobileMedicationDto> Items
    {
        get;
        init;
    } = [];
}

public record PatientMobileMedicationDto
{
    public string MedicationName
    {
        get;
        init;
    } = string.Empty;

    public string Dosage
    {
        get;
        init;
    } = string.Empty;

    public string Instructions
    {
        get;
        init;
    } = string.Empty;

    public int Quantity
    {
        get;
        init;
    }
}

public record PatientMobileDiagnosticResultDto
{
    public Guid Id
    {
        get;
        init;
    }

    public string RequestNumber
    {
        get;
        init;
    } = string.Empty;

    public string Category
    {
        get;
        init;
    } = "Laboratory";

    public string TestName
    {
        get;
        init;
    } = string.Empty;

    public DateTime ApprovedAt
    {
        get;
        init;
    }

    public string Status
    {
        get;
        init;
    } = "Final";

    public string ResultSummary
    {
        get;
        init;
    } = string.Empty;

    public string? ReferenceRange
    {
        get;
        init;
    }

    public bool IsCritical
    {
        get;
        init;
    }
}

public record PatientMobileSlotDto
{
    public Guid SlotId
    {
        get;
        init;
    }

    public string Time
    {
        get;
        init;
    } = string.Empty;

    public string DoctorName
    {
        get;
        init;
    } = string.Empty;

    public string DepartmentName
    {
        get;
        init;
    } = string.Empty;

    public bool IsAvailable
    {
        get;
        init;
    }
}
