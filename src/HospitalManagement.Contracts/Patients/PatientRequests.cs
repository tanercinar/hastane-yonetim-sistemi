namespace HospitalManagement.Contracts.Patients;

public sealed class PatientRegistrationRequest
{
    public Guid? PersonId
    {
        get; set;
    }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth
    {
        get; set;
    }

    public string Gender { get; set; } = "Unspecified";

    public string? NationalIdSynthetic
    {
        get; set;
    }

    public string? PhoneNumber
    {
        get; set;
    }

    public string? Email
    {
        get; set;
    }

    public AddressDto? Address
    {
        get; set;
    }

    public EmergencyContactDto? EmergencyContact
    {
        get; set;
    }

    public CommunicationPreferencesDto? CommunicationPreferences
    {
        get; set;
    }
}

public sealed class PatientUpdateRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth
    {
        get; set;
    }

    public string Gender { get; set; } = "Unspecified";

    public string? NationalIdSynthetic
    {
        get; set;
    }

    public string? PhoneNumber
    {
        get; set;
    }

    public string? Email
    {
        get; set;
    }

    public AddressDto? Address
    {
        get; set;
    }

    public EmergencyContactDto? EmergencyContact
    {
        get; set;
    }

    public CommunicationPreferencesDto? CommunicationPreferences
    {
        get; set;
    }
}

public sealed class DuplicatePatientCheckRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth
    {
        get; set;
    }

    public string? NationalIdSynthetic
    {
        get; set;
    }
}
