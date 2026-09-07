namespace HospitalManagement.Contracts.Patients;

public sealed record AddressDto(
    string City,
    string District,
    string Line1,
    string? PostalCode = null);

public sealed record EmergencyContactDto(
    string FullName,
    string Relationship,
    string PhoneNumber);

public sealed record CommunicationPreferencesDto(
    bool AllowSms = true,
    bool AllowEmail = true,
    string PreferredLanguage = "tr");

public sealed record PatientSummaryResponse(
    Guid Id,
    Guid PersonId,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? MaskedNationalId,
    string? MaskedPhoneNumber,
    string? Email,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PatientDetailResponse(
    Guid Id,
    Guid PersonId,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? NationalIdSynthetic,
    string? PhoneNumber,
    string? Email,
    AddressDto? Address,
    EmergencyContactDto? EmergencyContact,
    CommunicationPreferencesDto CommunicationPreferences,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    long Version);

public sealed record DuplicateCandidateDto(
    Guid PatientId,
    string MedicalRecordNumber,
    string FullName,
    DateOnly DateOfBirth,
    string MatchReason);

public sealed record DuplicateCheckResponse(
    bool HasPotentialDuplicate,
    IReadOnlyList<DuplicateCandidateDto> Candidates);

public sealed record PatientListResponse(
    IReadOnlyList<PatientSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
