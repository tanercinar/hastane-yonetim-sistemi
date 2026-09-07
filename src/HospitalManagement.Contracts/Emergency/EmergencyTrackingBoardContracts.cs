namespace HospitalManagement.Contracts.Emergency;

public sealed record EmergencyBoardSummaryResponse(
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

public sealed record EmergencyBoardWorklistItemResponse(
    Guid AdmissionId,
    string EmergencyProtocolNumber,
    Guid PatientId,
    string ArrivalType,
    string ChiefComplaint,
    string Status,
    string? TriageLevel,
    string? TriageCategoryReason,
    DateTime AdmittedAtUtc,
    DateTime? TriagedAtUtc,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone,
    int WaitingMinutes,
    int? DoctorWaitingMinutes);
