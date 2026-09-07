namespace HospitalManagement.Modules.Patients.Domain;

public sealed record EmergencyContactValue(
    string FullName,
    string Relationship,
    string PhoneNumber);
