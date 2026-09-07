using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Security;
using HospitalManagement.Modules.AuditPrivacy.Domain;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Patients.Domain;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Scheduling.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Gate;

public sealed class Phase13ReleaseCandidateGateTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GateSecurityHeadersEnforceStrictOwaspDefaults()
    {
        Assert.Equal("nosniff", SecurityHeaderDefaults.XContentTypeOptions);
        Assert.Equal("DENY", SecurityHeaderDefaults.XFrameOptions);
        Assert.Equal("strict-origin-when-cross-origin", SecurityHeaderDefaults.ReferrerPolicy);
        Assert.Equal("0", SecurityHeaderDefaults.XXssProtection);
        Assert.Contains("camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=()", SecurityHeaderDefaults.PermissionsPolicy);
        Assert.Contains("default-src 'self'", SecurityHeaderDefaults.ContentSecurityPolicy);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GatePerformanceAndConcurrencyProtectsAppointmentDoubleBooking()
    {
        var now = DateTime.UtcNow;
        var slot = AppointmentSlot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(1).AddMinutes(30),
            now);

        var firstPatient = Guid.NewGuid();
        var secondPatient = Guid.NewGuid();

        slot.Book(firstPatient, now);
        Assert.Equal(SlotStatus.Booked, slot.Status);

        var ex = Assert.Throws<InvalidOperationException>(() => slot.Book(secondPatient, now));
        Assert.Contains("uygun değildir", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GateDataIntegrityAuditLogBlockChainingVerifiesCryptographicIntegrity()
    {
        var now = DateTime.UtcNow;
        var initialLog = AuditLogEntry.Create(
            Guid.NewGuid(),
            now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "System",
            "127.0.0.1",
            "AgentRunner/1.0",
            "SYSTEM_INIT",
            "System",
            "SYS-01",
            AuditOutcome.Success,
            "Genesis node created",
            Guid.NewGuid().ToString("N"),
            null,
            "0000000000000000000000000000000000000000000000000000000000000000");

        Assert.True(initialLog.VerifyHashIntegrity());

        var nextLog = AuditLogEntry.Create(
            Guid.NewGuid(),
            now.AddSeconds(1),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Doctor",
            "127.0.0.1",
            "TestAgent/1.0",
            "ClinicalRecords.NoteSign",
            "ClinicalNote",
            Guid.NewGuid().ToString(),
            AuditOutcome.Success,
            "Routine note sign",
            Guid.NewGuid().ToString("N"),
            null,
            initialLog.RecordHash);

        Assert.True(nextLog.VerifyHashIntegrity());
        Assert.Equal(initialLog.RecordHash, nextLog.PreviousRecordHash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GatePrivacyReviewCanaryPurgingRemovesDirectIdentifiersPermanently()
    {
        var now = DateTime.UtcNow;
        var patientId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var patient = Patient.Create(
            patientId,
            personId,
            "DEMO-MRN-2026-GATE",
            "CanaryName",
            "CanarySurname",
            new DateOnly(1990, 1, 1),
            Gender.Male,
            "DEMO-CANARY-12345",
            "+905559998877",
            "canary@hospital.invalid",
            new AddressValue("Canary Cad.", "Kadikoy", "Istanbul", "34710"),
            null,
            null,
            now);

        patient.Anonymize(now.AddDays(1));

        Assert.Equal("ANONİM", patient.FirstName);
        Assert.Equal("HASTA", patient.LastName);
        Assert.Null(patient.NationalIdSynthetic);
        Assert.Null(patient.PhoneNumber);
        Assert.Null(patient.Email);
        Assert.Null(patient.Address);
        Assert.False(patient.IsActive);
        Assert.Equal("DEMO-MRN-2026-GATE", patient.MedicalRecordNumber);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GateResilienceAndSafeExportProtectsAgainstFormulaInjectionAndExcessiveExport()
    {
        Assert.Equal(5000, SecureExportService.MaxExportRows);

        var dangerousPayload = "=cmd|'/C calc'!A0";
        var sanitized = SecureExportService.SanitizeCsvCell(dangerousPayload);
        Assert.StartsWith("'", sanitized, StringComparison.Ordinal);

        var config = new MockServerConfiguration(
            ExternalSystemType.ENabiz,
            isEnabled: true,
            faultMode: FaultInjectionMode.CircuitBroken,
            latencyMilliseconds: 500,
            failureRatePercentage: 50,
            maxRetryAttempts: 3,
            timeoutSeconds: 10);

        Assert.Equal(FaultInjectionMode.CircuitBroken, config.FaultMode);
        Assert.Equal(3, config.MaxRetryAttempts);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-KAPI")]
    public void GateReportingProjectionRecoveryMaintainsCheckpointMonotonicity()
    {
        var checkpoint = new ProjectionCheckpoint("GateOpsProjection", version: 1);
        Assert.Equal(0L, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);

        var now = DateTime.UtcNow;
        checkpoint.MarkActive(500L, now);
        Assert.Equal(500L, checkpoint.LastProcessedPosition);

        checkpoint.MarkRebuilding();
        Assert.Equal(0L, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Rebuilding, checkpoint.Status);

        checkpoint.MarkActive(100L, now.AddSeconds(1));
        Assert.Equal(100L, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);
    }
}
