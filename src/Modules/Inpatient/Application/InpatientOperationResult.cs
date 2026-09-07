namespace HospitalManagement.Modules.Inpatient.Application;

public enum InpatientOperationStatus
{
    Success = 1,
    NotFound = 2,
    ValidationFailed = 3,
    Forbidden = 4,
    Conflict = 5,
}

public sealed class InpatientOperationResult<T>
{
    internal InpatientOperationResult(
        InpatientOperationStatus status,
        T? value,
        string? errorMessage,
        IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Value = value;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>(StringComparer.Ordinal);
    }

    public InpatientOperationStatus Status
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
    public bool Succeeded => Status == InpatientOperationStatus.Success;
}

public static class InpatientOperationResult
{
    public static InpatientOperationResult<T> Success<T>(T value) =>
        new(InpatientOperationStatus.Success, value, null, null);

    public static InpatientOperationResult<T> NotFound<T>(string message = "Kayıt bulunamadı.") =>
        new(InpatientOperationStatus.NotFound, default, message, null);

    public static InpatientOperationResult<T> Validation<T>(IReadOnlyDictionary<string, string[]> errors) =>
        new(InpatientOperationStatus.ValidationFailed, default, "Doğrulama hatası oluştu.", errors);

    public static InpatientOperationResult<T> Validation<T>(string propertyName, string error) =>
        new(InpatientOperationStatus.ValidationFailed, default, error, new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [propertyName] = [error],
        });

    public static InpatientOperationResult<T> Forbidden<T>(string message = "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.") =>
        new(InpatientOperationStatus.Forbidden, default, message, null);

    public static InpatientOperationResult<T> Conflict<T>(string message) =>
        new(InpatientOperationStatus.Conflict, default, message, null);
}
