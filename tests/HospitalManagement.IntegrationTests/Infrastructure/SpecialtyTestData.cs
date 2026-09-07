using HospitalManagement.Modules.Patients.Domain;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests.Infrastructure;

internal static class SpecialtyTestData
{
    internal static async Task<Guid> SeedNewbornPatientAsync(
        ApiWebApplicationFactory application,
        DateOnly dateOfBirth,
        Gender gender)
    {
        ArgumentNullException.ThrowIfNull(application);

        var patientId = Guid.NewGuid();
        var patient = Patient.Create(
            patientId,
            Guid.NewGuid(),
            $"DEMO-NB-{patientId:N}"[..24],
            "DEMO-Yenidoğan",
            "Hasta",
            dateOfBirth,
            gender,
            nationalIdSynthetic: null,
            phoneNumber: null,
            email: null,
            address: null,
            emergencyContact: null,
            communicationPreferences: new CommunicationPreferencesValue(
                AllowSms: false,
                AllowEmail: false,
                PreferredLanguage: "tr"),
            DateTime.UtcNow);

        await using var scope = application.Services.CreateAsyncScope();
        var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        patientsDb.Patients.Add(patient);
        await patientsDb.SaveChangesAsync();
        return patientId;
    }
}
