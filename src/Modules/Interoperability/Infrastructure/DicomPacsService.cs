using System.Globalization;
using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Domain.Dicom;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class DicomPacsService : IDicomPacsService
{
    private readonly IIntegrationMockEngine _mockEngine;
    private readonly TimeProvider _timeProvider;

    public DicomPacsService(
        IIntegrationMockEngine mockEngine,
        TimeProvider timeProvider)
    {
        _mockEngine = mockEngine ?? throw new ArgumentNullException(nameof(mockEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<List<DicomWorklistItem>> QueryModalityWorklistAsync(
        string? modality = null,
        string? scheduledDate = null,
        string? aeTitle = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.DicomPacs,
            "C-FIND_MWL_Query",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var todayStr = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

                var items = new List<DicomWorklistItem>
                {
                    new(
                        AccessionNumber: "ACC-2026-00101",
                        PatientId: "DEMO-PAT-1001",
                        PatientName: "YILMAZ^AHMET",
                        Modality: "CT",
                        ScheduledStationAeTitle: "CT_SCANNER_01",
                        ScheduledDate: todayStr,
                        ScheduledTime: "093000",
                        ScheduledProcedureStepDescription: "Toraks BT Kontrastsız",
                        RequestedProcedureId: "PROC-CT-01",
                        StudyInstanceUid: "1.2.840.10008.5.1.4.1.1.2.DEMO.20260901.00101",
                        Status: DicomWorklistStatus.Scheduled),
                    new(
                        AccessionNumber: "ACC-2026-00102",
                        PatientId: "DEMO-PAT-1002",
                        PatientName: "KAYA^FATMA",
                        Modality: "MR",
                        ScheduledStationAeTitle: "MR_SCANNER_01",
                        ScheduledDate: todayStr,
                        ScheduledTime: "101500",
                        ScheduledProcedureStepDescription: "Beyin MRG Difüzyon",
                        RequestedProcedureId: "PROC-MR-02",
                        StudyInstanceUid: "1.2.840.10008.5.1.4.1.1.4.DEMO.20260901.00102",
                        Status: DicomWorklistStatus.Scheduled),
                    new(
                        AccessionNumber: "ACC-2026-00103",
                        PatientId: "DEMO-PAT-1003",
                        PatientName: "DEMIR^MEHMET",
                        Modality: "XR",
                        ScheduledStationAeTitle: "XR_ROOM_02",
                        ScheduledDate: todayStr,
                        ScheduledTime: "110000",
                        ScheduledProcedureStepDescription: "Akciğer Grafisi PA",
                        RequestedProcedureId: "PROC-XR-03",
                        StudyInstanceUid: "1.2.840.10008.5.1.4.1.1.1.DEMO.20260901.00103",
                        Status: DicomWorklistStatus.InProgress)
                };

                if (!string.IsNullOrWhiteSpace(modality))
                {
                    items = items.Where(i => string.Equals(i.Modality, modality, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(aeTitle))
                {
                    items = items.Where(i => string.Equals(i.ScheduledStationAeTitle, aeTitle, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Task.FromResult(items);
            },
            payloadSummary: $"{{\"modality\": \"{modality ?? "*"}\", \"aeTitle\": \"{aeTitle ?? "*"}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "DICOM Modality Worklist sorgusu başarısız oldu.");
        }

        return result.Value ?? [];
    }

    public async Task<DicomWorklistItem> CreateWorklistOrderAsync(
        CreateDicomWorklistOrderDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.DicomPacs,
            "Create_MWL_Order",
            () =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var dateStr = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                var timeStr = now.ToString("HHmmss", CultureInfo.InvariantCulture);
                var shortId = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                var accNo = $"ACC-2026-{shortId}";
                var uid = $"1.2.840.10008.5.1.4.1.1.2.DEMO.{dateStr}.{shortId}";
                var stationAe = request.AeTitle ?? $"{request.Modality.ToUpperInvariant()}_SCANNER_01";

                var item = new DicomWorklistItem(
                    AccessionNumber: accNo,
                    PatientId: $"DEMO-PAT-{request.PatientId.ToString()[..8]}",
                    PatientName: "DEMO^PATIENT",
                    Modality: request.Modality.ToUpperInvariant(),
                    ScheduledStationAeTitle: stationAe,
                    ScheduledDate: dateStr,
                    ScheduledTime: timeStr,
                    ScheduledProcedureStepDescription: request.ProcedureDescription,
                    RequestedProcedureId: $"PROC-{request.Modality.ToUpperInvariant()}-{shortId}",
                    StudyInstanceUid: uid,
                    Status: DicomWorklistStatus.Scheduled);

                return Task.FromResult(item);
            },
            payloadSummary: $"{{\"patientId\": \"{request.PatientId}\", \"modality\": \"{request.Modality}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "DICOM MWL randevusu oluşturulamadı.");
        }

        return result.Value ?? throw new InvalidOperationException("İşlem tamamlanamadı.");
    }

    public async Task<DicomStudyMetadata?> QueryStudyMetadataAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studyInstanceUid);

        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.DicomPacs,
            "C-FIND_Study_Metadata",
            () =>
            {
                var instances = new List<DicomInstanceMetadata>
                {
                    new(
                        SopInstanceUid: $"{studyInstanceUid}.1.1",
                        InstanceNumber: 1,
                        SopClassUid: "1.2.840.10008.5.1.4.1.1.2",
                        Rows: 512,
                        Columns: 512,
                        BitsAllocated: 16,
                        SyntheticImageUrl: "/images/dicom/synthetic_slice_1.png"),
                    new(
                        SopInstanceUid: $"{studyInstanceUid}.1.2",
                        InstanceNumber: 2,
                        SopClassUid: "1.2.840.10008.5.1.4.1.1.2",
                        Rows: 512,
                        Columns: 512,
                        BitsAllocated: 16,
                        SyntheticImageUrl: "/images/dicom/synthetic_slice_2.png")
                };

                var series = new List<DicomSeriesMetadata>
                {
                    new(
                        SeriesInstanceUid: $"{studyInstanceUid}.1",
                        SeriesNumber: 1,
                        Modality: "CT",
                        SeriesDescription: "Axial 5mm Standard",
                        NumberOfInstances: 2,
                        Instances: instances)
                };

                var study = new DicomStudyMetadata(
                    StudyInstanceUid: studyInstanceUid,
                    AccessionNumber: "ACC-2026-00101",
                    PatientId: "DEMO-PAT-1001",
                    PatientName: "YILMAZ^AHMET",
                    StudyDate: "20260901",
                    StudyDescription: "Toraks BT Kontrastsız",
                    Modality: "CT",
                    NumberOfSeries: 1,
                    NumberOfInstances: 2,
                    Series: series);

                return Task.FromResult<DicomStudyMetadata?>(study);
            },
            payloadSummary: $"{{\"studyInstanceUid\": \"{studyInstanceUid}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "DICOM PACS çalışma sorgusu başarısız oldu.");
        }

        return result.Value;
    }

    public async Task<List<DicomStudyMetadata>> QueryPatientStudiesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mockEngine.ExecuteAsync(
            ExternalSystemType.DicomPacs,
            "C-FIND_Patient_Studies",
            async () =>
            {
                var studyUid = $"1.2.840.10008.5.1.4.1.1.2.DEMO.{patientId.ToString()[..8]}";
                var study = await QueryStudyMetadataAsync(studyUid, cancellationToken);
                return study is not null ? [study] : new List<DicomStudyMetadata>();
            },
            payloadSummary: $"{{\"patientId\": \"{patientId}\"}}",
            null,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Hasta PACS çalışmaları sorgulanamadı.");
        }

        return result.Value ?? [];
    }
}
