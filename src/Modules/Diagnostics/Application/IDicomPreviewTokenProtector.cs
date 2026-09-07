namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record DicomPreviewGrant(
    Guid StudyId,
    string SopInstanceUid,
    Guid AuthorizedPersonId,
    DateTime ExpiresAtUtc);

public interface IDicomPreviewTokenProtector
{
    string Protect(DicomPreviewGrant grant);

    bool TryUnprotect(string token, out DicomPreviewGrant? grant);
}
