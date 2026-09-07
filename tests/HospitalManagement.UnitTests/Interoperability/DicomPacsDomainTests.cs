using HospitalManagement.Modules.Interoperability.Domain.Dicom;
using Xunit;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class DicomPacsDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G04")]
    public void DicomWorklistItemInitializesCorrectly()
    {
        var item = new DicomWorklistItem(
            AccessionNumber: "ACC-2026-9901",
            PatientId: "DEMO-PAT-8801",
            PatientName: "TEST^PATIENT",
            Modality: "CT",
            ScheduledStationAeTitle: "CT_SCANNER_01",
            ScheduledDate: "20260901",
            ScheduledTime: "140000",
            ScheduledProcedureStepDescription: "Beyin BT",
            RequestedProcedureId: "PROC-CT-99",
            StudyInstanceUid: "1.2.840.10008.5.1.4.1.1.2.DEMO.9901",
            Status: DicomWorklistStatus.Scheduled);

        Assert.Equal("ACC-2026-9901", item.AccessionNumber);
        Assert.Equal("DEMO-PAT-8801", item.PatientId);
        Assert.Equal("CT", item.Modality);
        Assert.Equal("CT_SCANNER_01", item.ScheduledStationAeTitle);
        Assert.Equal(DicomWorklistStatus.Scheduled, item.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G04")]
    public void DicomStudyMetadataHierarchyIsConstructedProperly()
    {
        var instances = new List<DicomInstanceMetadata>
        {
            new("1.2.840.10008.5.1.4.1.1.2.1.1", 1, "1.2.840.10008.5.1.4.1.1.2", 512, 512, 16, "/preview/1.png"),
            new("1.2.840.10008.5.1.4.1.1.2.1.2", 2, "1.2.840.10008.5.1.4.1.1.2", 512, 512, 16, "/preview/2.png"),
        };

        var series = new List<DicomSeriesMetadata>
        {
            new("1.2.840.10008.5.1.4.1.1.2.1", 1, "CT", "Axial 5mm", 2, instances)
        };

        var study = new DicomStudyMetadata(
            StudyInstanceUid: "1.2.840.10008.5.1.4.1.1.2.STUDY.1",
            AccessionNumber: "ACC-01",
            PatientId: "PAT-01",
            PatientName: "DEMO^PAT",
            StudyDate: "20260901",
            StudyDescription: "Toraks BT",
            Modality: "CT",
            NumberOfSeries: 1,
            NumberOfInstances: 2,
            Series: series);

        Assert.Equal("1.2.840.10008.5.1.4.1.1.2.STUDY.1", study.StudyInstanceUid);
        Assert.Single(study.Series);
        Assert.Equal(2, study.Series[0].Instances.Count);
        Assert.Equal(512, study.Series[0].Instances[0].Rows);
        Assert.Equal(16, study.Series[0].Instances[0].BitsAllocated);
    }
}
