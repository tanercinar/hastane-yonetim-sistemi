using HospitalManagement.Modules.Interoperability.Domain.ENabiz;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class ENabizDomainTests
{
    [Fact]
    public void TransmissionRecordWithConsentShouldBeQueued()
    {
        // Arrange & Act
        var record = new ENabizTransmissionRecord(
            ENabizPackageType.Package101HastaKayit,
            Guid.NewGuid(),
            "11111111110",
            hasPatientConsent: true,
            "DEMO payload — hasta kayıt");

        // Assert
        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.NotEmpty(record.SysTakipNo);
        Assert.Equal(ENabizPackageType.Package101HastaKayit, record.PackageType);
        Assert.Equal("11111111110", record.PatientNationalId);
        Assert.True(record.HasPatientConsent);
        Assert.Equal(ENabizTransmissionStatus.Queued, record.Status);
        Assert.Equal("QUEUED", record.ResponseCode);
        Assert.Equal(0, record.RetryCount);
        Assert.Null(record.SentAtUtc);
        Assert.Null(record.LastAttemptAtUtc);
    }

    [Fact]
    public void TransmissionRecordWithoutConsentShouldBeConsentDenied()
    {
        // Arrange & Act
        var record = new ENabizTransmissionRecord(
            ENabizPackageType.Package105Recete,
            Guid.NewGuid(),
            "11111111110",
            hasPatientConsent: false,
            "DEMO payload — reçete");

        // Assert
        Assert.Equal(ENabizTransmissionStatus.ConsentDenied, record.Status);
        Assert.Equal("ERR_CONSENT_DENIED", record.ResponseCode);
        Assert.Contains("rıza", record.ResponseMessage);
    }

    [Fact]
    public void MarkTransmittingShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var record = new ENabizTransmissionRecord(
            ENabizPackageType.Package103LaboratuvarSonuc,
            Guid.NewGuid(),
            "11111111110",
            hasPatientConsent: true,
            "DEMO lab result payload");

        // Act
        record.MarkTransmitting();

        // Assert
        Assert.Equal(ENabizTransmissionStatus.Transmitting, record.Status);
        Assert.NotNull(record.LastAttemptAtUtc);
    }

    [Fact]
    public void MarkSuccessShouldSetSuccessfulStatusAndSentTimestamp()
    {
        // Arrange
        var record = new ENabizTransmissionRecord(
            ENabizPackageType.Package106Epikriz,
            Guid.NewGuid(),
            "11111111110",
            hasPatientConsent: true,
            "DEMO epikriz payload");
        record.MarkTransmitting();

        // Act
        record.MarkSuccess("SYS_200_106", "Epikriz başarıyla gönderildi.");

        // Assert
        Assert.Equal(ENabizTransmissionStatus.Successful, record.Status);
        Assert.Equal("SYS_200_106", record.ResponseCode);
        Assert.NotNull(record.SentAtUtc);
    }

    [Fact]
    public void MarkFailedShouldIncrementRetryCount()
    {
        // Arrange
        var record = new ENabizTransmissionRecord(
            ENabizPackageType.Package102HizmetIstem,
            Guid.NewGuid(),
            "11111111110",
            hasPatientConsent: true,
            "DEMO hizmet istem payload");
        record.MarkTransmitting();

        // Act
        record.MarkFailed("ERR_GATEWAY_TIMEOUT", "Sunucu zaman aşımı.");
        record.MarkFailed("ERR_GATEWAY_TIMEOUT", "Sunucu zaman aşımı.");

        // Assert
        Assert.Equal(ENabizTransmissionStatus.Failed, record.Status);
        Assert.Equal(2, record.RetryCount);
        Assert.Equal("ERR_GATEWAY_TIMEOUT", record.ResponseCode);
    }
}
