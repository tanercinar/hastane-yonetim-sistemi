namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface IPatientSpecialtyPortalService
{
    Task<PatientSpecialtyPortalDto> GetPublishedRecordsAsync(
        Guid patientId,
        Guid actorPersonId,
        CancellationToken cancellationToken = default);
}
