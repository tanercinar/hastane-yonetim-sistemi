using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class AdmissionDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequestAdmissionShouldInitializeWithRequestedStatusAndVersionOne()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var orderingDoc = Guid.NewGuid();
        var attendingDoc = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var wardId = Guid.NewGuid();

        var admission = InpatientAdmission.Request(
            id,
            "DEMO-ADM-20260830-112233",
            patientId,
            null,
            orderingDoc,
            attendingDoc,
            deptId,
            wardId,
            "Akut Koroner Sendrom",
            "I20.0",
            "Anstabil Angina Pektoris",
            "LowSodium",
            30,
            IsolationType.None,
            3,
            FixedNow);

        Assert.Equal(id, admission.Id);
        Assert.Equal("DEMO-ADM-20260830-112233", admission.AdmissionNumber);
        Assert.Equal(patientId, admission.PatientId);
        Assert.Equal(AdmissionStatus.Requested, admission.Status);
        Assert.Equal(1, admission.Version);
        Assert.Equal(30, admission.FallRiskScore);
        Assert.Equal("LowSodium", admission.DietType);
        Assert.Null(admission.AssignedBedId);
        Assert.Null(admission.AdmittedAtUtc);
    }

    [Fact]
    public void AcceptAdmissionWhenRequestedShouldSetAcceptedAndIncrementVersion()
    {
        var admission = CreateSampleAdmission();
        var nurseUserId = Guid.NewGuid();

        admission.Accept(nurseUserId, FixedNow.AddMinutes(15));

        Assert.Equal(AdmissionStatus.Accepted, admission.Status);
        Assert.Equal(nurseUserId, admission.AcceptedByUserId);
        Assert.Equal(FixedNow.AddMinutes(15), admission.AcceptedAtUtc);
        Assert.Equal(2, admission.Version);
    }

    [Fact]
    public void AcceptAdmissionWhenAlreadyAdmittedShouldThrowInvalidOperationException()
    {
        var admission = CreateSampleAdmission();
        admission.Accept(Guid.NewGuid(), FixedNow);
        admission.Admit(Guid.NewGuid(), FixedNow.AddMinutes(30));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            admission.Accept(Guid.NewGuid(), FixedNow.AddHours(1)));
        Assert.Contains("Yalnızca istem aşamasındaki", ex.Message);
    }

    [Fact]
    public void AdmitPatientWhenAcceptedShouldSetAdmittedAndAssignedBed()
    {
        var admission = CreateSampleAdmission();
        admission.Accept(Guid.NewGuid(), FixedNow);

        var bedId = Guid.NewGuid();
        admission.Admit(bedId, FixedNow.AddMinutes(45));

        Assert.Equal(AdmissionStatus.Admitted, admission.Status);
        Assert.Equal(bedId, admission.AssignedBedId);
        Assert.Equal(FixedNow.AddMinutes(45), admission.AdmittedAtUtc);
        Assert.Equal(3, admission.Version);
    }

    [Fact]
    public void CancelAdmissionShouldSetCancelledAndReason()
    {
        var admission = CreateSampleAdmission();

        admission.Cancel("Hasta acil yatışı reddetti.", FixedNow.AddHours(1));

        Assert.Equal(AdmissionStatus.Cancelled, admission.Status);
        Assert.Equal("Hasta acil yatışı reddetti.", admission.CancellationReason);
        Assert.Equal(FixedNow.AddHours(1), admission.CancelledAtUtc);
        Assert.Equal(2, admission.Version);
    }

    [Fact]
    public void DischargePatientWhenAdmittedShouldSetDischargedAndSummary()
    {
        var admission = CreateSampleAdmission();
        admission.Accept(Guid.NewGuid(), FixedNow);
        admission.Admit(Guid.NewGuid(), FixedNow.AddMinutes(30));

        admission.Discharge("Şifa ile taburcu edildi. 1 hafta sonra kontrol önerildi.", FixedNow.AddDays(3));

        Assert.Equal(AdmissionStatus.Discharged, admission.Status);
        Assert.Equal("Şifa ile taburcu edildi. 1 hafta sonra kontrol önerildi.", admission.DischargeSummary);
        Assert.Equal(FixedNow.AddDays(3), admission.DischargedAtUtc);
        Assert.Equal(4, admission.Version);
    }

    [Fact]
    public void UpdateCareDetailsShouldModifyValuesAndIncrementVersion()
    {
        var admission = CreateSampleAdmission();
        var newAttendingDoc = Guid.NewGuid();

        admission.UpdateCareDetails(newAttendingDoc, "Diabetic", 45, IsolationType.Contact, FixedNow.AddDays(1));

        Assert.Equal(newAttendingDoc, admission.AttendingDoctorId);
        Assert.Equal("Diabetic", admission.DietType);
        Assert.Equal(45, admission.FallRiskScore);
        Assert.Equal(IsolationType.Contact, admission.IsolationRequired);
        Assert.Equal(2, admission.Version);
    }

    private static InpatientAdmission CreateSampleAdmission() =>
        InpatientAdmission.Request(
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
}
