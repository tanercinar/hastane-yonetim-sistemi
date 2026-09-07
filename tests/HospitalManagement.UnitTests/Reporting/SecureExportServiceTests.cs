using System.Text;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class SecureExportServiceTests
{
    private sealed class InMemoryAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Theory]
    [InlineData("=cmd|' /C calc'!A0", "'=cmd|' /C calc'!A0")]
    [InlineData("+1+2", "'+1+2")]
    [InlineData("-2-3", "'-2-3")]
    [InlineData("@SUM(1,2)", "\"'@SUM(1,2)\"")]
    [InlineData("@TEST", "'@TEST")]
    [InlineData("\tDANGEROUS", "'\tDANGEROUS")]
    [InlineData("\rDANGEROUS", "\"'\rDANGEROUS\"")]
    [InlineData("Normal Metin", "Normal Metin")]
    [InlineData("Metin, Virgüllü", "\"Metin, Virgüllü\"")]
    [InlineData("Metin \"Tırnaklı\"", "\"Metin \"\"Tırnaklı\"\"\"")]
    [InlineData("=Virgüllü, Formül", "\"'=Virgüllü, Formül\"")]
    public void SanitizeCsvCellNeutralizesFormulaInjectionAndEscapes(string input, string expected)
    {
        var result = SecureExportService.SanitizeCsvCell(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExportCsvAsyncThrowsWhenRowLimitExceeded()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var audit = new InMemoryAuditPublisher();
        var service = new SecureExportService(dbContext, audit);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        // Add MaxExportRows + 1 items
        for (int i = 0; i <= SecureExportService.MaxExportRows; i++)
        {
            dbContext.DailyOutpatientMetrics.Add(new DailyOutpatientMetric(
                date,
                Guid.NewGuid(),
                $"Bölüm {i}",
                null,
                null,
                now));
        }
        await dbContext.SaveChangesAsync();

        var request = new SecureExportRequestDto("outpatient-metrics");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExportCsvAsync(request, Guid.NewGuid(), Guid.NewGuid(), "ChiefMedicalOfficer", "test-corr"));

        Assert.Contains("Dışa aktarma sınırı aşıldı", ex.Message);
    }

    [Fact]
    public async Task ExportCsvAsyncSanitizesContentAndPublishesAuditLog()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var audit = new InMemoryAuditPublisher();
        var service = new SecureExportService(dbContext, audit);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        // Malicious injection attempt in department name and doctor name
        var metric = new DailyOutpatientMetric(
            date,
            Guid.NewGuid(),
            "=1+1",
            Guid.NewGuid(),
            "+cmd|' /C notepad'!A0",
            now);
        metric.ApplyTransition("None", "Scheduled", now);

        dbContext.DailyOutpatientMetrics.Add(metric);
        await dbContext.SaveChangesAsync();

        var request = new SecureExportRequestDto("outpatient-metrics");
        var userId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var result = await service.ExportCsvAsync(request, userId, personId, "ChiefMedicalOfficer", "test-correlation-123");

        Assert.NotNull(result);
        Assert.Equal(1, result.RowCount);
        Assert.StartsWith("poliklinik-raporu-", result.FileName);
        Assert.Equal("text/csv; charset=utf-8", result.ContentType);

        // Verify UTF-8 BOM preamble
        var bom = Encoding.UTF8.GetPreamble();
        Assert.Equal(bom[0], result.Content[0]);
        Assert.Equal(bom[1], result.Content[1]);
        Assert.Equal(bom[2], result.Content[2]);

        var csvText = Encoding.UTF8.GetString(result.Content);
        // Ensure malicious formulas are sanitized with leading single quote
        Assert.Contains("'=1+1", csvText);
        Assert.Contains("'+cmd|' /C notepad'!A0", csvText);

        // Verify Audit publication
        Assert.Single(audit.PublishedEvents);
        var publishedAudit = audit.PublishedEvents[0];
        Assert.Equal("report.operations.export", publishedAudit.Action);
        Assert.Equal("outpatient-metrics", publishedAudit.TargetResourceId);
        Assert.Equal(userId, publishedAudit.ActorUserId);
        Assert.Equal("test-correlation-123", publishedAudit.CorrelationId);
    }
}
