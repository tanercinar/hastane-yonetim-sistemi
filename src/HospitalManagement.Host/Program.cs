using System.Globalization;
using HospitalManagement.Host.Api;
using HospitalManagement.Host.Audit;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.ClinicalRecords;
using HospitalManagement.Host.Components;
using HospitalManagement.Host.Configuration;
using HospitalManagement.Host.Database;
using HospitalManagement.Host.Diagnostics;
using HospitalManagement.Host.Emergency;
using HospitalManagement.Host.Identity;
using HospitalManagement.Host.Inpatient;
using HospitalManagement.Host.Interoperability;
using HospitalManagement.Host.Notifications;
using HospitalManagement.Host.Observability;
using HospitalManagement.Host.Patients;
using HospitalManagement.Host.Pharmacy;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Host.Reporting;
using HospitalManagement.Host.Scheduling;
using HospitalManagement.Host.Specialty;
using HospitalManagement.Host.Surgery;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Infrastructure;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure;
using HospitalManagement.Modules.Interoperability.Infrastructure;
using HospitalManagement.Modules.Notifications.Infrastructure;
using HospitalManagement.Modules.Organization.Infrastructure;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Infrastructure;
using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Infrastructure;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservabilityFoundation();

builder.Services
    .AddLocalization()
    .AddValidatedHospitalManagementConfiguration(builder.Configuration, builder.Environment)
    .AddDatabaseFoundation(builder.Configuration)
    .AddApiFoundation(builder.Configuration)
    .AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOrganizationPersistence(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddIdentityAccess(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddAuditPrivacyPersistence(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddPatientsModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddSchedulingModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddNotificationsModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddClinicalRecordsModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddPharmacyModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddDiagnosticsModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddInpatientModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddEmergencyModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddSurgeryCriticalCareModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddSpecialtyCareModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddInteroperabilityModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddReportingModule(
    builder.Configuration,
    DatabaseOptions.ConnectionStringName);
builder.Services.AddHospitalAuthorization();
builder.Services.AddScoped<IPrescriptionAccessContext, PrescriptionAccessContext>();
builder.Services.AddScoped<IDiagnosticsAccessContext, DiagnosticsAccessContext>();
builder.Services.AddScoped<InpatientAccessControl>();
builder.Services.AddScoped<Phase8ClinicalAccessControl>();
builder.Services.AddScoped<SpecialtyCareAccessControl>();
builder.Services.AddScoped<InpatientMedicationOrderValidator>();
builder.Services.AddScoped<IInpatientMedicationOrderValidator>(services =>
    services.GetRequiredService<InpatientMedicationOrderValidator>());
builder.Services.AddScoped<IDiagnosticsRealtimeNotifier, DiagnosticsRealtimeNotifier>();
builder.Services.AddScoped<IInpatientRealtimeNotifier, InpatientRealtimeNotifier>();
builder.Services.AddScoped<IEmergencyRealtimeNotifier, EmergencyRealtimeNotifier>();
builder.Services.AddScoped<IReportingRealtimeNotifier, ReportingRealtimeNotifier>();
builder.Services.AddSingleton<IIdentityMessageSender, MockSmtpIdentityMessageSender>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IHospitalRealtimeNotifier, HospitalRealtimeNotifier>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    options.DefaultRequestCulture = new RequestCulture(turkishCulture);
    options.SupportedCultures = [turkishCulture];
    options.SupportedUICultures = [turkishCulture];
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var demoDataScope = app.Services.CreateAsyncScope();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IOrganizationDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IIdentityDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IPatientDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<ISchedulingDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IClinicalRecordsDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IMedicationCatalogDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<HospitalManagement.Modules.Diagnostics.Application.ILabCatalogDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<IInpatientDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<HospitalManagement.Modules.Emergency.Application.IEmergencyDataSeeder>()
        .SeedAsync();
    await demoDataScope.ServiceProvider
        .GetRequiredService<ISurgeryDataSeeder>()
        .SeedAsync();

    // The local portfolio environment has no external care-team feed. Establish only the
    // documented synthetic relationship so the DEMO clinical journeys are executable.
    var demoCareRelationships = demoDataScope.ServiceProvider
        .GetRequiredService<CareRelationshipRegistry>();
    var demoPatientPersonId = Guid.Parse("00000000-0000-0000-0000-000000000109");
    demoCareRelationships.EstablishCareRelationship(
        Guid.Parse("00000000-0000-0000-0000-000000000102"),
        demoPatientPersonId);
    demoCareRelationships.EstablishCareRelationship(
        Guid.Parse("00000000-0000-0000-0000-000000000103"),
        demoPatientPersonId);
}

if (!app.Environment.IsEnvironment("Testing"))
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
}

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseApiFoundation();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapApiFoundationEndpoints();
app.MapIdentityEndpoints();
app.MapAuditEndpoints();
app.MapPatientEndpoints();
app.MapSchedulingEndpoints();
app.MapNotificationEndpoints();
app.MapClinicalRecordsEndpoints();
app.MapPharmacyEndpoints();
app.MapDiagnosticsEndpoints();
app.MapInpatientEndpoints();
app.MapEmergencyEndpoints();
app.MapSurgeryEndpoints();
app.MapIcuEndpoints();
app.MapClinicalHandoffEndpoints();
app.MapSpecialtyCareEndpoints();
app.MapInteroperabilityEndpoints();
app.MapReportingEndpoints();
app.MapHub<HospitalHub>("/hubs/hospital");
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(HospitalManagement.Web.Client._Imports).Assembly,
        typeof(HospitalManagement.UI.UiAssemblyMarker).Assembly);

app.Run();

public partial class Program
{
}
