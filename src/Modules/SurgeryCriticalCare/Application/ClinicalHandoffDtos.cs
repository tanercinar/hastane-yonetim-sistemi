using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public sealed record InitiateClinicalHandoffDto(
    Guid PatientId,
    Guid? InpatientStayId,
    Guid? EncounterId,
    ClinicalAreaType SourceArea,
    string SourceLocationDetails,
    ClinicalAreaType DestinationArea,
    string DestinationLocationDetails,
    string Situation,
    string Background,
    string Assessment,
    string Recommendation,
    string? CriticalAlerts);

public sealed record ClinicalHandoffDto(
    Guid Id,
    string HandoffProtocolNumber,
    Guid PatientId,
    Guid? InpatientStayId,
    Guid? EncounterId,
    ClinicalAreaType SourceArea,
    string SourceLocationDetails,
    ClinicalAreaType DestinationArea,
    string DestinationLocationDetails,
    Guid HandingOverStaffId,
    Guid? ReceivingStaffId,
    string Situation,
    string Background,
    string Assessment,
    string Recommendation,
    string? CriticalAlerts,
    HandoffStatus Status,
    string? StatusReason,
    DateTime HandedOverAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);
