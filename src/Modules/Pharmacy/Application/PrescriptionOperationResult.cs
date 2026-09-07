namespace HospitalManagement.Modules.Pharmacy.Application;

public enum PrescriptionOperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict,
    Forbidden,
    SafetyWarningOverrideRequired,
}

public sealed class PrescriptionOperationResult<T>
{
    internal PrescriptionOperationResult(
        PrescriptionOperationStatus status,
        T? value,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
    }

    public PrescriptionOperationStatus Status
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
    public bool Succeeded => Status == PrescriptionOperationStatus.Success;
}

public static class PrescriptionOperationResult
{
    public static PrescriptionOperationResult<T> Success<T>(T value) =>
        new(PrescriptionOperationStatus.Success, value, null, null);

    public static PrescriptionOperationResult<T> NotFound<T>(string message = "Reçete kaydı bulunamadı.") =>
        new(PrescriptionOperationStatus.NotFound, default, message, null);

    public static PrescriptionOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new(PrescriptionOperationStatus.ValidationFailed, default, "Doğrulama hatası oluştu.", errors);

    public static PrescriptionOperationResult<T> Validation<T>(string key, string message) =>
        new(
            PrescriptionOperationStatus.ValidationFailed,
            default,
            message,
            new Dictionary<string, string[]>(StringComparer.Ordinal) { [key] = [message] });

    public static PrescriptionOperationResult<T> Conflict<T>(string message) =>
        new(PrescriptionOperationStatus.Conflict, default, message, null);

    public static PrescriptionOperationResult<T> Forbidden<T>(string message = "Bu reçeteye erişim veya işlem yetkiniz bulunmamaktadır.") =>
        new(PrescriptionOperationStatus.Forbidden, default, message, null);

    public static PrescriptionOperationResult<T> SafetyWarningOverrideRequired<T>(
        string message,
        IReadOnlyDictionary<string, string[]> warnings) =>
        new(PrescriptionOperationStatus.SafetyWarningOverrideRequired, default, message, warnings);
}
