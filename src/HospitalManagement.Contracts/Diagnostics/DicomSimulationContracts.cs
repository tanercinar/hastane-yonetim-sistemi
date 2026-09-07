namespace HospitalManagement.Contracts.Diagnostics;

public sealed record DicomInstanceResponse(
    string SopInstanceUid,
    int InstanceNumber,
    string Modality,
    string ContentType,
    long FileSizeBytes,
    string ViewToken);

public sealed record DicomSeriesResponse(
    string SeriesInstanceUid,
    int SeriesNumber,
    string Modality,
    string SeriesDescription,
    int NumberOfInstances,
    List<DicomInstanceResponse> Instances);

public sealed record DicomStudyMetadataResponse(
    Guid StudyId,
    string AccessionNumber,
    string StudyInstanceUid,
    DateTime StudyDateUtc,
    string Modality,
    string StudyDescription,
    string PatientId,
    bool IsMockSimulation,
    List<DicomSeriesResponse> Series);

public sealed record GenerateDicomPreviewTokenRequest
{
    public required string SopInstanceUid
    {
        get; init;
    }
}

public sealed record DicomPreviewTokenResponse(
    string Token,
    DateTime ExpiresAtUtc);

public sealed record DicomPreviewImageRequest
{
    public required string Token
    {
        get; init;
    }
}
