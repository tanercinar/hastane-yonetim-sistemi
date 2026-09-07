using System.Collections.Concurrent;

namespace HospitalManagement.Host.Authorization;

public sealed class CareRelationshipRegistry
{
    private readonly ConcurrentDictionary<(Guid ClinicianPersonId, Guid PatientPersonId), bool> _relationships = new();

    public void EstablishCareRelationship(Guid clinicianPersonId, Guid patientPersonId)
    {
        if (clinicianPersonId != Guid.Empty && patientPersonId != Guid.Empty)
        {
            _relationships[(clinicianPersonId, patientPersonId)] = true;
        }
    }

    public void TerminateCareRelationship(Guid clinicianPersonId, Guid patientPersonId)
    {
        _relationships.TryRemove((clinicianPersonId, patientPersonId), out _);
    }

    public bool HasCareRelationship(Guid clinicianPersonId, Guid patientPersonId)
    {
        return clinicianPersonId != Guid.Empty
            && patientPersonId != Guid.Empty
            && _relationships.ContainsKey((clinicianPersonId, patientPersonId));
    }

    public void Clear()
    {
        _relationships.Clear();
    }
}
