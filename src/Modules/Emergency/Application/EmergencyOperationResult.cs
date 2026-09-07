namespace HospitalManagement.Modules.Emergency.Application;

public enum EmergencyOperationStatus
{
    Success = 1,
    NotFound = 2,
    ValidationFailed = 3,
    Forbidden = 4,
    Conflict = 5,
}

public sealed class EmergencyOperationResult<T>
{
    internal EmergencyOperationResult(
        EmergencyOperationStatus status,
        T? value,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
    }

    public EmergencyOperationStatus Status
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
    public bool Succeeded => Status == EmergencyOperationStatus.Success;
}

public static class EmergencyOperationResult
{
    public static EmergencyOperationResult<T> Success<T>(T value) =>
        new(EmergencyOperationStatus.Success, value, null, null);

    public static EmergencyOperationResult<T> NotFound<T>(string message = "Kayıt bulunamadı.") =>
        new(EmergencyOperationStatus.NotFound, default, message, null);

    public static EmergencyOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new(EmergencyOperationStatus.ValidationFailed, default, "Doğrulama hatası oluştu.", errors);

    public static EmergencyOperationResult<T> Validation<T>(string propertyName, string error) =>
        new(EmergencyOperationStatus.ValidationFailed, default, error, new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [propertyName] = [error],
        });

    public static EmergencyOperationResult<T> Forbidden<T>(string message = "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.") =>
        new(EmergencyOperationStatus.Forbidden, default, message, null);

    public static EmergencyOperationResult<T> Conflict<T>(string message) =>
        new(EmergencyOperationStatus.Conflict, default, message, null);
}
