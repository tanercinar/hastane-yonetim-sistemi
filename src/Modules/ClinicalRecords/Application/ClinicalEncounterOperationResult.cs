namespace HospitalManagement.Modules.ClinicalRecords.Application;

public enum ClinicalEncounterOperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict,
    Forbidden,
}

public sealed class ClinicalEncounterOperationResult<T>
{
    internal ClinicalEncounterOperationResult(
        ClinicalEncounterOperationStatus status,
        T? value,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
    }

    public ClinicalEncounterOperationStatus Status
    {
        get;
    }

    public T? Value
    {
        get;
    }

    public string? ErrorMessage
    {
        get;
    }

    public IReadOnlyDictionary<string, string[]> ValidationErrors
    {
        get;
    }

    public bool Succeeded => Status == ClinicalEncounterOperationStatus.Success;
}

public static class ClinicalEncounterOperationResult
{
    public static ClinicalEncounterOperationResult<T> Success<T>(T value) =>
        new(ClinicalEncounterOperationStatus.Success, value, null, null);

    public static ClinicalEncounterOperationResult<T> NotFound<T>(string message = "Karşılaşma kaydı bulunamadı.") =>
        new(ClinicalEncounterOperationStatus.NotFound, default, message, null);

    public static ClinicalEncounterOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new(ClinicalEncounterOperationStatus.ValidationFailed, default, "Doğrulama hatası oluştu.", errors);

    public static ClinicalEncounterOperationResult<T> Validation<T>(string key, string message) =>
        new(
            ClinicalEncounterOperationStatus.ValidationFailed,
            default,
            message,
            new Dictionary<string, string[]>(StringComparer.Ordinal) { [key] = [message] });

    public static ClinicalEncounterOperationResult<T> Conflict<T>(string message) =>
        new(ClinicalEncounterOperationStatus.Conflict, default, message, null);

    public static ClinicalEncounterOperationResult<T> Forbidden<T>(string message = "Bu klinik karşılaşmaya erişim veya işlem yetkiniz bulunmamaktadır.") =>
        new(ClinicalEncounterOperationStatus.Forbidden, default, message, null);
}
