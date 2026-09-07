using Bunit;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Web.Client.Pages.Surgery;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class SurgerySchedulingComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F08-G04")]
    public void SurgerySchedulingRendersKPIsRoomsAndBookingsList()
    {
        var fakeApi = new FakeSurgeryApiClient();
        Services.AddSingleton<ISurgeryApiClient>(fakeApi);

        var cut = Render<SurgeryScheduling>();

        // Verify Title and KPI Headers
        Assert.Contains("Ameliyathane &amp; Cerrahi Planlama", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Toplam Ameliyat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pre-Op Hazır", cut.Markup, StringComparison.Ordinal);

        // Verify Booking Item
        Assert.Contains("DEMO-SURG-20260831-1001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Laparoskopik Kolesistektomi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-OR-01", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Onaylandı (6/6)", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSurgeryApiClient : ISurgeryApiClient
    {
        public Task<List<OperatingRoomResponse>> GetOperatingRoomsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<OperatingRoomResponse>
            {
                new(Guid.NewGuid(), "DEMO-OR-01", "Salon 1 - Genel Cerrahi", true, 1, null),
                new(Guid.NewGuid(), "DEMO-OR-02", "Salon 2 - Kalp Damar", true, 1, null),
            });
        }

        public Task<List<SurgeryBookingResponse>> GetBookingsAsync(
            Guid? operatingRoomId = null,
            Guid? leadSurgeonId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var checklist = new PreOpChecklistResponse(
                ConsentSigned: true,
                AnesthesiaClearance: true,
                NpoConfirmed: true,
                BloodProductsReserved: true,
                SiteMarked: true,
                AllergyChecked: true,
                IsFullyCleared: true,
                CompletedByStaffId: Guid.NewGuid(),
                CompletedAtUtc: DateTime.UtcNow,
                Notes: "Tamamlandı");

            return Task.FromResult(new List<SurgeryBookingResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    BookingProtocolNumber: "DEMO-SURG-20260831-1001",
                    PatientId: Guid.NewGuid(),
                    EncounterId: null,
                    DepartmentId: Guid.NewGuid(),
                    DepartmentName: "Genel Cerrahi",
                    ProcedureName: "Laparoskopik Kolesistektomi",
                    ProcedureCode: "DEMO-PRC-CHOLE",
                    Urgency: "Elective",
                    OperatingRoomId: Guid.NewGuid(),
                    LeadSurgeonDoctorId: Guid.NewGuid(),
                    AnesthesiologistDoctorId: Guid.NewGuid(),
                    OperatingNurseStaffId: null,
                    ScheduledStartTimeUtc: DateTime.UtcNow.Date.AddHours(9),
                    ScheduledEndTimeUtc: DateTime.UtcNow.Date.AddHours(11),
                    Status: "PreOpCleared",
                    PreOpChecklist: checklist,
                    ClinicalNotes: "Laparoskopik set",
                    CancellationReason: null,
                    CreatedAtUtc: DateTime.UtcNow,
                    UpdatedAtUtc: DateTime.UtcNow,
                    Version: 1),
            });
        }

        public Task<SurgeryBookingResponse?> CreateBookingAsync(CreateSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> RescheduleBookingAsync(Guid id, RescheduleSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> RecordPreOpChecklistAsync(Guid id, RecordPreOpChecklistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> CancelBookingAsync(Guid id, CancelSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<PerioperativeRecordResponse?> SavePerioperativeRecordAsync(SavePerioperativeRecordRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeRecordResponse?> GetPerioperativeRecordByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeRecordResponse?> GetPerioperativeRecordByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeRecordResponse?> SignPerioperativeRecordAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeCorrectionResponse?> AddPerioperativeCorrectionAsync(Guid id, AddPerioperativeCorrectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeCorrectionResponse?>(null);
    }
}
