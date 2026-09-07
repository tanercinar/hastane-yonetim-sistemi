using System.Globalization;
using HospitalManagement.UI.Services;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Emergency;
using HospitalManagement.Web.Client.Identity;
using HospitalManagement.Web.Client.Inpatient;
using HospitalManagement.Web.Client.Interoperability;
using HospitalManagement.Web.Client.Notifications;
using HospitalManagement.Web.Client.Patients;
using HospitalManagement.Web.Client.Pharmacy;
using HospitalManagement.Web.Client.Reporting;
using HospitalManagement.Web.Client.Scheduling;
using HospitalManagement.Web.Client.Services;
using HospitalManagement.Web.Client.Specialty;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddLocalization();
builder.Services.AddSingleton<IPlatformInfoService, WebPlatformInfoService>();
builder.Services.AddSingleton<IPlatformConnectivityService, WebPlatformConnectivityService>();
builder.Services.AddSingleton<IAppSecureStorage, WebPlatformSecureStorage>();
builder.Services.AddSingleton<IPlatformNotificationService, WebPlatformNotificationService>();
builder.Services.AddScoped<IPlatformBrowserService, WebPlatformBrowserService>();
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute),
});
builder.Services.AddScoped<IdentityApiClient>();
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<IPatientApiClient, PatientApiClient>();
builder.Services.AddScoped<ISchedulingApiClient, SchedulingApiClient>();
builder.Services.AddScoped<INotificationApiClient, NotificationApiClient>();
builder.Services.AddScoped<IPharmacyApiClient, PharmacyApiClient>();
builder.Services.AddScoped<IDiagnosticsApiClient, DiagnosticsApiClient>();
builder.Services.AddScoped<IInpatientApiClient, InpatientApiClient>();
builder.Services.AddScoped<IEmergencyApiClient, EmergencyApiClient>();
builder.Services.AddScoped<ISurgeryApiClient, SurgeryApiClient>();
builder.Services.AddScoped<IIcuApiClient, IcuApiClient>();
builder.Services.AddScoped<IClinicalHandoffApiClient, ClinicalHandoffApiClient>();
builder.Services.AddScoped<ISpecialtyCareApiClient, SpecialtyCareApiClient>();
builder.Services.AddScoped<IPatientSpecialtyPortalApiClient, PatientSpecialtyPortalApiClient>();
builder.Services.AddScoped<IInteroperabilityApiClient, InteroperabilityApiClient>();
builder.Services.AddScoped<IReportingApiClient, ReportingApiClient>();

var defaultCulture = CultureInfo.GetCultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

await builder.Build().RunAsync();
