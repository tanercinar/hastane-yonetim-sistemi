namespace HospitalManagement.Modules.Diagnostics.Application;

public enum DiagnosticOperationStatus
{
    Success = 1,
    NotFound = 2,
    ValidationFailed = 3,
    Forbidden = 4,
    Conflict = 5,
}

public sealed class DiagnosticOrderOperationResult<T>
{
    internal DiagnosticOrderOperationResult(
        DiagnosticOperationStatus status,
        T? value,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
    }

    public DiagnosticOperationStatus Status
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
    public bool Succeeded => Status == DiagnosticOperationStatus.Success;
}

public static class DiagnosticOrderOperationResult
{
    public static DiagnosticOrderOperationResult<T> Success<T>(T value) =>
        new(DiagnosticOperationStatus.Success, value, null, null);

    public static DiagnosticOrderOperationResult<T> NotFound<T>(string message = "Kayıt bulunamadı.") =>
        new(DiagnosticOperationStatus.NotFound, default, message, null);

    public static DiagnosticOrderOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new(DiagnosticOperationStatus.ValidationFailed, default, "Doğrulama hatası oluştu.", errors);

    public static DiagnosticOrderOperationResult<T> Validation<T>(string propertyName, string error) =>
        new(DiagnosticOperationStatus.ValidationFailed, default, error, new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [propertyName] = [error],
        });

    public static DiagnosticOrderOperationResult<T> Forbidden<T>(string message = "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.") =>
        new(DiagnosticOperationStatus.Forbidden, default, message, null);

    public static DiagnosticOrderOperationResult<T> Conflict<T>(string message) =>
        new(DiagnosticOperationStatus.Conflict, default, message, null);
}
