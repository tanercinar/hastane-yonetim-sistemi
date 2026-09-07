using System.Globalization;
using System.Text;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.BuildingBlocks.Authorization;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class SecureExportService : ISecureExportService
{
    public const int MaxExportRows = 5000;
    private readonly ReportingDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;

    public SecureExportService(
        ReportingDbContext dbContext,
        IAuditEventPublisher auditPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    }

    public async Task<SecureExportResultDto> ExportCsvAsync(
        SecureExportRequestDto request,
        Guid? actorUserId,
        Guid? actorPersonId,
        string? actorRole,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReportType);

        var reportTypeNormalized = request.ReportType.Trim().ToLowerInvariant();
        var sb = new StringBuilder();
        var rowCount = 0;
        string fileName;

        switch (reportTypeNormalized)
        {
            case "outpatient-metrics":
            case "outpatient":
            case "poliklinik":
                {
                    var query = _dbContext.DailyOutpatientMetrics.AsNoTracking();
                    if (request.StartDate.HasValue)
                    {
                        query = query.Where(m => m.Date >= request.StartDate.Value);
                    }
                    if (request.EndDate.HasValue)
                    {
                        query = query.Where(m => m.Date <= request.EndDate.Value);
                    }
                    if (request.DepartmentId.HasValue)
                    {
                        query = query.Where(m => m.DepartmentId == request.DepartmentId.Value);
                    }

                    var total = await query.CountAsync(cancellationToken);
                    if (total > MaxExportRows)
                    {
                        throw new InvalidOperationException($"Dışa aktarma sınırı aşıldı (en fazla {MaxExportRows} satır).");
                    }

                    var data = await query.OrderBy(m => m.Date).ThenBy(m => m.DepartmentName).ToListAsync(cancellationToken);
                    rowCount = data.Count;

                    sb.AppendLine("Tarih,Bölüm Adı,Hekim Adı,Toplam Randevu,Planlanan,Giriş Yapmış,Muayenede,Tamamlanan,İptal,Gelmedi");
                    foreach (var item in data)
                    {
                        sb.AppendLine(string.Join(",",
                            SanitizeCsvCell(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                            SanitizeCsvCell(item.DepartmentName),
                            SanitizeCsvCell(actorRole == HospitalRoles.HospitalManager
                                ? "DEIDENTIFIED"
                                : item.DoctorName),
                            item.TotalAppointments,
                            item.ScheduledCount,
                            item.CheckedInCount,
                            item.InProgressCount,
                            item.CompletedCount,
                            item.CancelledCount,
                            item.NoShowCount));
                    }

                    fileName = $"poliklinik-raporu-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.csv";
                    break;
                }

            case "diagnostic-workload":
            case "diagnostics":
            case "tanisal":
                {
                    var query = _dbContext.DiagnosticWorkloadMetrics.AsNoTracking();
                    if (request.StartDate.HasValue)
                    {
                        query = query.Where(m => m.Date >= request.StartDate.Value);
                    }
                    if (request.EndDate.HasValue)
                    {
                        query = query.Where(m => m.Date <= request.EndDate.Value);
                    }

                    var total = await query.CountAsync(cancellationToken);
                    if (total > MaxExportRows)
                    {
                        throw new InvalidOperationException($"Dışa aktarma sınırı aşıldı (en fazla {MaxExportRows} satır).");
                    }

                    var data = await query.OrderBy(m => m.Date).ThenBy(m => m.ModalityOrSection).ToListAsync(cancellationToken);
                    rowCount = data.Count;

                    sb.AppendLine("Tarih,Modalite/Bölüm,Toplam İstem,Bekleyen Numune,İşlemde,Sonuçlanan,Kritik Sonuç,Ortalama TAT (dk)");
                    foreach (var item in data)
                    {
                        sb.AppendLine(string.Join(",",
                            SanitizeCsvCell(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                            SanitizeCsvCell(item.ModalityOrSection),
                            item.TotalOrders,
                            item.PendingSpecimenCount,
                            item.ProcessingCount,
                            item.FinalizedCount,
                            item.CriticalCount,
                            item.AvgTurnaroundMinutes));
                    }

                    fileName = $"tanisal-hizmetler-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.csv";
                    break;
                }

            case "bed-occupancy":
            case "occupancy":
            case "yatak":
                {
                    var query = _dbContext.BedOccupancyMetrics.AsNoTracking();
                    if (request.StartDate.HasValue)
                    {
                        query = query.Where(m => m.Date >= request.StartDate.Value);
                    }
                    if (request.EndDate.HasValue)
                    {
                        query = query.Where(m => m.Date <= request.EndDate.Value);
                    }
                    if (request.DepartmentId.HasValue)
                    {
                        query = query.Where(m => m.DepartmentId == request.DepartmentId.Value);
                    }

                    var total = await query.CountAsync(cancellationToken);
                    if (total > MaxExportRows)
                    {
                        throw new InvalidOperationException($"Dışa aktarma sınırı aşıldı (en fazla {MaxExportRows} satır).");
                    }

                    var data = await query.OrderBy(m => m.Date).ThenBy(m => m.DepartmentName).ToListAsync(cancellationToken);
                    rowCount = data.Count;

                    sb.AppendLine("Tarih,Bölüm Adı,Servis Türü,Toplam Yatak,Dolu Yatak,Müsait Yatak,Doluluk Oranı %,Bekleyen Transfer");
                    foreach (var item in data)
                    {
                        sb.AppendLine(string.Join(",",
                            SanitizeCsvCell(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                            SanitizeCsvCell(item.DepartmentName),
                            SanitizeCsvCell(item.WardType),
                            item.TotalBeds,
                            item.OccupiedBeds,
                            item.AvailableBeds,
                            item.OccupancyRatePercentage,
                            item.PendingTransferCount));
                    }

                    fileName = $"yatak-doluluk-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.csv";
                    break;
                }

            case "pharmacy-dispensing":
            case "pharmacy":
            case "eczane":
                {
                    var query = _dbContext.PharmacyDispensingMetrics.AsNoTracking();
                    if (request.StartDate.HasValue)
                    {
                        query = query.Where(m => m.Date >= request.StartDate.Value);
                    }
                    if (request.EndDate.HasValue)
                    {
                        query = query.Where(m => m.Date <= request.EndDate.Value);
                    }

                    var total = await query.CountAsync(cancellationToken);
                    if (total > MaxExportRows)
                    {
                        throw new InvalidOperationException($"Dışa aktarma sınırı aşıldı (en fazla {MaxExportRows} satır).");
                    }

                    var data = await query.OrderBy(m => m.Date).ToListAsync(cancellationToken);
                    rowCount = data.Count;

                    sb.AppendLine("Tarih,Toplam Reçete,Bekleyen Çıkış,Teslim Edilen,Düşük Stok Kalemi,Miat Yaklaşan Lot");
                    foreach (var item in data)
                    {
                        sb.AppendLine(string.Join(",",
                            SanitizeCsvCell(item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                            item.TotalPrescriptions,
                            item.PendingDispenseCount,
                            item.DispensedCount,
                            item.LowStockItemCount,
                            item.NearExpiryLotCount));
                    }

                    fileName = $"eczane-hareketleri-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.csv";
                    break;
                }

            default:
                throw new ArgumentException($"Desteklenmeyen rapor türü: '{request.ReportType}'.", nameof(request));
        }

        // Audit log kaydı yayınla (Klinik ve PHI detay içermez)
        await _auditPublisher.PublishAsync(new AuditEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            actorUserId,
            actorPersonId,
            actorRole,
            null,
            null,
            "report.operations.export",
            "ReportExport",
            request.ReportType,
            AuditOutcome.Success,
            null,
            correlationId,
            $"{{\"reportType\": \"{request.ReportType}\", \"rowCount\": {rowCount}}}"),
            cancellationToken);

        // UTF-8 BOM ile byte array oluştur (Excel Türkçe karakter desteği için)
        var utf8Bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var finalBytes = new byte[utf8Bom.Length + contentBytes.Length];
        Buffer.BlockCopy(utf8Bom, 0, finalBytes, 0, utf8Bom.Length);
        Buffer.BlockCopy(contentBytes, 0, finalBytes, utf8Bom.Length, contentBytes.Length);

        return new SecureExportResultDto(fileName, "text/csv; charset=utf-8", finalBytes, rowCount);
    }

    public static string SanitizeCsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var result = value;

        // Formula injection koruması: Hücre tehlikeli sembollerle başlıyorsa tek tırnak ekle
        if (result.Length > 0 && (result[0] == '=' || result[0] == '+' || result[0] == '-' || result[0] == '@' || result[0] == '\t' || result[0] == '\r'))
        {
            result = "'" + result;
        }

        // RFC 4180 CSV kaçışı: Virgül, tırnak veya yeni satır varsa çift tırnağa al
        if (result.Contains('"') || result.Contains(',') || result.Contains('\n') || result.Contains('\r'))
        {
            return $"\"{result.Replace("\"", "\"\"")}\"";
        }

        return result;
    }
}
