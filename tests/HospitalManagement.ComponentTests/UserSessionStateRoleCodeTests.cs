using HospitalManagement.Contracts.Identity;
using HospitalManagement.Web.Client.Identity;

namespace HospitalManagement.ComponentTests;

public sealed class UserSessionStateRoleCodeTests
{
    [Theory]
    [InlineData("DOC", "doctor")]
    [InlineData("CHM", "doctor")]
    [InlineData("NUR", "nurse")]
    [InlineData("PHA", "pharmacist")]
    [InlineData("REG", "registration")]
    [InlineData("ADM", "admin")]
    [InlineData("MGR", "manager")]
    [InlineData("PAT", "patient")]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-KAPI")]
    public void CanonicalRoleCodesAreRecognizedByClientSession(string roleCode, string expectedRole)
    {
        var session = new UserSessionState(new IdentityApiClient(new HttpClient()));
        session.SetAccount(new CurrentAccountResponse(
            "DEMO-role@hospital.invalid",
            roleCode == "PAT" ? "Patient" : "Staff",
            Guid.NewGuid().ToString(),
            Roles: [roleCode]));

        Assert.Equal(expectedRole == "doctor", session.IsDoctor);
        Assert.Equal(expectedRole == "nurse", session.IsNurse);
        Assert.Equal(expectedRole == "pharmacist", session.IsPharmacist);
        Assert.Equal(expectedRole == "registration", session.IsRegistrationStaff);
        Assert.Equal(expectedRole == "admin", session.IsSystemAdmin);
        Assert.Equal(expectedRole == "manager", session.IsHospitalManager);
        Assert.Equal(expectedRole == "patient", session.IsPatient);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F05-KAPI")]
    public void PharmacistHomeRouteUsesWorklist()
    {
        var session = new UserSessionState(new IdentityApiClient(new HttpClient()));
        session.SetAccount(new CurrentAccountResponse(
            "DEMO-pharmacist@hospital.invalid",
            "Staff",
            Guid.NewGuid().ToString(),
            Roles: ["PHA"]));

        Assert.Equal("pharmacy/worklist", session.GetHomeRouteForCurrentRole());
    }
}
