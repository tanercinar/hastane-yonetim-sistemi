namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum PatientTimelineEventType
{
    Encounter = 1,
    VitalSigns = 2,
    ClinicalNote = 3,
    Diagnosis = 4,
    Consultation = 5,
    Attachment = 6,
    Allergy = 7,
    Problem = 8,
}
