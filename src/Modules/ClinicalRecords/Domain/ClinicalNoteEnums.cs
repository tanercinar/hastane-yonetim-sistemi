namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum ClinicalNoteType
{
    GeneralSoap = 1,
    ProgressNote = 2,
    Consultation = 3,
    DischargeSummary = 4,
    NurseNote = 5,
    Addendum = 6,
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "Clinical note signature status")]
public enum ClinicalNoteStatus
{
    Draft = 1,
    Signed = 2,
    Amended = 3,
    EnteredInError = 4,
}
