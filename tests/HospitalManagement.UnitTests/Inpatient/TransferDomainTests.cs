using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class TransferDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 30, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequestTransferShouldInitializeWithRequestedStatusAndVersionOne()
    {
        var id = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var srcWard = Guid.NewGuid();
        var srcBed = Guid.NewGuid();
        var tgtWard = Guid.NewGuid();
        var requester = Guid.NewGuid();

        var transfer = InpatientTransfer.Request(
            id,
            admissionId,
            patientId,
            srcWard,
            srcBed,
            tgtWard,
            null,
            "Yoğun bakıma transfer",
            "Vital bulgular instabil",
            requester,
            FixedNow);

        Assert.Equal(id, transfer.Id);
        Assert.Equal(admissionId, transfer.AdmissionId);
        Assert.Equal(patientId, transfer.PatientId);
        Assert.Equal(srcWard, transfer.SourceWardId);
        Assert.Equal(srcBed, transfer.SourceBedId);
        Assert.Equal(tgtWard, transfer.TargetWardId);
        Assert.Null(transfer.TargetBedId);
        Assert.Equal(TransferStatus.Requested, transfer.Status);
        Assert.Equal(1, transfer.Version);
    }

    [Fact]
    public void AcceptTransferShouldSetAcceptedAndTargetBed()
    {
        var transfer = CreateSampleTransfer();
        var nurseId = Guid.NewGuid();
        var bedId = Guid.NewGuid();

        transfer.Accept(nurseId, bedId, FixedNow.AddMinutes(10));

        Assert.Equal(TransferStatus.Accepted, transfer.Status);
        Assert.Equal(nurseId, transfer.AcceptedByUserId);
        Assert.Equal(bedId, transfer.TargetBedId);
        Assert.Equal(2, transfer.Version);
    }

    [Fact]
    public void CompleteTransferShouldSetCompletedStatusAndTargetBed()
    {
        var transfer = CreateSampleTransfer();
        transfer.Accept(Guid.NewGuid(), null, FixedNow.AddMinutes(10));

        var targetBedId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();

        transfer.Complete(nurseId, targetBedId, FixedNow.AddMinutes(30));

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(targetBedId, transfer.TargetBedId);
        Assert.Equal(nurseId, transfer.CompletedByUserId);
        Assert.Equal(3, transfer.Version);
    }

    [Fact]
    public void CancelTransferShouldSetCancelledAndReason()
    {
        var transfer = CreateSampleTransfer();
        var nurseId = Guid.NewGuid();

        transfer.Cancel(nurseId, "Hasta transferden vazgeçti", FixedNow.AddMinutes(15));

        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
        Assert.Equal("Hasta transferden vazgeçti", transfer.CancellationReason);
        Assert.Equal(nurseId, transfer.CancelledByUserId);
        Assert.Equal(2, transfer.Version);
    }

    [Fact]
    public void AdmissionTransferTransitionsShouldUpdateStateCorrectly()
    {
        var admission = InpatientAdmission.Request(
            Guid.NewGuid(),
            "DEMO-ADM-20260830-112233",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Akut Koroner Sendrom",
            "I20.0",
            "Anstabil Angina Pektoris",
            "LowSodium",
            30,
            IsolationType.None,
            3,
            FixedNow);

        admission.Accept(Guid.NewGuid(), FixedNow);
        var oldBedId = Guid.NewGuid();
        admission.Admit(oldBedId, FixedNow);

        admission.InitiateTransfer(FixedNow.AddHours(1));
        Assert.Equal(AdmissionStatus.Transferring, admission.Status);

        var newWardId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();
        var newBedId = Guid.NewGuid();
        admission.CompleteTransfer(newWardId, newDepartmentId, newBedId, FixedNow.AddHours(2));

        Assert.Equal(AdmissionStatus.Admitted, admission.Status);
        Assert.Equal(newWardId, admission.AdmittingWardId);
        Assert.Equal(newDepartmentId, admission.DepartmentId);
        Assert.Equal(newBedId, admission.AssignedBedId);
    }

    private static InpatientTransfer CreateSampleTransfer() =>
        InpatientTransfer.Request(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Yoğun bakıma transfer",
            "Vital bulgular instabil",
            Guid.NewGuid(),
            FixedNow);
}
