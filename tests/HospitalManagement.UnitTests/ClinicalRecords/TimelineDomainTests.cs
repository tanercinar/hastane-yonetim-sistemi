using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class TimelineDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G08")]
    public void PatientTimelineItemInitializesCorrectly()
    {
        var eventId = Guid.NewGuid();
        var encId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var item = new PatientTimelineItem(
            eventId,
            PatientTimelineEventType.Diagnosis,
            nowUtc,
            "Tanı: Akut Tonsillit",
            "ICD-10: J03.9",
            encId,
            "Final",
            "Normal",
            false);

        Assert.Equal(eventId, item.EventId);
        Assert.Equal(PatientTimelineEventType.Diagnosis, item.EventType);
        Assert.Equal(nowUtc, item.TimestampUtc);
        Assert.Equal("Tanı: Akut Tonsillit", item.Title);
        Assert.Equal("ICD-10: J03.9", item.Summary);
        Assert.Equal(encId, item.EncounterId);
        Assert.Equal("Final", item.Badge);
        Assert.Equal("Normal", item.Severity);
        Assert.False(item.IsEnteredInError);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G08")]
    public void PatientTimelinePagedDtoCalculatesPaginationCorrectly()
    {
        var patientId = Guid.NewGuid();
        var items = new List<PatientTimelineItemDto>
        {
            new(Guid.NewGuid(), PatientTimelineEventType.Encounter, DateTime.UtcNow, "Karşılaşma", "Özet", null, "Completed", "Normal", false),
            new(Guid.NewGuid(), PatientTimelineEventType.VitalSigns, DateTime.UtcNow.AddMinutes(-10), "Vital", "120/80", null, "Normal", "Normal", false),
        };

        var paged = new PatientTimelinePagedDto(
            patientId,
            PageNumber: 1,
            PageSize: 10,
            TotalCount: 2,
            TotalPages: 1,
            HasPreviousPage: false,
            HasNextPage: false,
            items);

        Assert.Equal(patientId, paged.PatientId);
        Assert.Equal(1, paged.PageNumber);
        Assert.Equal(10, paged.PageSize);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(1, paged.TotalPages);
        Assert.False(paged.HasPreviousPage);
        Assert.False(paged.HasNextPage);
        Assert.Equal(2, paged.Items.Count);
    }
}
