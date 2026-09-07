namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record DicomInstanceDto(
    string SopInstanceUid,
    int InstanceNumber,
    string Modality,
    string ContentType,
    long FileSizeBytes,
    string ViewToken);

public sealed record DicomSeriesDto(
    string SeriesInstanceUid,
    int SeriesNumber,
    string Modality,
    string SeriesDescription,
    int NumberOfInstances,
    IReadOnlyList<DicomInstanceDto> Instances);

public sealed record DicomStudyMetadataDto(
    Guid StudyId,
    string AccessionNumber,
    string StudyInstanceUid,
    DateTime StudyDateUtc,
    string Modality,
    string StudyDescription,
    string PatientId,
    bool IsMockSimulation,
    IReadOnlyList<DicomSeriesDto> Series);

public sealed record DicomPreviewTokenDto(
    string Token,
    DateTime ExpiresAtUtc);
