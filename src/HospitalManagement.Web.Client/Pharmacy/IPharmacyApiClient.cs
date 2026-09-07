using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Pharmacy;

namespace HospitalManagement.Web.Client.Pharmacy;

public interface IPharmacyApiClient
{
    Task<EncounterDetailResponse?> GetEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationCatalogItemResponse>?> SearchMedicationsAsync(
        string? query,
        string? route = null,
        string? form = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<MedicationCatalogItemResponse?> GetMedicationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> CreatePrescriptionDraftAsync(
        CreatePrescriptionDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> UpdatePrescriptionDraftAsync(
        Guid id,
        UpdatePrescriptionDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> SignPrescriptionAsync(
        Guid id,
        SignPrescriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> CancelPrescriptionAsync(
        Guid id,
        CancelPrescriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> GetPrescriptionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionsByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionsByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<MedicationSafetyCheckResponse?> CheckMedicationSafetyAsync(
        MedicationSafetyCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionWorklistAsync(
        string? status = null,
        string? prescriptionNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<PrescriptionDetailResponse?> DispensePrescriptionAsync(
        Guid id,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FefoCandidateStockResponse>?> GetFefoCandidatesAsync(
        Guid medicationCatalogItemId,
        CancellationToken cancellationToken = default);
}
