namespace HospitalManagement.Modules.Patients.Domain;

public sealed record AddressValue(
    string City,
    string District,
    string Line1,
    string? PostalCode = null);
