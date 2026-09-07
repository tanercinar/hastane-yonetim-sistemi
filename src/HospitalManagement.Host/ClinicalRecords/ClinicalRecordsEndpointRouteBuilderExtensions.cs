using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.Notifications.Application;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.ClinicalRecords;

public static class ClinicalRecordsEndpointRouteBuilderExtensions
{
    public static WebApplication MapClinicalRecordsEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Encounter Endpoints
        var encounterGroup = app.MapGroup("/api/v1/clinical-records/encounters")
            .WithTags("ClinicalRecords - Encounters");

        encounterGroup.MapPost(string.Empty, CreateEncounterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterStart)
            .WithName("CreateEncounter")
            .Produces<EncounterDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapGet("/{id:guid}", GetEncounterByIdAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterById")
            .Produces<EncounterDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        encounterGroup.MapPost("/{id:guid}/start", StartEncounterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterStart)
            .WithName("StartEncounter")
            .Produces<EncounterDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapPost("/{id:guid}/complete", CompleteEncounterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterComplete)
            .WithName("CompleteEncounter")
            .Produces<EncounterDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapPost("/{id:guid}/reopen", ReopenEncounterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteReopen)
            .WithName("ReopenEncounter")
            .Produces<EncounterDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        encounterGroup.MapPost("/{id:guid}/cancel", CancelEncounterAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterStart)
            .WithName("CancelEncounter")
            .Produces<EncounterDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapPost("/{id:guid}/entered-in-error", MarkEncounterEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect)
            .WithName("MarkEncounterEnteredInError")
            .Produces<EncounterDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapPost("/{id:guid}/participants", AddParticipantAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.EncounterStart)
            .WithName("AddEncounterParticipant")
            .Produces<EncounterDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        encounterGroup.MapGet("/by-patient/{patientId:guid}", GetPatientEncountersAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientEncounters")
            .Produces<IReadOnlyList<EncounterSummaryResponse>>();

        encounterGroup.MapGet("/by-appointment/{appointmentId:guid}", GetActiveEncounterByAppointmentIdAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetActiveEncounterByAppointmentId")
            .Produces<EncounterDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Allergy Endpoints
        var allergyGroup = app.MapGroup("/api/v1/clinical-records/allergies")
            .WithTags("ClinicalRecords - Allergies");

        allergyGroup.MapPost(string.Empty, CreateAllergyAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("CreateAllergy")
            .Produces<AllergyResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        allergyGroup.MapPatch("/{id:guid}/status", UpdateAllergyStatusAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("UpdateAllergyStatus")
            .Produces<AllergyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        allergyGroup.MapPost("/{id:guid}/entered-in-error", MarkAllergyEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("MarkAllergyEnteredInError")
            .Produces<AllergyResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        allergyGroup.MapGet("/by-patient/{patientId:guid}", GetPatientAllergiesAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientAllergies")
            .Produces<IReadOnlyList<AllergyResponse>>();

        // Problem Endpoints
        var problemGroup = app.MapGroup("/api/v1/clinical-records/problems")
            .WithTags("ClinicalRecords - Problems");

        problemGroup.MapPost(string.Empty, CreateProblemAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("CreateClinicalProblem")
            .Produces<ClinicalProblemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        problemGroup.MapPatch("/{id:guid}/status", UpdateProblemStatusAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("UpdateClinicalProblemStatus")
            .Produces<ClinicalProblemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        problemGroup.MapPost("/{id:guid}/entered-in-error", MarkProblemEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("MarkClinicalProblemEnteredInError")
            .Produces<ClinicalProblemResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        problemGroup.MapGet("/by-patient/{patientId:guid}", GetPatientProblemsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientProblems")
            .Produces<IReadOnlyList<ClinicalProblemResponse>>();

        // Vital Signs Endpoints
        var vitalGroup = app.MapGroup("/api/v1/clinical-records/vital-signs")
            .WithTags("ClinicalRecords - VitalSigns");

        vitalGroup.MapPost(string.Empty, RecordVitalSignObservationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("RecordVitalSignObservation")
            .Produces<VitalSignObservationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        vitalGroup.MapPost("/panel", RecordVitalSignsPanelAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("RecordVitalSignsPanel")
            .Produces<VitalSignsPanelResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        vitalGroup.MapPost("/{id:guid}/entered-in-error", MarkVitalSignEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ObservationRecordVital)
            .WithName("MarkVitalSignEnteredInError")
            .Produces<VitalSignObservationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        vitalGroup.MapGet("/by-patient/{patientId:guid}", GetPatientVitalSignsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientVitalSigns")
            .Produces<IReadOnlyList<VitalSignObservationResponse>>();

        vitalGroup.MapGet("/by-encounter/{encounterId:guid}", GetEncounterVitalSignsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterVitalSigns")
            .Produces<IReadOnlyList<VitalSignObservationResponse>>();

        // Clinical Notes Endpoints
        var noteGroup = app.MapGroup("/api/v1/clinical-records/notes")
            .WithTags("ClinicalRecords - Notes");

        noteGroup.MapPost(string.Empty, CreateDraftNoteAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteEditDraft)
            .WithName("CreateDraftNote")
            .Produces<ClinicalNoteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapPut("/{id:guid}", UpdateDraftNoteAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteEditDraft)
            .WithName("UpdateDraftNote")
            .Produces<ClinicalNoteResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapPost("/{id:guid}/sign", SignNoteAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteSign)
            .WithName("SignClinicalNote")
            .Produces<ClinicalNoteResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapPost("/{id:guid}/addendum", AddNoteAddendumAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect)
            .WithName("AddClinicalNoteAddendum")
            .Produces<ClinicalNoteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapPost("/{id:guid}/entered-in-error", MarkNoteEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect)
            .WithName("MarkClinicalNoteEnteredInError")
            .Produces<ClinicalNoteResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapGet("/{id:guid}", GetNoteByIdAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetClinicalNoteById")
            .Produces<ClinicalNoteResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        noteGroup.MapGet("/by-encounter/{encounterId:guid}", GetEncounterNotesAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterClinicalNotes")
            .Produces<IReadOnlyList<ClinicalNoteResponse>>();

        noteGroup.MapGet("/by-patient/{patientId:guid}", GetPatientNotesAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientClinicalNotes")
            .Produces<IReadOnlyList<ClinicalNoteResponse>>();

        // Diagnosis Endpoints
        var diagnosisGroup = app.MapGroup("/api/v1/clinical-records")
            .WithTags("ClinicalRecords - Diagnoses");

        diagnosisGroup.MapGet("/diagnosis-catalog", SearchDiagnosisCatalogAsync)
            .RequirePermission(HospitalPermissions.ClinicalRecords.DiagnosisRecord)
            .WithName("SearchDiagnosisCatalog")
            .Produces<IReadOnlyList<DiagnosisCatalogItemResponse>>();

        diagnosisGroup.MapPost("/diagnoses", RecordDiagnosisAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.DiagnosisRecord)
            .WithName("RecordDiagnosis")
            .Produces<EncounterDiagnosisResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        diagnosisGroup.MapPut("/diagnoses/{id:guid}", UpdateDiagnosisAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.DiagnosisRecord)
            .WithName("UpdateDiagnosis")
            .Produces<EncounterDiagnosisResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        diagnosisGroup.MapPost("/diagnoses/{id:guid}/entered-in-error", MarkDiagnosisEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.DiagnosisRecord)
            .WithName("MarkDiagnosisEnteredInError")
            .Produces<EncounterDiagnosisResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        diagnosisGroup.MapGet("/diagnoses/by-encounter/{encounterId:guid}", GetEncounterDiagnosesAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterDiagnoses")
            .Produces<IReadOnlyList<EncounterDiagnosisResponse>>();

        diagnosisGroup.MapGet("/diagnoses/by-patient/{patientId:guid}", GetPatientDiagnosesAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientDiagnoses")
            .Produces<IReadOnlyList<EncounterDiagnosisResponse>>();

        // Consultation Endpoints
        var consultationGroup = app.MapGroup("/api/v1/clinical-records/consultations")
            .WithTags("ClinicalRecords - Consultations");

        consultationGroup.MapPost(string.Empty, RequestConsultationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRequest)
            .WithName("RequestConsultation")
            .Produces<ConsultationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapPost("/{id:guid}/accept", AcceptConsultationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRespond)
            .WithName("AcceptConsultation")
            .Produces<ConsultationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapPost("/{id:guid}/complete", CompleteConsultationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRespond)
            .WithName("CompleteConsultation")
            .Produces<ConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapPost("/{id:guid}/decline", DeclineConsultationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRespond)
            .WithName("DeclineConsultation")
            .Produces<ConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapPost("/{id:guid}/cancel", CancelConsultationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRequest)
            .WithName("CancelConsultation")
            .Produces<ConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapPost("/{id:guid}/entered-in-error", MarkConsultationEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRequest)
            .WithName("MarkConsultationEnteredInError")
            .Produces<ConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapGet("/{id:guid}", GetConsultationByIdAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.ClinicalRecords.ConsultationRespond,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetConsultationById")
            .Produces<ConsultationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        consultationGroup.MapGet("/by-encounter/{encounterId:guid}", GetEncounterConsultationsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.ClinicalRecords.ConsultationRespond,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterConsultations")
            .Produces<IReadOnlyList<ConsultationResponse>>();

        consultationGroup.MapGet("/by-department/{departmentId:guid}/pending", GetDepartmentPendingConsultationsAsync)
            .RequirePermission(HospitalPermissions.ClinicalRecords.ConsultationRespond)
            .WithName("GetDepartmentPendingConsultations")
            .Produces<IReadOnlyList<ConsultationResponse>>();

        consultationGroup.MapGet("/by-patient/{patientId:guid}", GetPatientConsultationsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.ClinicalRecords.ConsultationRespond,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientConsultations")
            .Produces<IReadOnlyList<ConsultationResponse>>();

        // Attachment Endpoints
        var attachmentGroup = app.MapGroup("/api/v1/clinical-records/attachments")
            .WithTags("ClinicalRecords - Attachments");

        attachmentGroup.MapPost(string.Empty, UploadAttachmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalAttachmentUpload)
            .DisableAntiforgery()
            .WithName("UploadClinicalAttachment")
            .Produces<ClinicalAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        attachmentGroup.MapGet("/{id:guid}", GetAttachmentMetadataAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetAttachmentMetadata")
            .Produces<ClinicalAttachmentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        attachmentGroup.MapGet("/{id:guid}/download", DownloadAttachmentAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("DownloadClinicalAttachment")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        attachmentGroup.MapPost("/{id:guid}/entered-in-error", MarkAttachmentEnteredInErrorAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect)
            .WithName("MarkClinicalAttachmentEnteredInError")
            .Produces<ClinicalAttachmentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        attachmentGroup.MapGet("/by-encounter/{encounterId:guid}", GetEncounterAttachmentsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetEncounterAttachments")
            .Produces<IReadOnlyList<ClinicalAttachmentResponse>>();

        attachmentGroup.MapGet("/by-patient/{patientId:guid}", GetPatientAttachmentsAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientAttachments")
            .Produces<IReadOnlyList<ClinicalAttachmentResponse>>();

        // Timeline Endpoints
        var timelineGroup = app.MapGroup("/api/v1/clinical-records/timeline")
            .WithTags("ClinicalRecords - Timeline");

        timelineGroup.MapGet("/by-patient/{patientId:guid}", GetPatientTimelineAsync)
            .RequireAnyPermission(
                HospitalPermissions.ClinicalRecords.EncounterView,
                HospitalPermissions.Patient.ViewOwn)
            .WithName("GetPatientTimeline")
            .Produces<PatientTimelinePagedResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> CreateEncounterAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateEncounterRequest request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<EncounterType>(request.EncounterType, true, out var encounterType))
        {
            encounterType = EncounterType.Outpatient;
        }

        var command = new CreateEncounterCommand(
            request.AppointmentId,
            request.PatientId,
            request.DepartmentId,
            request.PrimaryPractitionerId,
            encounterType,
            request.PlannedStartTimeUtc,
            request.ChiefComplaint,
            StartImmediately: false);

        var result = await encounterService.CreateEncounterAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/encounters/{dto.Id}", MapToDetailResponse(dto)));
    }

    private static async Task<IResult> GetEncounterByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var result = await encounterService.GetEncounterByIdAsync(actor, id, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> StartEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] StartEncounterRequest? request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var practitionerId = ExtractActorPersonId(actor);
        var command = new StartEncounterCommand(
            id,
            practitionerId,
            request?.ExpectedVersion ?? 0,
            request?.StartTimeUtc);
        var result = await encounterService.StartEncounterAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> CompleteEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CompleteEncounterRequest? request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var practitionerId = ExtractActorPersonId(actor);
        var command = new CompleteEncounterCommand(
            id,
            practitionerId,
            request?.ExpectedVersion ?? 0,
            request?.EndTimeUtc,
            request?.Summary);
        var result = await encounterService.CompleteEncounterAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> ReopenEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] ReopenEncounterRequest request,
        [FromServices] IEncounterService encounterService,
        [FromServices] IAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["reason"] = ["Karşılaşmayı yeniden açmak için geçerli bir klinik gerekçe girilmelidir."],
            });
        }

        var recentAuthentication = await authorizationService.AuthorizeAsync(
            actor,
            resource: null,
            [new RecentAuthenticationRequirement(TimeSpan.FromMinutes(5))]);
        if (!recentAuthentication.Succeeded
            || !actor.HasClaim(HospitalClaimTypes.Amr, "mfa"))
        {
            return Results.Forbid();
        }

        var practitionerId = ExtractActorPersonId(actor);
        var command = new ReopenEncounterCommand(
            id,
            practitionerId,
            request.ExpectedVersion,
            request.Reason);
        var result = await encounterService.ReopenEncounterAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> CancelEncounterAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelEncounterRequest? request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var practitionerId = ExtractActorPersonId(actor);
        var command = new CancelEncounterCommand(
            id,
            practitionerId,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? "Hizmet verilmedi");
        var result = await encounterService.CancelEncounterAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> MarkEncounterEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkEncounterEnteredInErrorRequest? request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var practitionerId = ExtractActorPersonId(actor);
        var command = new MarkEncounterEnteredInErrorCommand(
            id,
            practitionerId,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? string.Empty);
        var result = await encounterService.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> AddParticipantAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] AddEncounterParticipantRequest request,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<ParticipantRole>(request.Role, true, out var role))
        {
            role = ParticipantRole.Secondary;
        }

        var command = new AddEncounterParticipantCommand(
            id,
            request.PractitionerId,
            request.ExpectedVersion,
            role);
        var result = await encounterService.AddParticipantAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    private static async Task<IResult> GetPatientEncountersAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var result = await encounterService.GetPatientEncountersAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToSummaryResponse).ToList()));
    }

    private static async Task<IResult> GetActiveEncounterByAppointmentIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid appointmentId,
        [FromServices] IEncounterService encounterService,
        CancellationToken cancellationToken)
    {
        var result = await encounterService.GetActiveEncounterByAppointmentIdAsync(actor, appointmentId, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDetailResponse(dto)));
    }

    // Allergy Handlers
    private static async Task<IResult> CreateAllergyAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateAllergyRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<AllergyCategory>(request.Category, true, out var category))
        {
            category = AllergyCategory.Medication;
        }

        if (!Enum.TryParse<AllergyCriticality>(request.Criticality, true, out var criticality))
        {
            criticality = AllergyCriticality.Low;
        }

        var command = new CreateAllergyCommand(
            request.PatientId,
            request.EncounterId,
            request.Substance,
            category,
            criticality,
            request.Manifestation,
            request.OnsetDateTimeUtc,
            request.Notes);

        var result = await service.CreateAllergyAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/allergies/{dto.Id}", MapToAllergyResponse(dto)));
    }

    private static async Task<IResult> UpdateAllergyStatusAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateAllergyStatusRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<AllergyClinicalStatus>(request.ClinicalStatus, true, out var status))
        {
            status = AllergyClinicalStatus.Active;
        }

        var command = new UpdateAllergyStatusCommand(
            id,
            request.ExpectedVersion,
            status,
            request.Notes);
        var result = await service.UpdateAllergyStatusAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToAllergyResponse(dto)));
    }

    private static async Task<IResult> MarkAllergyEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkAllergyEnteredInErrorRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        var command = new MarkAllergyEnteredInErrorCommand(
            id,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? "Hatalı giriş");
        var result = await service.MarkAllergyEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToAllergyResponse(dto)));
    }

    private static async Task<IResult> GetPatientAllergiesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientAllergiesAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToAllergyResponse).ToList()));
    }

