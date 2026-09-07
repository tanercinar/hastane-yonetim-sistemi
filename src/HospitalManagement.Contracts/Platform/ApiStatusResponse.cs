namespace HospitalManagement.Contracts.Platform;

public sealed record ApiStatusResponse(
    string Service,
    string ApiVersion,
    string DataMode,
    string CorrelationId);
