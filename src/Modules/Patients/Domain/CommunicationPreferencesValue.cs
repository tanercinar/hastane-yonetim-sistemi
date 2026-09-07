namespace HospitalManagement.Modules.Patients.Domain;

public sealed record CommunicationPreferencesValue(
    bool AllowSms = true,
    bool AllowEmail = true,
    string PreferredLanguage = "tr");
