using HospitalManagement.Modules.Emergency.Domain;

namespace HospitalManagement.Modules.Emergency.Application;

public sealed record EmergencyBoardSummaryDto(
    int TotalActiveAdmissions,
    int WaitingTriageCount,
    int TriagedWaitingDoctorCount,
    int InEvaluationCount,
    int InObservationCount,
    int AdmittedPendingTransferCount,
    int TodayDischargedCount,
    int Red1Count,
    int Red2Count,
    int YellowCount,
    int GreenCount,
    int BlackCount,
    double AverageWaitMinutesTriage,
    double AverageWaitMinutesDoctor,
    double AverageLengthOfStayMinutes);

public sealed record EmergencyBoardWorklistItemDto(
    Guid AdmissionId,
    string EmergencyProtocolNumber,
    Guid PatientId,
    EmergencyArrivalType ArrivalType,
    string ChiefComplaint,
    EmergencyAdmissionStatus Status,
    TriageLevel? TriageLevel,
    string? TriageCategoryReason,
    DateTime AdmittedAtUtc,
    DateTime? TriagedAtUtc,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone,
    int WaitingMinutes,
    int? DoctorWaitingMinutes);
