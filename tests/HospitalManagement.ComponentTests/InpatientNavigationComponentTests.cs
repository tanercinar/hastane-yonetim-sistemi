using Bunit;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class InpatientNavigationComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F07-KAPI")]
    public void SystemAdministratorDoesNotSeeClinicalInpatientNavigation()
    {
        using var context = CreateContext(
            role: "ADM",
            permissions: ["identity.role.assign", "audit.technical.view"]);

        var component = RenderLayout(context);

        Assert.All(
            component.FindAll("a[href^='inpatient/']"),
            link => Assert.True(link.HasAttribute("hidden")));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F07-KAPI")]
    public void DoctorOnlySeesInpatientNavigationForGrantedPermissions()
    {
        using var context = CreateContext(
            role: "DOC",
            permissions: ["admission.request", "discharge.complete"]);

        var component = RenderLayout(context);

        AssertVisible(component, "inpatient/board");
        AssertVisible(component, "inpatient/dashboard");
        AssertVisible(component, "inpatient/admissions");
        AssertVisible(component, "inpatient/discharges");
        AssertHidden(component, "inpatient/nursing");
        AssertHidden(component, "inpatient/emar");
        AssertHidden(component, "inpatient/transfers");
        AssertHidden(component, "inpatient/beds");
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F07-KAPI")]
    public void NurseDoesNotSeeDischargeNavigationWithoutPermission()
    {
        using var context = CreateContext(
            role: "NUR",
            permissions:
            [
                "admission.accept",
                "bed.assign",
                "bed.transfer",
                "care-plan.manage",
                "medication.administer",
            ]);

        var component = RenderLayout(context);

        AssertVisible(component, "inpatient/nursing");
        AssertVisible(component, "inpatient/emar");
        AssertVisible(component, "inpatient/transfers");
        AssertVisible(component, "inpatient/beds");
        AssertHidden(component, "inpatient/discharges");
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-KAPI")]
    public void SystemAdministratorDoesNotSeePhase8ClinicalNavigation()
    {
        using var context = CreateContext(
            role: "ADM",
            permissions: ["identity.role.assign", "audit.technical.view"]);

        var component = RenderLayout(context);

        AssertHidden(component, "emergency/admissions");
        AssertHidden(component, "emergency/board");
        AssertHidden(component, "surgery/scheduling");
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-KAPI")]
    public void DoctorOnlySeesPhase8NavigationForGrantedPermissions()
    {
        using var context = CreateContext(
            role: "DOC",
            permissions: ["emergency.triage.record", "critical-care.record"]);

        var component = RenderLayout(context);

        AssertVisible(component, "emergency/admissions");
        AssertVisible(component, "emergency/board");
        AssertHidden(component, "surgery/scheduling");
        AssertVisible(component, "icu/beds");
        AssertVisible(component, "clinical-handoffs");
    }

    private static BunitContext CreateContext(string role, IReadOnlyList<string> permissions)
    {
        var context = new BunitContext();
        context.Services.AddLocalization();

        var session = new UserSessionState(new IdentityApiClient(new HttpClient()));
        session.SetAccount(new CurrentAccountResponse(
            $"DEMO-{role.ToLowerInvariant()}@hospital.invalid",
            "Staff",
            Guid.NewGuid().ToString(),
            Roles: [role],
            Permissions: permissions));
        context.Services.AddSingleton(session);
        return context;
    }

    private static void AssertVisible(IRenderedComponent<MainLayout> component, string href) =>
        Assert.False(component.Find($"a[href='{href}']").HasAttribute("hidden"));

    private static void AssertHidden(IRenderedComponent<MainLayout> component, string href) =>
        Assert.True(component.Find($"a[href='{href}']").HasAttribute("hidden"));

    private static IRenderedComponent<MainLayout> RenderLayout(BunitContext context) =>
        context.Render<MainLayout>(parameters => parameters.Add(
            layout => layout.Body,
            (RenderFragment)(builder => builder.AddContent(0, "İçerik"))));
}
