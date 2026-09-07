using HospitalManagement.Modules.Patients.Domain;

namespace HospitalManagement.Modules.Patients.Application;

public sealed record RegisterPatientCommand(
    Guid? PersonId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalIdSynthetic,
    string? PhoneNumber,
    string? Email,
    AddressValue? Address,
    EmergencyContactValue? EmergencyContact,
    CommunicationPreferencesValue? CommunicationPreferences);

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalIdSynthetic,
    string? PhoneNumber,
    string? Email,
    AddressValue? Address,
    EmergencyContactValue? EmergencyContact,
    CommunicationPreferencesValue? CommunicationPreferences,
    long ExpectedVersion);

public sealed record PatientSearchQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50);

public sealed record DuplicateCheckQuery(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? NationalIdSynthetic);

public enum PatientOperationStatus
{
    Succeeded,
    NotFound,
    ValidationFailed,
    Conflict,
    Unauthorized,
    Forbidden,
}

public sealed class PatientOperationResult<T>
{
    public PatientOperationStatus Status
    {
        get; init;
    }

    public T? Value
    {
        get; init;
    }

    public IReadOnlyDictionary<string, string[]>? Errors
    {
        get; init;
    }
}

public static class PatientOperationResult
{
    public static PatientOperationResult<T> Success<T>(T value) =>
        new()
        {
            Status = PatientOperationStatus.Succeeded,
            Value = value
        };

    public static PatientOperationResult<T> NotFound<T>(string message = "Hasta kaydı bulunamadı.") =>
        new()
        {
            Status = PatientOperationStatus.NotFound,
            Errors = new Dictionary<string, string[]> { ["patient"] = [message] },
        };

    public static PatientOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new()
        {
            Status = PatientOperationStatus.ValidationFailed,
            Errors = errors
        };

    public static PatientOperationResult<T> Validation<T>(string key, string message) =>
        new()
        {
            Status = PatientOperationStatus.ValidationFailed,
            Errors = new Dictionary<string, string[]> { [key] = [message] },
        };

    public static PatientOperationResult<T> Conflict<T>(string message) =>
        new()
        {
            Status = PatientOperationStatus.Conflict,
            Errors = new Dictionary<string, string[]> { ["conflict"] = [message] },
        };

    public static PatientOperationResult<T> Forbidden<T>(string message = "Bu hasta kaydına erişim yetkiniz bulunmamaktadır.") =>
        new()
        {
            Status = PatientOperationStatus.Forbidden,
            Errors = new Dictionary<string, string[]> { ["authorization"] = [message] },
        };
}
