using HospitalManagement.Modules.Interoperability.Domain.Dicom;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record CreateDicomWorklistOrderDto(
    Guid PatientId,
    string Modality,
    string ProcedureDescription,
    string? AeTitle = null);

public interface IDicomPacsService
{
    Task<List<DicomWorklistItem>> QueryModalityWorklistAsync(
        string? modality = null,
        string? scheduledDate = null,
        string? aeTitle = null,
        CancellationToken cancellationToken = default);

    Task<DicomWorklistItem> CreateWorklistOrderAsync(
        CreateDicomWorklistOrderDto request,
        CancellationToken cancellationToken = default);

    Task<DicomStudyMetadata?> QueryStudyMetadataAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default);

    Task<List<DicomStudyMetadata>> QueryPatientStudiesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
