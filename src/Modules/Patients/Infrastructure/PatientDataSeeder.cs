using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Domain;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Patients.Infrastructure;

public sealed class PatientDataSeeder(
    PatientsDbContext dbContext,
    TimeProvider timeProvider) : IPatientDataSeeder
{
    private readonly PatientsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public static readonly IReadOnlyList<DemoPatientDefinition> DemoPatients =
    [
        new(
            Guid.Parse("00000000-0000-0000-0000-000000000201"),
            Guid.Parse("00000000-0000-0000-0000-000000000109"), // Matching Identity patient user PersonId
            "MRN-2026-000001",
            "Ayşe",
            "Yılmaz",
            new DateOnly(1985, 4, 15),
            Gender.Female,
            "99900000001",
            "+90 555 123 4501",
            "DEMO-patient@hospital.invalid",
            new AddressValue("İstanbul", "Kadıköy", "Moda Cad. No: 12 D: 4", "34710"),
            new EmergencyContactValue("Mehmet Yılmaz", "Eşi", "+90 555 123 4502"),
            new CommunicationPreferencesValue(AllowSms: true, AllowEmail: true, PreferredLanguage: "tr")),
        new(
            Guid.Parse("00000000-0000-0000-0000-000000000202"),
            Guid.Parse("00000000-0000-0000-0000-000000000192"),
            "MRN-2026-000002",
            "Fatma",
            "Kaya",
            new DateOnly(1992, 8, 20),
            Gender.Female,
            "99900000002",
            "+90 555 123 4503",
            "DEMO-patient2@hospital.invalid",
            new AddressValue("Ankara", "Çankaya", "Tunalı Hilmi Cad. No: 5", "06680"),
            new EmergencyContactValue("Ali Kaya", "Babası", "+90 555 123 4504"),
            new CommunicationPreferencesValue(AllowSms: true, AllowEmail: false, PreferredLanguage: "tr")),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var def in DemoPatients)
        {
            var existing = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == def.Id || p.PersonId == def.PersonId || p.MedicalRecordNumber == def.MedicalRecordNumber, cancellationToken);

            if (existing is null)
            {
                var patient = Patient.Create(
                    def.Id,
                    def.PersonId,
                    def.MedicalRecordNumber,
                    def.FirstName,
                    def.LastName,
                    def.DateOfBirth,
                    def.Gender,
                    def.NationalIdSynthetic,
                    def.PhoneNumber,
                    def.Email,
                    def.Address,
                    def.EmergencyContact,
                    def.CommunicationPreferences,
                    now);

                _dbContext.Patients.Add(patient);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DemoPatientDefinition(
    Guid Id,
    Guid PersonId,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalIdSynthetic,
    string? PhoneNumber,
    string? Email,
    AddressValue? Address,
    EmergencyContactValue? EmergencyContact,
    CommunicationPreferencesValue? CommunicationPreferences);
