using System.Globalization;
using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Mhrs;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class MhrsService : IMhrsService
{
    private readonly InteroperabilityDbContext _dbContext;
    private readonly IIntegrationMockEngine _mockEngine;
    private readonly TimeProvider _timeProvider;

    public MhrsService(
        InteroperabilityDbContext dbContext,
        IIntegrationMockEngine mockEngine,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private static readonly int[] SlotHours = [9, 10, 11, 13, 14, 15];
    private static readonly int[] SlotMinutes = [0, 15, 30, 45];

    public async Task<List<MhrsSlot>> QueryAvailableSlotsAsync(
        Guid? doctorId = null,
        string? clinicCode = null,
        DateTime? slotDate = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.SpecifyKind((slotDate ?? _timeProvider.GetUtcNow().UtcDateTime.Date).Date, DateTimeKind.Utc);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Mhrs,
            "QueryAvailableSlots",
            async () =>
            {
                var docId = doctorId ?? Guid.Parse("00000000-0000-0000-0000-000000000102");
                var clinic = clinicCode ?? "KARD-01";
                var clinicName = clinicCode == "DAH-01" ? "Dahiliye Polikliniği" : "Kardiyoloji Polikliniği";
                var docName = "Dr. Ahmet Tabip";

                // Fetch existing booked slots from DB for this doctor on targetDate
                var startUtc = targetDate;
                var endUtc = startUtc.AddDays(1);

                var bookedSlotIds = await _dbContext.MhrsAppointments
                    .Where(a => a.DoctorId == docId &&
                                a.AppointmentDateTimeUtc >= startUtc &&
                                a.AppointmentDateTimeUtc < endUtc &&
                                a.Status != MhrsAppointmentStatus.CancelledByPatient &&
                                a.Status != MhrsAppointmentStatus.CancelledByDoctor)
                    .Select(a => a.SlotId)
                    .ToListAsync(cancellationToken);

                var slots = new List<MhrsSlot>();

                foreach (var h in SlotHours)
                {
                    foreach (var m in SlotMinutes)
                    {
                        var slotTime = targetDate.Date.AddHours(h).AddMinutes(m);
                        var slotId = $"SLOT-{targetDate:yyyyMMdd}-{h:D2}{m:D2}-{docId.ToString()[..4]}";
                        var isAvailable = !bookedSlotIds.Contains(slotId);

                        slots.Add(new MhrsSlot(
                            SlotId: slotId,
                            DoctorId: docId,
                            DoctorName: docName,
                            ClinicCode: clinic,
                            ClinicName: clinicName,
                            HospitalCode: "DEMO-HOSP-01",
                            SlotDateTimeUtc: slotTime,
                            DurationMinutes: 15,
                            IsAvailable: isAvailable));
                    }
                }

                return slots;
            },
            payloadSummary: $"{{\"doctorId\": \"{doctorId}\", \"clinicCode\": \"{clinicCode}\", \"date\": \"{targetDate:yyyy-MM-dd}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "MHRS randevu saatleri sorgulanamadı.");
        }

        return result.Value ?? [];
    }

    public async Task<MhrsAppointmentDto> BookAppointmentAsync(
        BookMhrsAppointmentDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Mhrs,
            "BookAppointment",
            async () =>
            {
                // 1. Idempotency check
                var existingIdemp = await _dbContext.MhrsAppointments
                    .FirstOrDefaultAsync(a => a.IdempotencyKey == request.IdempotencyKey, cancellationToken);

                if (existingIdemp is not null)
                {
                    return MapToDto(existingIdemp);
                }

                // 2. Conflict check (Slot double-booking)
                var existingSlotBooking = await _dbContext.MhrsAppointments
                    .FirstOrDefaultAsync(a => a.SlotId == request.SlotId &&
                                              a.Status != MhrsAppointmentStatus.CancelledByPatient &&
                                              a.Status != MhrsAppointmentStatus.CancelledByDoctor,
                                         cancellationToken);

                if (existingSlotBooking is not null)
                {
                    throw new InvalidOperationException($"Seçilen randevu saati ({request.SlotId}) başka bir hasta tarafından alınmıştır.");
                }

                // 3. Create appointment
                var appointment = new MhrsAppointmentRecord(
                    request.SlotId,
                    request.PatientNationalId,
                    request.PatientFullName,
                    request.DoctorId,
                    request.DoctorName,
                    request.ClinicName,
                    request.AppointmentDateTimeUtc,
                    request.IdempotencyKey);

                _dbContext.MhrsAppointments.Add(appointment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return MapToDto(appointment);
            },
            payloadSummary: $"{{\"slotId\": \"{request.SlotId}\", \"tcKimlik\": \"{request.PatientNationalId}\", \"doctorId\": \"{request.DoctorId}\"}}",
            correlationId: $"CORR-MHRS-{request.IdempotencyKey}",
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "MHRS randevu alma işlemi başarısız oldu.");
        }

        return result.Value ?? throw new InvalidOperationException("Randevu kaydı oluşturulamadı.");
    }

    public async Task<MhrsAppointmentDto> CancelAppointmentAsync(
        string mhrsAppointmentId,
        string reason,
        bool isDoctor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mhrsAppointmentId);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Mhrs,
            "CancelAppointment",
            async () =>
            {
                var appointment = await _dbContext.MhrsAppointments
                    .FirstOrDefaultAsync(a => a.MhrsAppointmentId == mhrsAppointmentId, cancellationToken);

                if (appointment is null)
                {
                    throw new KeyNotFoundException($"MHRS randevusu bulunamadı: {mhrsAppointmentId}");
                }

                appointment.Cancel(reason, isDoctor);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return MapToDto(appointment);
            },
            payloadSummary: $"{{\"mhrsAppointmentId\": \"{mhrsAppointmentId}\", \"reason\": \"{reason}\"}}",
            correlationId: $"CORR-MHRS-CANCEL-{mhrsAppointmentId}",
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "MHRS randevu iptali başarısız oldu.");
        }

        return result.Value ?? throw new InvalidOperationException("İptal işlemi tamamlanamadı.");
    }

    public async Task<List<MhrsAppointmentDto>> GetPatientAppointmentsAsync(
        string patientNationalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientNationalId);

        var list = await _dbContext.MhrsAppointments
            .AsNoTracking()
            .Where(a => a.PatientNationalId == patientNationalId)
            .OrderByDescending(a => a.AppointmentDateTimeUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<MhrsSyncSummaryDto> SyncWithLocalScheduleAsync(
        DateTime syncDate,
        CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.SpecifyKind(syncDate.Date, DateTimeKind.Utc);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.Mhrs,
            "SyncWithLocalSchedule",
            async () =>
            {
                var startUtc = targetDate;
                var endUtc = startUtc.AddDays(1);

                var list = await _dbContext.MhrsAppointments
                    .Where(a => a.AppointmentDateTimeUtc >= startUtc && a.AppointmentDateTimeUtc < endUtc)
                    .ToListAsync(cancellationToken);

                var total = list.Count;
                var cancelled = list.Count(a => a.Status == MhrsAppointmentStatus.CancelledByPatient || a.Status == MhrsAppointmentStatus.CancelledByDoctor);
                var active = total - cancelled;

                return new MhrsSyncSummaryDto(
                    TotalSynced: total,
                    ConflictsDetected: 0,
                    NewAppointmentsAdded: active,
                    CancelledAppointments: cancelled,
                    SyncTimestampUtc: _timeProvider.GetUtcNow().UtcDateTime);
            },
            payloadSummary: $"{{\"syncDate\": \"{syncDate:yyyy-MM-dd}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "MHRS senkronizasyon işlemi başarısız oldu.");
        }

        return result.Value ?? new MhrsSyncSummaryDto(0, 0, 0, 0, DateTime.UtcNow);
    }

    private static MhrsAppointmentDto MapToDto(MhrsAppointmentRecord a) =>
        new(
            a.Id,
            a.MhrsAppointmentId,
            a.SlotId,
            a.PatientNationalId,
            a.PatientFullName,
            a.DoctorId,
            a.DoctorName,
            a.ClinicName,
            a.AppointmentDateTimeUtc,
            a.Status.ToString(),
            a.IdempotencyKey,
            a.CancellationReason,
            a.CreatedAtUtc,
            a.UpdatedAtUtc);
}
