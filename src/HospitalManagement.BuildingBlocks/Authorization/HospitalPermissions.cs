namespace HospitalManagement.BuildingBlocks.Authorization;

public static class HospitalPermissions
{
    public static class Identity
    {
        public const string ProfileViewOwn = "identity.profile.view-own";
        public const string ProfileEditOwn = "identity.profile.edit-own";
        public const string UserInviteStaff = "identity.user.invite-staff";
        public const string UserDisable = "identity.user.disable";
        public const string RoleAssign = "identity.role.assign";
        public const string PermissionAssign = "identity.permission.assign";
    }

    public static class Organization
    {
        public const string View = "organization.view";
        public const string Manage = "organization.manage";
    }

    public static class Patient
    {
        public const string ViewOwn = "patient.view-own";
        public const string Search = "patient.search";
        public const string DemographicsView = "patient.demographics.view";
        public const string DemographicsCreate = "patient.demographics.create";
        public const string DemographicsEdit = "patient.demographics.edit";
    }

    public static class Appointment
    {
        public const string ViewOwn = "appointment.view-own";
        public const string BookOwn = "appointment.book-own";
        public const string ManageOwn = "appointment.manage-own";
        public const string Manage = "appointment.manage";
        public const string ScheduleManage = "appointment.schedule.manage";
        public const string CheckIn = "appointment.check-in";
    }

    public static class ClinicalRecords
    {
        public const string EncounterView = "encounter.view";
        public const string EncounterStart = "encounter.start";
        public const string EncounterComplete = "encounter.complete";
        public const string ObservationRecordVital = "observation.record-vital";
        public const string ClinicalNoteEditDraft = "clinical-note.edit-draft";
        public const string ClinicalNoteSign = "clinical-note.sign";
        public const string ClinicalNoteCorrect = "clinical-note.correct";
        public const string ClinicalNoteReopen = "clinical-note.reopen";
        public const string DiagnosisRecord = "diagnosis.record";
        public const string ConsultationRequest = "consultation.request";
        public const string ConsultationRespond = "consultation.respond";
        public const string ClinicalAttachmentUpload = "clinical-attachment.upload";
    }

    public static class Pharmacy
    {
        public const string MedicationCatalogView = "medication-catalog.view";
        public const string MedicationCatalogManage = "medication-catalog.manage";
        public const string PrescriptionView = "prescription.view";
        public const string PrescriptionCreate = "prescription.create";
        public const string PrescriptionSign = "prescription.sign";
        public const string PrescriptionCancel = "prescription.cancel";
        public const string PrescriptionDispense = "prescription.dispense";
        public const string InventoryPharmacyView = "inventory.pharmacy.view";
        public const string InventoryPharmacyAdjust = "inventory.pharmacy.adjust";
    }

    public static class Diagnostics
    {
        public const string DiagnosticOrderCreate = "diagnostic-order.create";
        public const string LaboratoryWorklistView = "laboratory.worklist.view";
        public const string LaboratorySpecimenTransition = "laboratory.specimen.transition";
        public const string LaboratoryResultEditDraft = "laboratory.result.edit-draft";
        public const string LaboratoryResultFinalize = "laboratory.result.finalize";
        public const string RadiologyWorklistView = "radiology.worklist.view";
        public const string RadiologyStudyComplete = "radiology.study.complete";
        public const string RadiologyReportFinalize = "radiology.report.finalize";
        public const string BloodTransfusionRecord = "blood-bank.transfusion.record";
        public const string DiagnosticResultViewFinalOwn = "diagnostic-result.view-final-own";
    }

    public static class Inpatient
    {
        public const string AdmissionRequest = "admission.request";
        public const string AdmissionAccept = "admission.accept";
        public const string BedAssign = "bed.assign";
        public const string BedTransfer = "bed.transfer";
        public const string CarePlanManage = "care-plan.manage";
        public const string MedicationAdminister = "medication.administer";
        public const string DischargeComplete = "discharge.complete";
        public const string EmergencyTriageRecord = "emergency.triage.record";
        public const string SurgerySchedule = "surgery.schedule";
        public const string CriticalCareRecord = "critical-care.record";
    }

    public static class SpecialtyCare
    {
        public const string SpecialtyCareManage = "specialty-care.manage";
        public const string SpecialtyCareRecord = "specialty-care.record";
        public const string SpecialtyCareView = "specialty-care.view";
        public const string SpecialtyCareViewOwn = "specialty-care.view-own";
    }

    public static class Interoperability
    {
        public const string MockManage = "interoperability.mock.manage";
        public const string ClinicalExchange = "interoperability.clinical.exchange";
        public const string FhirExport = "interoperability.fhir.export";
    }

    public static class ReportingAndAudit
    {
        public const string ProjectionManage = "report.projection.manage";
        public const string ReportOperationsView = "report.operations.view";
        public const string ReportOperationsExport = "report.operations.export";
        public const string AuditTechnicalView = "audit.technical.view";
        public const string AuditClinicalAccessView = "audit.clinical-access.view";
        public const string AuditOwnAccessView = "audit.own-access.view";
    }
}
