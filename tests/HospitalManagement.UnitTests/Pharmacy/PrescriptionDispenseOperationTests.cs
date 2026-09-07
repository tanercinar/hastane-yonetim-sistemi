using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.UnitTests.Pharmacy;

public sealed class PrescriptionDispenseOperationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void CreateStoresNonClinicalIdempotencyReceipt()
    {
        var id = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var prescriptionId = Guid.NewGuid();
        var completedAtUtc = DateTime.UtcNow;

        var operation = PrescriptionDispenseOperation.Create(
            id,
            idempotencyKey,
            prescriptionId,
            "  SHA256-FINGERPRINT  ",
            completedAtUtc);

        Assert.Equal(id, operation.Id);
        Assert.Equal(idempotencyKey, operation.IdempotencyKey);
        Assert.Equal(prescriptionId, operation.PrescriptionId);
        Assert.Equal("SHA256-FINGERPRINT", operation.RequestFingerprint);
        Assert.Equal(completedAtUtc, operation.CompletedAtUtc);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("idempotency")]
    [InlineData("prescription")]
    [InlineData("fingerprint")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F05-G07")]
    public void CreateRejectsIncompleteReceipt(string invalidField)
    {
        var id = invalidField == "id" ? Guid.Empty : Guid.NewGuid();
        var idempotencyKey = invalidField == "idempotency" ? Guid.Empty : Guid.NewGuid();
        var prescriptionId = invalidField == "prescription" ? Guid.Empty : Guid.NewGuid();
        var fingerprint = invalidField == "fingerprint" ? " " : "SHA256-FINGERPRINT";

        Assert.Throws<ArgumentException>(() =>
            PrescriptionDispenseOperation.Create(
                id,
                idempotencyKey,
                prescriptionId,
                fingerprint,
                DateTime.UtcNow));
    }
}
