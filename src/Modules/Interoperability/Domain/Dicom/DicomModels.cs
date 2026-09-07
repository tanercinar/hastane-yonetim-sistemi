namespace HospitalManagement.Modules.Interoperability.Domain.Dicom;

public enum DicomWorklistStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Discontinued = 4,
}

public sealed record DicomWorklistItem(
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
    DicomWorklistStatus Status);

public sealed record DicomInstanceMetadata(
    string SopInstanceUid,
    int InstanceNumber,
    string SopClassUid,
    int Rows,
    int Columns,
    int BitsAllocated,
    string SyntheticImageUrl);

public sealed record DicomSeriesMetadata(
    string SeriesInstanceUid,
    int SeriesNumber,
    string Modality,
    string SeriesDescription,
    int NumberOfInstances,
    List<DicomInstanceMetadata> Instances);

public sealed record DicomStudyMetadata(
    string StudyInstanceUid,
    string AccessionNumber,
    string PatientId,
    string PatientName,
    string StudyDate,
    string StudyDescription,
    string Modality,
    int NumberOfSeries,
    int NumberOfInstances,
    List<DicomSeriesMetadata> Series);
