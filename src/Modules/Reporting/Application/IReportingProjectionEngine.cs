namespace HospitalManagement.Modules.Reporting.Application;

public interface IReportingProjectionEngine
{
    Task<bool> ProjectAppointmentEventAsync(
        AppointmentProjectedEvent evt,
        CancellationToken cancellationToken = default);

    Task<bool> ProjectDiagnosticEventAsync(
        DiagnosticProjectedEvent evt,
        CancellationToken cancellationToken = default);

    Task<bool> ProjectBedOccupancyEventAsync(
        BedOccupancyProjectedEvent evt,
        CancellationToken cancellationToken = default);

    Task<bool> ProjectPharmacyEventAsync(
        PharmacyProjectedEvent evt,
        CancellationToken cancellationToken = default);
}