    // Problem Handlers
    private static async Task<IResult> CreateProblemAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateClinicalProblemRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<ProblemCategory>(request.Category, true, out var category))
        {
            category = ProblemCategory.ActiveProblem;
        }

        var command = new CreateClinicalProblemCommand(
            request.PatientId,
            request.EncounterId,
            request.ProblemTitle,
            request.Code,
            category,
            request.OnsetDate,
            request.Notes);

        var result = await service.CreateProblemAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/problems/{dto.Id}", MapToProblemResponse(dto)));
    }

    private static async Task<IResult> UpdateProblemStatusAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateClinicalProblemStatusRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<ProblemClinicalStatus>(request.ClinicalStatus, true, out var status))
        {
            status = ProblemClinicalStatus.Active;
        }

        var command = new UpdateClinicalProblemStatusCommand(
            id,
            request.ExpectedVersion,
            status,
            request.ResolvedDate,
            request.Notes);
        var result = await service.UpdateProblemStatusAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToProblemResponse(dto)));
    }

    private static async Task<IResult> MarkProblemEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkClinicalProblemEnteredInErrorRequest request,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        var command = new MarkClinicalProblemEnteredInErrorCommand(
            id,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? "Hatalı giriş");
        var result = await service.MarkProblemEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToProblemResponse(dto)));
    }

    private static async Task<IResult> GetPatientProblemsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IAllergyProblemService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientProblemsAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToProblemResponse).ToList()));
    }

    // Vital Signs Handlers
    private static async Task<IResult> RecordVitalSignObservationAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateVitalSignObservationRequest request,
        [FromServices] IVitalSignsService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<VitalSignType>(request.MeasurementType, true, out var measurementType))
        {
            measurementType = VitalSignType.HeartRate;
        }

        var command = new RecordVitalSignObservationCommand(
            request.PatientId,
            request.EncounterId,
            measurementType,
            request.Value,
            request.Unit,
            request.MeasurementMethod,
            request.MeasuredAtUtc,
            request.Notes);

        var result = await service.RecordObservationAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/vital-signs/{dto.Id}", MapToObservationResponse(dto)));
    }

    private static async Task<IResult> RecordVitalSignsPanelAsync(
        ClaimsPrincipal actor,
        [FromBody] RecordVitalSignsPanelRequest request,
        [FromServices] IVitalSignsService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new RecordVitalSignsPanelCommand(
            request.PatientId,
            request.EncounterId,
            request.TemperatureCelsius,
            request.SystolicBloodPressureMmHg,
            request.DiastolicBloodPressureMmHg,
            request.HeartRateBpm,
            request.RespiratoryRatePerMin,
            request.OxygenSaturationPercent,
            request.BodyWeightKg,
            request.BodyHeightCm,
            request.BloodGlucoseMgDl,
            request.PainScore,
            request.ConsciousnessState,
            request.Notes);

        var result = await service.RecordPanelAsync(actor, command, cancellationToken);
        return ToResult(result, panelDto => Results.Created(
            $"/api/v1/clinical-records/vital-signs/by-patient/{panelDto.PatientId}",
            new VitalSignsPanelResponse(
                panelDto.PatientId,
                panelDto.EncounterId,
                panelDto.Observations.Select(MapToObservationResponse).ToList(),
                panelDto.ConsciousnessState,
                panelDto.RecordedAtUtc)));
    }

    private static async Task<IResult> MarkVitalSignEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkVitalSignEnteredInErrorRequest request,
        [FromServices] IVitalSignsService service,
        CancellationToken cancellationToken)
    {
        var command = new MarkVitalSignEnteredInErrorCommand(
            id,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? "Hatalı giriş");
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToObservationResponse(dto)));
    }

    private static async Task<IResult> GetPatientVitalSignsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IVitalSignsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientObservationsAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToObservationResponse).ToList()));
    }

    private static async Task<IResult> GetEncounterVitalSignsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IVitalSignsService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEncounterObservationsAsync(actor, encounterId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToObservationResponse).ToList()));
    }

    // Clinical Notes Handlers
    private static async Task<IResult> CreateDraftNoteAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateClinicalNoteRequest request,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<ClinicalNoteType>(request.NoteType, true, out var noteType))
        {
            noteType = ClinicalNoteType.GeneralSoap;
        }

        var command = new CreateDraftNoteCommand(
            request.EncounterId,
            request.PatientId,
            noteType,
            request.Title,
            request.ChiefComplaint,
            request.HistoryOfPresentIllness,
            request.PhysicalExamination,
            request.Assessment,
            request.Plan,
            request.Content);

        var result = await service.CreateDraftNoteAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/notes/{dto.Id}", MapToNoteResponse(dto)));
    }

    private static async Task<IResult> UpdateDraftNoteAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateClinicalNoteDraftRequest request,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new UpdateDraftNoteCommand(
            id,
            request.ExpectedVersion,
            request.Title,
            request.ChiefComplaint,
            request.HistoryOfPresentIllness,
            request.PhysicalExamination,
            request.Assessment,
            request.Plan,
            request.Content);

        var result = await service.UpdateDraftNoteAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToNoteResponse(dto)));
    }

    private static async Task<IResult> SignNoteAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] SignClinicalNoteRequest? request,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new SignClinicalNoteCommand(id, request.ExpectedVersion, request.SignatureNote);
        var result = await service.SignNoteAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToNoteResponse(dto)));
    }

    private static async Task<IResult> AddNoteAddendumAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] AddClinicalNoteAddendumRequest request,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new AddClinicalNoteAddendumCommand(
            id,
            request.ExpectedVersion,
            request.AddendumContent,
            request.Reason);
        var result = await service.AddAddendumAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/notes/{dto.Id}", MapToNoteResponse(dto)));
    }

    private static async Task<IResult> MarkNoteEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkClinicalNoteEnteredInErrorRequest request,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new MarkClinicalNoteEnteredInErrorCommand(
            id,
            request.ExpectedVersion,
            request.Reason);
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToNoteResponse(dto)));
    }

    private static async Task<IResult> GetNoteByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetNoteByIdAsync(actor, id, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToNoteResponse(dto)));
    }

    private static async Task<IResult> GetEncounterNotesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEncounterNotesAsync(actor, encounterId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToNoteResponse).ToList()));
    }

    private static async Task<IResult> GetPatientNotesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IClinicalNoteService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientNotesAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToNoteResponse).ToList()));
    }

    // Diagnosis Handlers
    private static async Task<IResult> SearchDiagnosisCatalogAsync(
        [FromQuery] string? query,
        [FromQuery] int? maxResults,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        var results = await service.SearchDiagnosisCatalogAsync(query, maxResults ?? 50, cancellationToken);
        return Results.Ok(results.Select(c => new DiagnosisCatalogItemResponse(
            c.Id,
            c.Code,
            c.NameTurkish,
            c.NameEnglish,
            c.Chapter,
            c.Block,
            c.CatalogVersion)).ToList());
    }

    private static async Task<IResult> RecordDiagnosisAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateDiagnosisRequest request,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<DiagnosisType>(request.DiagnosisType, true, out var diagnosisType))
        {
            diagnosisType = DiagnosisType.Preliminary;
        }

        var command = new RecordDiagnosisCommand(
            request.EncounterId,
            request.PatientId,
            diagnosisType,
            request.IsCoded,
            request.Icd10Code,
            request.DiagnosisTitle,
            request.Notes);

        var result = await service.RecordDiagnosisAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/diagnoses/{dto.Id}", MapToDiagnosisResponse(dto)));
    }

    private static async Task<IResult> UpdateDiagnosisAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] UpdateDiagnosisRequest request,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<DiagnosisType>(request.DiagnosisType, true, out var diagnosisType))
        {
            diagnosisType = DiagnosisType.Preliminary;
        }

        var command = new UpdateDiagnosisCommand(
            id,
            request.ExpectedVersion,
            diagnosisType,
            request.IsCoded,
            request.Icd10Code,
            request.DiagnosisTitle,
            request.Notes);

        var result = await service.UpdateDiagnosisAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDiagnosisResponse(dto)));
    }

    private static async Task<IResult> MarkDiagnosisEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkDiagnosisEnteredInErrorRequest request,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new MarkDiagnosisEnteredInErrorCommand(
            id,
            request.ExpectedVersion,
            request.Reason);
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToDiagnosisResponse(dto)));
    }

    private static async Task<IResult> GetEncounterDiagnosesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEncounterDiagnosesAsync(actor, encounterId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToDiagnosisResponse).ToList()));
    }

    private static async Task<IResult> GetPatientDiagnosesAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IDiagnosisService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientDiagnosesAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToDiagnosisResponse).ToList()));
    }

    // Consultation Handlers
    private static async Task<IResult> RequestConsultationAsync(
        ClaimsPrincipal actor,
        [FromBody] CreateConsultationRequest request,
        [FromServices] IConsultationService service,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        if (!Enum.TryParse<ConsultationUrgency>(request.Urgency, true, out var urgency))
        {
            urgency = ConsultationUrgency.Routine;
        }

        var command = new RequestConsultationCommand(
            request.EncounterId,
            request.PatientId,
            request.TargetDepartmentId,
            request.TargetPractitionerId,
            urgency,
            request.ReasonForConsultation,
            request.ClinicalQuestion);

        var result = await service.RequestConsultationAsync(actor, command, cancellationToken);
        if (result.Succeeded && result.Value is not null && result.Value.TargetPractitionerId.HasValue)
        {
            await PublishConsultationStatusNotificationAsync(
                notificationService,
                result.Value,
                result.Value.TargetPractitionerId.Value,
                "Requested",
                "Yeni konsültasyon isteği",
                "Yeni bir konsültasyon isteği atandı.",
                cancellationToken);
        }

        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/consultations/{dto.Id}", MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> AcceptConsultationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] AcceptConsultationRequest? request,
        [FromServices] IConsultationService service,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new AcceptConsultationCommand(id, request.ExpectedVersion, request.Notes);
        var result = await service.AcceptConsultationAsync(actor, command, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            await PublishConsultationStatusNotificationAsync(
                notificationService,
                result.Value,
                result.Value.RequestingPractitionerId,
                "Accepted",
                "Konsültasyon kabul edildi",
                "İstediğiniz konsültasyon kabul edildi.",
                cancellationToken);
        }

        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> CompleteConsultationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CompleteConsultationRequest request,
        [FromServices] IConsultationService service,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new CompleteConsultationCommand(
            id,
            request.ExpectedVersion,
            request.ConsultationReport,
            request.Recommendation);
        var result = await service.CompleteConsultationAsync(actor, command, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            await PublishConsultationStatusNotificationAsync(
                notificationService,
                result.Value,
                result.Value.RequestingPractitionerId,
                "Completed",
                "Konsültasyon tamamlandı",
                "İstediğiniz konsültasyon tamamlandı.",
                cancellationToken);
        }

        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> DeclineConsultationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] DeclineConsultationRequest request,
        [FromServices] IConsultationService service,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new DeclineConsultationCommand(id, request.ExpectedVersion, request.Reason);
        var result = await service.DeclineConsultationAsync(actor, command, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            await PublishConsultationStatusNotificationAsync(
                notificationService,
                result.Value,
                result.Value.RequestingPractitionerId,
                "Declined",
                "Konsültasyon reddedildi",
                "İstediğiniz konsültasyon gerekçeli olarak reddedildi.",
                cancellationToken);
        }

        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> CancelConsultationAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] CancelConsultationRequest request,
        [FromServices] IConsultationService service,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new CancelConsultationCommand(id, request.ExpectedVersion, request.Reason);
        var result = await service.CancelConsultationAsync(actor, command, cancellationToken);
        var recipientPersonId = result.Value?.AssignedPractitionerId ?? result.Value?.TargetPractitionerId;
        if (result.Succeeded && result.Value is not null && recipientPersonId.HasValue)
        {
            await PublishConsultationStatusNotificationAsync(
                notificationService,
                result.Value,
                recipientPersonId.Value,
                "Cancelled",
                "Konsültasyon iptal edildi",
                "Size yönlendirilen konsültasyon isteği iptal edildi.",
                cancellationToken);
        }

        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> MarkConsultationEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkConsultationEnteredInErrorRequest request,
        [FromServices] IConsultationService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["İstek gövdesi boş olamaz."],
            });
        }

        var command = new MarkConsultationEnteredInErrorCommand(
            id,
            request.ExpectedVersion,
            request.Reason);
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> GetConsultationByIdAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IConsultationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetConsultationByIdAsync(actor, id, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToConsultationResponse(dto)));
    }

    private static async Task<IResult> GetEncounterConsultationsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IConsultationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEncounterConsultationsAsync(actor, encounterId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToConsultationResponse).ToList()));
    }

    private static async Task<IResult> GetDepartmentPendingConsultationsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid departmentId,
        [FromServices] IConsultationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDepartmentPendingConsultationsAsync(actor, departmentId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToConsultationResponse).ToList()));
    }

    private static async Task<IResult> GetPatientConsultationsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IConsultationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientConsultationsAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToConsultationResponse).ToList()));
    }

    // Attachment Handlers
    private static async Task<IResult> UploadAttachmentAsync(
        ClaimsPrincipal actor,
        HttpRequest httpRequest,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.HasFormContentType)
        {
            return Results.Problem(
                title: "Geçersiz İstek",
                detail: "Yükleme isteği 'multipart/form-data' biçiminde olmalıdır.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Yüklenecek dosya seçilmelidir."],
            });
        }

        if (file.Length > AttachmentSecurityValidator.MaxFileSizeBytes)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Dosya boyutu maksimum izin verilen 15 MB sınırını aşıyor."],
            });
        }

        if (!Guid.TryParse(form["encounterId"], out var encounterId) || encounterId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["encounterId"] = ["Geçerli bir karşılaşma kimliği zorunludur."],
            });
        }

        if (!Guid.TryParse(form["patientId"], out var patientId) || patientId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["patientId"] = ["Geçerli bir hasta kimliği zorunludur."],
            });
        }

        if (!Enum.TryParse<ClinicalAttachmentType>(form["attachmentType"], true, out var attachmentType))
        {
            attachmentType = ClinicalAttachmentType.LabReport;
        }

        var description = form["description"].ToString();

        var command = new UploadAttachmentCommand(
            encounterId,
            patientId,
            attachmentType,
            file.FileName,
            file.ContentType,
            description);

        await using var stream = file.OpenReadStream();
        var result = await service.UploadAttachmentAsync(actor, command, stream, cancellationToken);
        return ToResult(result, dto => Results.Created($"/api/v1/clinical-records/attachments/{dto.Id}", MapToAttachmentResponse(dto)));
    }

    private static async Task<IResult> GetAttachmentMetadataAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAttachmentMetadataAsync(actor, id, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToAttachmentResponse(dto)));
    }

    private static async Task<IResult> DownloadAttachmentAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DownloadAttachmentAsync(actor, id, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToResult(result, _ => Results.NotFound());
        }

        return Results.File(
            result.Value.Stream,
            result.Value.ContentType,
            result.Value.FileName);
    }

    private static async Task<IResult> MarkAttachmentEnteredInErrorAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid id,
        [FromBody] MarkAttachmentEnteredInErrorRequest request,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        var command = new MarkAttachmentEnteredInErrorCommand(
            id,
            request?.ExpectedVersion ?? 0,
            request?.Reason ?? "Hatalı yükleme");
        var result = await service.MarkEnteredInErrorAsync(actor, command, cancellationToken);
        return ToResult(result, dto => Results.Ok(MapToAttachmentResponse(dto)));
    }

    private static async Task<IResult> GetEncounterAttachmentsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid encounterId,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEncounterAttachmentsAsync(actor, encounterId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToAttachmentResponse).ToList()));
    }

    private static async Task<IResult> GetPatientAttachmentsAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromServices] IClinicalAttachmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientAttachmentsAsync(actor, patientId, cancellationToken);
        return ToResult(result, dtos => Results.Ok(dtos.Select(MapToAttachmentResponse).ToList()));
    }

    // Timeline Handlers
    private static async Task<IResult> GetPatientTimelineAsync(
        ClaimsPrincipal actor,
        [FromRoute] Guid patientId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool? includeEnteredInError,
        [FromServices] IPatientTimelineService service,
        CancellationToken cancellationToken)
    {
        var query = new GetPatientTimelineQuery(
            patientId,
            pageNumber ?? 1,
            pageSize ?? 20,
            EventTypes: null,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            IncludeEnteredInError: includeEnteredInError ?? false);

        var result = await service.GetPatientTimelineAsync(actor, query, cancellationToken);
        return ToResult(result, pagedDto => Results.Ok(new PatientTimelinePagedResponse(
            pagedDto.PatientId,
            pagedDto.PageNumber,
            pagedDto.PageSize,
            pagedDto.TotalCount,
            pagedDto.TotalPages,
            pagedDto.HasPreviousPage,
            pagedDto.HasNextPage,
            pagedDto.Items.Select(i => new PatientTimelineItemResponse(
                i.EventId,
                i.EventType.ToString(),
                i.TimestampUtc,
                i.Title,
                i.Summary,
                i.EncounterId,
                i.Badge,
                i.Severity,
                i.IsEnteredInError)).ToList())));
    }

    private static Guid ExtractActorPersonId(ClaimsPrincipal actor)
    {
        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(personIdStr, out var pid) ? pid : Guid.Empty;
    }

    private static async Task PublishConsultationStatusNotificationAsync(
        INotificationService notificationService,
        ConsultationDto consultation,
        Guid recipientPersonId,
        string status,
        string subject,
        string message,
        CancellationToken cancellationToken)
    {
        var command = new PublishOutboxEventCommand(
            EventType: $"Consultation.{status}",
            IdempotencyKey: $"consultation-{consultation.Id:N}-{status.ToLowerInvariant()}-v{consultation.Version}",
            RecipientPersonId: recipientPersonId,
            RecipientEmail: null,
            RecipientPhone: null,
            Subject: subject,
            Message: message);

        await notificationService.EnqueueOutboxEventAsync(command, cancellationToken);
        await notificationService.ProcessOutboxAsync(cancellationToken);
    }

    private static IResult ToResult<TIn>(
        ClinicalEncounterOperationResult<TIn> result,
        Func<TIn, IResult> successMapper)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return successMapper(result.Value);
        }

        return result.Status switch
        {
            ClinicalEncounterOperationStatus.NotFound => Results.Problem(
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage ?? "İstenen klinik kayıt bulunamadı.",
                statusCode: StatusCodes.Status404NotFound),

            ClinicalEncounterOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value),
                title: "Doğrulama Hatası",
                detail: result.ErrorMessage),

            ClinicalEncounterOperationStatus.Conflict => Results.Problem(
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage ?? "İşlem mevcut durum ile çakışmaktadır.",
                statusCode: StatusCodes.Status409Conflict),

            ClinicalEncounterOperationStatus.Forbidden => Results.Problem(
                title: "Erişim Reddedildi",
                detail: result.ErrorMessage ?? "Bu işlem için yetkiniz bulunmamaktadır.",
                statusCode: StatusCodes.Status403Forbidden),

            _ => Results.Problem(
                title: "Sunucu Hatası",
                detail: result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.",
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static EncounterDetailResponse MapToDetailResponse(EncounterDto dto)
    {
        return new EncounterDetailResponse(
            dto.Id,
            dto.AppointmentId,
            dto.PatientId,
            dto.DepartmentId,
            dto.PrimaryPractitionerId,
            dto.EncounterType.ToString(),
            dto.Status.ToString(),
            dto.PlannedStartTimeUtc,
            dto.ActualStartTimeUtc,
            dto.ActualEndTimeUtc,
            dto.ChiefComplaint,
            dto.CancellationReason,
            dto.EnteredInErrorReason,
            dto.ReopenReason,
            dto.ReopenedAtUtc,
            dto.ReopenedByPractitionerId,
            dto.Version,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.Participants.Select(p => new EncounterParticipantResponse(
                p.Id,
                p.EncounterId,
                p.PractitionerId,
                p.Role.ToString(),
                p.JoinedAtUtc,
                p.LeftAtUtc)).ToList());
    }

    private static EncounterSummaryResponse MapToSummaryResponse(EncounterDto dto)
    {
        return new EncounterSummaryResponse(
            dto.Id,
            dto.AppointmentId,
            dto.PatientId,
            dto.DepartmentId,
            dto.PrimaryPractitionerId,
            dto.EncounterType.ToString(),
            dto.Status.ToString(),
            dto.ActualStartTimeUtc ?? dto.PlannedStartTimeUtc,
            dto.ChiefComplaint,
            dto.CreatedAtUtc);
    }

    private static AllergyResponse MapToAllergyResponse(AllergyDto dto)
    {
        return new AllergyResponse(
            dto.Id,
            dto.PatientId,
            dto.EncounterId,
            dto.Substance,
            dto.Category.ToString(),
            dto.Criticality.ToString(),
            dto.ClinicalStatus.ToString(),
            dto.VerificationStatus.ToString(),
            dto.Manifestation,
            dto.OnsetDateTimeUtc,
            dto.RecordedByPractitionerId,
            dto.RecordedAtUtc,
            dto.UpdatedAtUtc,
            dto.EnteredInErrorReason,
            dto.Version);
    }

    private static ClinicalProblemResponse MapToProblemResponse(ClinicalProblemDto dto)
    {
        return new ClinicalProblemResponse(
            dto.Id,
            dto.PatientId,
            dto.EncounterId,
            dto.ProblemTitle,
            dto.Code,
            dto.Category.ToString(),
            dto.ClinicalStatus.ToString(),
            dto.VerificationStatus.ToString(),
            dto.OnsetDate,
            dto.ResolvedDate,
            dto.Notes,
            dto.RecordedByPractitionerId,
            dto.RecordedAtUtc,
            dto.UpdatedAtUtc,
            dto.EnteredInErrorReason,
            dto.Version);
    }

    private static VitalSignObservationResponse MapToObservationResponse(VitalSignObservationDto dto)
    {
        return new VitalSignObservationResponse(
            dto.Id,
            dto.PatientId,
            dto.EncounterId,
            dto.MeasurementType.ToString(),
            dto.Value,
            dto.Unit,
            dto.Interpretation.ToString(),
            dto.MeasurementMethod,
            dto.MeasuredAtUtc,
            dto.Notes,
            dto.RecordedByPractitionerId,
            dto.RecordedAtUtc,
            dto.UpdatedAtUtc,
            dto.IsEnteredInError,
            dto.EnteredInErrorReason,
            dto.Version);
    }

    private static ClinicalNoteResponse MapToNoteResponse(ClinicalNoteDto dto)
    {
        return new ClinicalNoteResponse(
            dto.Id,
            dto.EncounterId,
            dto.PatientId,
            dto.AuthorPractitionerId,
            dto.NoteType.ToString(),
            dto.Status.ToString(),
            dto.Title,
            dto.ChiefComplaint,
            dto.HistoryOfPresentIllness,
            dto.PhysicalExamination,
            dto.Assessment,
            dto.Plan,
            dto.Content,
            dto.SignedAtUtc,
            dto.SignedByPractitionerId,
            dto.ParentNoteId,
            dto.CorrectionReason,
            dto.EnteredInErrorReason,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.Version);
    }

    private static EncounterDiagnosisResponse MapToDiagnosisResponse(EncounterDiagnosisDto dto)
    {
        return new EncounterDiagnosisResponse(
            dto.Id,
            dto.EncounterId,
            dto.PatientId,
            dto.DiagnosedByPractitionerId,
            dto.DiagnosisType.ToString(),
            dto.IsCoded,
            dto.Icd10Code,
            dto.DiagnosisTitle,
            dto.CatalogVersion,
            dto.Notes,
            dto.DiagnosedAtUtc,
            dto.UpdatedAtUtc,
            dto.IsEnteredInError,
            dto.EnteredInErrorReason,
            dto.Version);
    }

    private static ConsultationResponse MapToConsultationResponse(ConsultationDto dto)
    {
        return new ConsultationResponse(
            dto.Id,
            dto.EncounterId,
            dto.PatientId,
            dto.RequestingPractitionerId,
            dto.TargetDepartmentId,
            dto.TargetPractitionerId,
            dto.AssignedPractitionerId,
            dto.Urgency.ToString(),
            dto.Status.ToString(),
            dto.ReasonForConsultation,
            dto.ClinicalQuestion,
            dto.ConsultationReport,
            dto.Recommendation,
            dto.DeclineReason,
            dto.CancellationReason,
            dto.EnteredInErrorReason,
            dto.RequestedAtUtc,
            dto.AcceptedAtUtc,
            dto.CompletedAtUtc,
            dto.UpdatedAtUtc,
            dto.Version);
    }

    private static ClinicalAttachmentResponse MapToAttachmentResponse(ClinicalAttachmentDto dto)
    {
        return new ClinicalAttachmentResponse(
            dto.Id,
            dto.EncounterId,
            dto.PatientId,
            dto.UploadedByPractitionerId,
            dto.AttachmentType.ToString(),
            dto.FileName,
            dto.ContentType,
            dto.ByteSize,
            dto.Sha256Checksum,
            dto.Description,
            dto.UploadedAtUtc,
            dto.IsEnteredInError,
            dto.EnteredInErrorReason,
            dto.Version);
    }
}
