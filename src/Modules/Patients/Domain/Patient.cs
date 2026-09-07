using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Patients.Domain;

public sealed class Patient : IHasConcurrencyVersion
{
    private Patient()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid PersonId
    {
        get; private set;
    }

    public string MedicalRecordNumber { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateOnly DateOfBirth
    {
        get; private set;
    }

    public Gender Gender
    {
        get; private set;
    }

    public string? NationalIdSynthetic
    {
        get; private set;
    }

    public string? PhoneNumber
    {
        get; private set;
    }

    public string? Email
    {
        get; private set;
    }

    public AddressValue? Address
    {
        get; private set;
    }

    public EmergencyContactValue? EmergencyContact
    {
        get; private set;
    }

    public CommunicationPreferencesValue CommunicationPreferences { get; private set; } = new();

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public static Patient Create(
        Guid id,
        Guid personId,
        string medicalRecordNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nationalIdSynthetic,
        string? phoneNumber,
        string? email,
        AddressValue? address,
        EmergencyContactValue? emergencyContact,
        CommunicationPreferencesValue? communicationPreferences,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicalRecordNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(id));
        }

        if (personId == Guid.Empty)
        {
            throw new ArgumentException("Kişi ID (PersonId) boş olamaz.", nameof(personId));
        }

        return new Patient
        {
            Id = id,
            PersonId = personId,
            MedicalRecordNumber = medicalRecordNumber.Trim().ToUpperInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            Gender = gender,
            NationalIdSynthetic = string.IsNullOrWhiteSpace(nationalIdSynthetic) ? null : nationalIdSynthetic.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Address = address,
            EmergencyContact = emergencyContact,
            CommunicationPreferences = communicationPreferences ?? new CommunicationPreferencesValue(),
            IsActive = true,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void UpdateDemographics(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nationalIdSynthetic,
        string? phoneNumber,
        string? email,
        AddressValue? address,
        EmergencyContactValue? emergencyContact,
        CommunicationPreferencesValue? communicationPreferences,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        NationalIdSynthetic = string.IsNullOrWhiteSpace(nationalIdSynthetic) ? null : nationalIdSynthetic.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Address = address;
        EmergencyContact = emergencyContact;
        if (communicationPreferences is not null)
        {
            CommunicationPreferences = communicationPreferences;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void SetActive(bool isActive, DateTime nowUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = nowUtc;
    }

    public void Anonymize(DateTime nowUtc)
    {
        FirstName = "ANONİM";
        LastName = "HASTA";
        NationalIdSynthetic = null;
        PhoneNumber = null;
        Email = null;
        Address = null;
        EmergencyContact = null;
        CommunicationPreferences = new CommunicationPreferencesValue
        {
            AllowEmail = false,
            AllowSms = false,
        };
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}
