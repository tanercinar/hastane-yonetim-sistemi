namespace HospitalManagement.Contracts.Interoperability;

public sealed record MockServerConfigResponse(
    Guid Id,
    string SystemType,
    bool IsEnabled,
    string FaultMode,
    int LatencyMilliseconds,
    int FailureRatePercentage,
    int MaxRetryAttempts,
    int TimeoutSeconds,
    DateTime UpdatedAtUtc);

public sealed record UpdateMockServerConfigRequest
{
    public string SystemType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string FaultMode { get; set; } = "None";
    public int LatencyMilliseconds { get; set; } = 50;
    public int FailureRatePercentage
    {
        get; set;
    }
    public int MaxRetryAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed record IntegrationMessageLogResponse(
    Guid Id,
    string CorrelationId,
    string SystemType,
    string Direction,
    string ActionName,
    string PayloadSummary,
    string Status,
    int RetryCount,
    int DurationMs,
    string? ErrorMessage,
    DateTime TimestampUtc);

public sealed record SimulateMockEngineRequest
{
    public string SystemType { get; set; } = "Fhir";
    public string ActionName { get; set; } = "SimulatedOperation";
    public string Payload { get; set; } = "{}";
}

public sealed record SimulateMockEngineResponse(
    bool IsSuccess,
    string? Data,
    string? ErrorMessage,
    int DurationMs,
    int RetryAttempts,
    string CorrelationId);

public sealed record Hl7InboundRequest
{
    public string RawEr7Message { get; set; } = string.Empty;
}

public sealed record Hl7InboundResponse(
    string MessageControlId,
    string AckCode,
    string TextMessage,
    string RawEr7Content);

public sealed record Hl7GenerateRequest
{
    public Guid? PatientId
    {
        get; set;
    }
    public string? ProtocolNumber
    {
        get; set;
    }
    public string? WardName
    {
        get; set;
    }
    public string? BedNumber
    {
        get; set;
    }
    public Guid? OrderId
    {
        get; set;
    }
    public string? TestCode
    {
        get; set;
    }
    public string? TestName
    {
        get; set;
    }
    public string? ResultValue
    {
        get; set;
    }
    public string? Units
    {
        get; set;
    }
}

public sealed record Hl7GenerateResponse(
    string MessageType,
    string RawEr7Message);

public sealed record Hl7DeadLetterResponse(
    Guid Id,
    string MessageControlId,
    string MessageType,
    string FailureReason,
    string PayloadSummary,
    DateTime ReceivedAtUtc,
    int RetryCount,
    bool IsResolved,
    DateTime? ResolvedAtUtc);

public sealed record DicomWorklistItemResponse(
    string AccessionNumber,
    string PatientId,
    string PatientName,
    string Modality,
    string ScheduledStationAeTitle,
    string ScheduledDate,
    string ScheduledTime,
    string ScheduledProcedureStepDescription,
    string RequestedProcedureId,
    string StudyInstanceUid,
    string Status);

public sealed record CreateDicomWorklistOrderRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public string Modality { get; set; } = "CT";
    public string ProcedureDescription { get; set; } = "Standart Tomografi İncelemesi";
    public string? AeTitle
    {
        get; set;
    }
}

public sealed record DicomInstanceMetadataResponse(
    string SopInstanceUid,
    int InstanceNumber,
    string SopClassUid,
    int Rows,
    int Columns,
    int BitsAllocated,
    string SyntheticImageUrl);

public sealed record DicomSeriesMetadataResponse(
    string SeriesInstanceUid,
    int SeriesNumber,
    string Modality,
    string SeriesDescription,
    int NumberOfInstances,
    List<DicomInstanceMetadataResponse> Instances);

public sealed record DicomStudyMetadataResponse(
    string StudyInstanceUid,
    string AccessionNumber,
    string PatientId,
    string PatientName,
    string StudyDate,
    string StudyDescription,
    string Modality,
    int NumberOfSeries,
    int NumberOfInstances,
    List<DicomSeriesMetadataResponse> Series);

public sealed record MhrsSlotResponse(
    string SlotId,
    Guid DoctorId,
    string DoctorName,
    string ClinicCode,
    string ClinicName,
    string HospitalCode,
    DateTime SlotDateTimeUtc,
    int DurationMinutes,
    bool IsAvailable);

public sealed record MhrsBookAppointmentRequest
{
    public string SlotId { get; set; } = string.Empty;
    public string PatientNationalId { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public Guid DoctorId
    {
        get; set;
    }
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public DateTime AppointmentDateTimeUtc
    {
        get; set;
    }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed record MhrsCancelAppointmentRequest
{
    public string Reason { get; set; } = "Kullanıcı talebi";
    public bool IsDoctor
    {
        get; set;
    }
}

public sealed record MhrsAppointmentResponse(
    Guid Id,
    string MhrsAppointmentId,
    string SlotId,
    string PatientNationalId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorName,
    string ClinicName,
    DateTime AppointmentDateTimeUtc,
    string Status,
    string IdempotencyKey,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record MhrsPatientAppointmentsQueryRequest
{
    public string PatientNationalId { get; set; } = string.Empty;
}

public sealed record MhrsSyncSummaryResponse(
    int TotalSynced,
    int ConflictsDetected,
    int NewAppointmentsAdded,
    int CancelledAppointments,
    DateTime SyncTimestampUtc);

public sealed record EnqueueENabizPackageRequest
{
    public int PackageType { get; set; } = 101;
    public Guid PatientId
    {
        get; set;
    }
    public string PatientNationalId { get; set; } = string.Empty;
    public bool HasPatientConsent { get; set; } = true;
    public string PayloadSummary { get; set; } = "{}";
}

public sealed record ENabizTransmissionResponse(
    Guid Id,
    string SysTakipNo,
    int PackageTypeCode,
    string PackageTypeName,
    Guid PatientId,
    string PatientNationalId,
    bool HasPatientConsent,
    string Status,
    string PayloadSummary,
    string ResponseCode,
    string ResponseMessage,
    int RetryCount,
    DateTime QueuedAtUtc,
    DateTime? SentAtUtc,
    DateTime? LastAttemptAtUtc);

public sealed record MedulaDemoOperationRequest
{
    public int OperationType { get; set; } = 1;
    public string RequestSummary { get; set; } = "{}";
}

public sealed record MedulaOutOfScopeRequest
{
    public string OperationName { get; set; } = string.Empty;
}

public sealed record MedulaOperationResultResponse(
    Guid Id,
    string OperationType,
    string Status,
    string StatusDescription,
    string DemoDisclaimer,
    string RequestSummary,
    string ResponseSummary,
    DateTime ProcessedAtUtc);
