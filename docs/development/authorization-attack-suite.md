# Yetkilendirme Saldırı Paketi ve Kötüye Kullanım Doğrulaması (F13-G02)

## 1. Amaç ve Kapsam

Bu belge, Faz 13 kapsamında hastane yönetim sisteminin yetkilendirme katmanını (`Permission + Resource Scope + Care Relationship`) kötü niyetli veya hatalı istemci isteklerine, yatay/dikey yetki yükseltme girişimlerine, IDOR (Insecure Direct Object Reference) saldırılarına ve veri sızıntılarına karşı test eden otomatik güvenlik test paketini belgeler.

## 2. Test Edilen Saldırı Vektörleri

Otomatik test paketi `tests/HospitalManagement.UnitTests/Security/AuthorizationAttackSuiteTests.cs` içerisinde uygulanmış olup şu vektörleri kapsar:

### 2.1. IDOR (Insecure Direct Object Reference)
- **Hasta - Hasta İzolasyonu**: Hasta A kullanıcısının, `PersonId` eşleşmesi bulunmayan Hasta B kaynağına (randevu, profil, reçete) erişim denemesi engellenir (`IdorAttackWhenPatientAccessesAnotherPatientsResourceIsDenied`).
- **Bakım İlişkisi Bulunmayan Hekim**: Atanmamış veya aktif bakım ilişkisi (`CareRelationship`) tanımlanmamış bir hekimin klinik karşılaşma veya notlara erişimi reddedilir (`IdorAttackWhenClinicianWithoutCareRelationshipAccessesPatientIsDenied`).

### 2.2. Dikey ve Yatay Yetki Yükseltme (Privilege Escalation)
- **Hasta Rolünün İdari/Klinik İzin Talebi**: `Patient` rolünün klinik not oluşturma (`clinical.encounters.create`), reçete yazma (`pharmacy.prescriptions.create`), rol/izin yönetimi (`identity.roles.manage`, `identity.users.lockout`) veya laboratuvar onaylama izinlerine sahip olamayacağı garantilenir (`PrivilegeEscalationPatientRoleAttemptingClinicalOrAdminPermissionsIsDenied`).
- **Hemşire Rolünün Reçete İmzalama Talebi**: `Nurse` rolünün reçete imzalama (`pharmacy.prescriptions.sign`) yetkisi alması engellenir; bu izin yalnızca hekim ve başhekim rollerine tahsis edilir (`PrivilegeEscalationNurseRoleAttemptingPrescriptionSigningIsDenied`).
- **Departman ve Şube İzolasyonu**: Farklı bir departmanda (ör. Kardiyoloji) görevli personelin, Nöroloji departmanı kaynaklarına erişimi departman kapsam denetleyicisi tarafından engellenir (`DepartmentBoundaryDoctorFromDifferentDepartmentWithoutCareRelationshipIsDenied`).

### 2.3. Kitle Atama (Mass Assignment) ve Veri İzolasyonu
- **Model Binding Koruması**: Hasta kayıt ve profil güncelleme DTO'larında (`PatientRegistrationRequest`), rol (`Role`, `Roles`), idari bayraklar (`IsAdmin`, `IsSuperUser`) veya izin dizisi (`Permissions`) gibi yetki yükseltici alanların HTTP gövdesinden doğrudan bind edilemeyeceği garanti edilir (`MassAssignmentPatientRegistrationRequestDoesNotExposeAdministrativeProperties`).
- **Hassas Alan Sızıntısı Engeli**: Dışa açılan DTO yanıt modellerinde (`PatientDetailResponse`), şifre hash'leri (`Password`, `Salt`), güvenlik damgası (`SecurityStamp`) ve iki aşamalı doğrulama sırları (`TwoFactorSecret`, `RecoveryCode`) yer almaz (`SensitiveFieldLeakagePatientDtoResponsesDoNotExposeHashedPasswordsOrMfaSecrets`).

### 2.4. SignalR Grup Atlama (Group Hopping)
- **Canlı Bildirim ve Hub Güvenliği**: Kullanıcının kendi departman/şube yetkisi dışındaki SignalR gruplarına (ör. Nöroloji acil bildirim grubu) yetkisiz claim ile katılması (`Group Hopping`) engellenir (`SignalRGroupHoppingUnauthorizedGroupJoinBlocksMismatchedDepartmentClaim`).

### 2.5. Denetim İzi ve Veri Minimizasyonu
- **Yetki Reddi Denetim Kayıtları**: Yetki reddi durumlarında (`Outcome: Forbidden`) loglanan denetim olaylarında teşhis, ilaç dozu, klinik tanı veya laboratuvar verisi gibi PHI içeren hiçbir klinik içerik kaydedilmez; yalnızca eylem, aktör kimliği, kaynak tipi ve anonim hata sebebi saklanır (`AuthorizationDenyGeneratesAuditEventWithoutExposingClinicalData`).

## 3. Doğrulama Çıktısı

- `AuthorizationAttackSuiteTests` içindeki 10 senaryonun tamamı yeşildir.
- Toplam 439 birim testi sıfır hata ve sıfır uyarı ile geçmiştir.
- `dotnet format --verify-no-changes` uyumludur.
