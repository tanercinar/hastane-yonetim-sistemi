using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientBoardService : IInpatientBoardService
{
    private readonly InpatientDbContext _dbContext;

    public InpatientBoardService(InpatientDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<InpatientBoardItemDto>> GetInpatientBoardAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? riskLevel = null,
        bool? isolationOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Admissions
            .AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Admitted || a.Status == AdmissionStatus.Transferring);

        if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            query = query.Where(a => a.AdmittingWardId == wardId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(a => a.DepartmentId == departmentId.Value);
        }

        if (isolationOnly == true)
        {
            query = query.Where(a => a.IsolationRequired != IsolationType.None);
        }

        var admissions = await query.ToListAsync(cancellationToken);

        var wardIds = admissions.Select(a => a.AdmittingWardId).Distinct().ToList();
        var bedIds = admissions.Where(a => a.AssignedBedId.HasValue).Select(a => a.AssignedBedId!.Value).Distinct().ToList();
        var admissionIds = admissions.Select(a => a.Id).ToList();

        var wards = await _dbContext.Wards
            .AsNoTracking()
            .Where(w => wardIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => bedIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var roomIds = beds.Values.Select(b => b.RoomId).Distinct().ToList();
        var rooms = await _dbContext.Rooms
            .AsNoTracking()
            .Where(r => roomIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var pendingTransfers = await _dbContext.Transfers
            .AsNoTracking()
            .Where(t => admissionIds.Contains(t.AdmissionId) &&
                (t.Status == TransferStatus.Requested || t.Status == TransferStatus.Accepted))
            .Select(t => t.AdmissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var pendingTransferSet = new HashSet<Guid>(pendingTransfers);
        var now = DateTime.UtcNow;

        var items = admissions.Select(a =>
        {
            wards.TryGetValue(a.AdmittingWardId, out var ward);
            Bed? bed = null;
            if (a.AssignedBedId.HasValue)
            {
                beds.TryGetValue(a.AssignedBedId.Value, out bed);
            }

            string? roomNumber = null;
            if (bed is not null && rooms.TryGetValue(bed.RoomId, out var room))
            {
                roomNumber = room.RoomNumber;
            }

            var daysInHospital = a.AdmittedAtUtc.HasValue
                ? Math.Max(1, (int)Math.Ceiling((now - a.AdmittedAtUtc.Value).TotalDays))
                : 1;

            var fallRiskLevel = CalculateFallRiskLevel(a.FallRiskScore);
            var hasPendingTransfer = pendingTransferSet.Contains(a.Id);

            return new InpatientBoardItemDto(
                a.Id,
                a.AdmissionNumber,
                a.PatientId,
                $"DEMO-P-{a.PatientId.ToString().Substring(0, 8)}",
                $"DEMO Hasta ({a.PatientId.ToString().Substring(0, 4)})",
                null,
                null,
                a.AdmittingWardId,
                ward?.Name ?? "Servis",
                a.AssignedBedId ?? Guid.Empty,
                bed?.BedNumber ?? "-",
                roomNumber,
                a.AttendingDoctorId,
                $"DEMO Uzm. Dr. ({a.AttendingDoctorId.ToString().Substring(0, 4)})",
                a.DepartmentId,
                a.DiagnosisCode ?? "R69",
                a.DiagnosisDescription ?? a.AdmissionReason,
                a.DietType,
                a.FallRiskScore,
                fallRiskLevel,
                a.IsolationRequired,
                hasPendingTransfer,
                a.AdmittedAtUtc ?? a.RequestedAtUtc,
                daysInHospital,
                a.EstimatedStayDays,
                0);
        }).ToList();

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            items = items.Where(i => string.Equals(i.FallRiskLevel, riskLevel, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items
            .OrderBy(i => i.WardName)
            .ThenBy(i => i.BedNumber)
            .ToList();
    }

    public async Task<InpatientPatientSummaryDto?> GetPatientSummaryAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var admission = await _dbContext.Admissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);

        if (admission is null)
        {
            return null;
        }

        var ward = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == admission.AdmittingWardId, cancellationToken);

        Bed? bed = null;
        string? roomNumber = null;
        if (admission.AssignedBedId.HasValue)
        {
            bed = await _dbContext.Beds
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == admission.AssignedBedId.Value, cancellationToken);

            if (bed is not null)
            {
                var room = await _dbContext.Rooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == bed.RoomId, cancellationToken);
                roomNumber = room?.RoomNumber;
            }
        }

        var hasPendingTransfer = await _dbContext.Transfers
            .AsNoTracking()
            .AnyAsync(t => t.AdmissionId == admission.Id &&
                (t.Status == TransferStatus.Requested || t.Status == TransferStatus.Accepted), cancellationToken);

        var now = DateTime.UtcNow;
        var daysInHospital = admission.AdmittedAtUtc.HasValue
            ? Math.Max(1, (int)Math.Ceiling((now - admission.AdmittedAtUtc.Value).TotalDays))
            : 1;

        return new InpatientPatientSummaryDto(
            admission.Id,
            admission.AdmissionNumber,
            admission.PatientId,
            $"DEMO Hasta ({admission.PatientId.ToString().Substring(0, 4)})",
            ward?.Name ?? "Servis",
            bed?.BedNumber ?? "-",
            roomNumber,
            $"DEMO Uzm. Dr. ({admission.AttendingDoctorId.ToString().Substring(0, 4)})",
            admission.DiagnosisDescription ?? admission.AdmissionReason,
            admission.DietType,
            admission.FallRiskScore,
            CalculateFallRiskLevel(admission.FallRiskScore),
            admission.IsolationRequired,
            admission.AdmittedAtUtc ?? admission.RequestedAtUtc,
            daysInHospital,
            hasPendingTransfer);
    }

    private static string CalculateFallRiskLevel(int score) =>
        score switch
        {
            >= 50 => "High",
            >= 25 => "Medium",
            _ => "Low",
        };
}
