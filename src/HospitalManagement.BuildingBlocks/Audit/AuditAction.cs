namespace HospitalManagement.BuildingBlocks.Audit;

public static class AuditAction
{
    public const string UserLogin = "Identity.Login";
    public const string UserLogout = "Identity.Logout";
    public const string TwoFactorVerify = "Identity.TwoFactorVerify";
    public const string PasswordResetRequest = "Identity.PasswordResetRequest";
    public const string PasswordReset = "Identity.PasswordReset";
    public const string StaffInvite = "Identity.StaffInvite";
    public const string StaffInviteAccept = "Identity.StaffInviteAccept";
    public const string MfaEnable = "Identity.MfaEnable";
    public const string MfaDisable = "Identity.MfaDisable";
    public const string UserDisable = "Identity.UserDisable";
    public const string RoleAssign = "Identity.RoleAssign";
    public const string PermissionAssign = "Identity.PermissionAssign";

    public const string PatientSearch = "Patient.Search";
    public const string PatientView = "Patient.View";
    public const string PatientRegister = "Patient.Register";
    public const string PatientDemographicsEdit = "Patient.DemographicsEdit";

    public const string EncounterView = "ClinicalRecords.EncounterView";
    public const string EncounterStart = "ClinicalRecords.EncounterStart";
    public const string EncounterComplete = "ClinicalRecords.EncounterComplete";
    public const string ClinicalNoteCreate = "ClinicalRecords.NoteCreate";
    public const string ClinicalNoteSign = "ClinicalRecords.NoteSign";
    public const string ClinicalNoteAmend = "ClinicalRecords.NoteAmend";
    public const string VitalRecord = "ClinicalRecords.VitalRecord";

    public const string PrescriptionCreate = "Pharmacy.PrescriptionCreate";
    public const string PrescriptionSign = "Pharmacy.PrescriptionSign";
    public const string PrescriptionDispense = "Pharmacy.PrescriptionDispense";
    public const string PrescriptionCancel = "Pharmacy.PrescriptionCancel";

    public const string DiagnosticOrderCreate = "Diagnostics.OrderCreate";
    public const string DiagnosticResultFinalize = "Diagnostics.ResultFinalize";

    public const string DataExport = "Privacy.DataExport";
    public const string AuditLogView = "Audit.LogView";
    public const string SecurityAccessDenied = "Security.AccessDenied";
}
