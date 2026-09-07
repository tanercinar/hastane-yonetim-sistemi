using Bunit;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Web.Client.Pages.Surgery;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class PerioperativeRecordComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F08-G05")]
    public void PerioperativeRecordFlowRendersMilestonesAnesthesiaAndFindings()
    {
        var bookingId = Guid.NewGuid();
        var fakeApi = new FakeSurgeryApiClient(bookingId);
        Services.AddSingleton<ISurgeryApiClient>(fakeApi);

        var cut = Render<PerioperativeRecordFlow>(parameters => parameters.Add(p => p.BookingId, bookingId));

        // Verify Title and Milestone headers
        Assert.Contains("Perioperatif Ameliyat &amp; Anestezi Kaydı", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("1. Ameliyathane Zaman Milestoneları", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("2. Anestezi Simülasyonu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("3. İntraoperatif Cerrahi Bulgular", cut.Markup, StringComparison.Ordinal);

        // Verify Protocol & Anesthesia Type
        Assert.Contains("DEMO-SURG-20260831-2001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Laparoskopik Kolesistektomi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Genel Anestezi (Endotrakeal Entübasyon)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Gazlı Bez, İğne &amp; Cerrahi Alet Sayımı", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSurgeryApiClient : ISurgeryApiClient
    {
        private readonly Guid _bookingId;

        public FakeSurgeryApiClient(Guid bookingId)
        {
            _bookingId = bookingId;
        }

        public Task<SurgeryBookingResponse?> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SurgeryBookingResponse?>(new SurgeryBookingResponse(
                Id: _bookingId,
                BookingProtocolNumber: "DEMO-SURG-20260831-2001",
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
                Status: "Scheduled",
                PreOpChecklist: null,
                ClinicalNotes: null,
                CancellationReason: null,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: DateTime.UtcNow,
                Version: 1));
        }

        public Task<PerioperativeRecordResponse?> GetPerioperativeRecordByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PerioperativeRecordResponse?>(null);
        }

        public Task<List<OperatingRoomResponse>> GetOperatingRoomsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<OperatingRoomResponse>());

        public Task<SurgeryBookingResponse?> CreateBookingAsync(CreateSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> RescheduleBookingAsync(Guid id, RescheduleSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> RecordPreOpChecklistAsync(Guid id, RecordPreOpChecklistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<SurgeryBookingResponse?> CancelBookingAsync(Guid id, CancelSurgeryBookingRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SurgeryBookingResponse?>(null);

        public Task<List<SurgeryBookingResponse>> GetBookingsAsync(Guid? operatingRoomId = null, Guid? leadSurgeonId = null, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SurgeryBookingResponse>());

        public Task<PerioperativeRecordResponse?> SavePerioperativeRecordAsync(SavePerioperativeRecordRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeRecordResponse?> GetPerioperativeRecordByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeRecordResponse?> SignPerioperativeRecordAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeRecordResponse?>(null);

        public Task<PerioperativeCorrectionResponse?> AddPerioperativeCorrectionAsync(Guid id, AddPerioperativeCorrectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PerioperativeCorrectionResponse?>(null);
    }
}
