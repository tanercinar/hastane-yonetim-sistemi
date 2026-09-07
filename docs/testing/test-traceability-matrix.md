# Test İzlenebilirlik Matrisi (Test Traceability Matrix)

Bu belge, Hastane Yönetim Sistemi projesinin `ROADMAP.md` Bölüm 2.3'te tanımlanan 12 temel başarı ölçütünün ve tüm kullanıcı rollerinin otomatik test suite'leri (Unit, Architecture, Component, Integration, E2E) ile olan izlenebilirlik haritasını belgeler.

---

## 1. Temel Başarı Ölçütleri ve Kanıt Haritası

| No | Başarı Ölçütü | Test Seviyesi | Doğrulayan Test Sınıfları ve Kanıt Dosyaları |
| :---: | :--- | :--- | :--- |
| **SC-01** | Hasta hesap açar, uygun doktoru bulur ve randevu alır. | E2E, Integration, Component, Unit | `Phase3ProductGateEndToEndTests.cs`<br>`DoctorAvailabilityScheduleIntegrationTests.cs`<br>`AppointmentLifecycleIntegrationTests.cs`<br>`AppointmentComponentTests.cs`<br>`ScheduleDomainUnitTests.cs` |
| **SC-02** | Kayıt personeli hastayı doğrular ve randevuyu check-in durumuna getirir. | E2E, Integration, Component, Unit | `Phase3ProductGateEndToEndTests.cs`<br>`DailyAppointmentQueueIntegrationTests.cs`<br>`DailyAppointmentQueueComponentTests.cs`<br>`AppointmentDomainUnitTests.cs` |
| **SC-03** | Hemşire vital bulguları girer; doktor muayeneyi tamamlar, tanı ve reçete ekler. | E2E, Integration, Component, Unit | `Phase5ProductGateEndToEndTests.cs`<br>`ClinicalVitalSignsIntegrationTests.cs`<br>`ClinicalDiagnosisIntegrationTests.cs`<br>`ClinicalNotesIntegrationTests.cs`<br>`DoctorPrescriptionEditorComponentTests.cs`<br>`EmarIntegrationTests.cs` |
| **SC-04** | Eczacı yalnızca yetkisi dâhilindeki reçeteyi görür ve teslim kaydı oluşturur. | E2E, Integration, Component, Unit | `Phase5ProductGateEndToEndTests.cs`<br>`PharmacyWorklistIntegrationTests.cs`<br>`PharmacyWorklistDispenseComponentTests.cs`<br>`PharmacyDomainUnitTests.cs` |
| **SC-05** | Doktor laboratuvar/radyoloji istemi verir; ilgili personel iş listesini görür ve sonuç girer. | E2E, Integration, Component, Unit | `Phase6ProductGateEndToEndTests.cs`<br>`DiagnosticOrderIntegrationTests.cs`<br>`LabResultEntryAndApprovalComponentTests.cs`<br>`DoctorDiagnosticOrderEditorComponentTests.cs` |
| **SC-06** | Kritik laboratuvar sonucu ilgili klinik kullanıcıya gerçek zamanlı bildirilir. | E2E, Integration, Component, Unit | `Phase6ProductGateEndToEndTests.cs`<br>`CriticalResultIntegrationTests.cs`<br>`CriticalResultWorklistComponentTests.cs`<br>`NotificationDomainUnitTests.cs` |
| **SC-07** | Hasta yatırılır, yatağa atanır, servis işlemleri yürütülür ve taburcu edilir. | E2E, Integration, Component, Unit | `Phase7ProductGateEndToEndTests.cs`<br>`InpatientAdmissionIntegrationTests.cs`<br>`BedManagementIntegrationTests.cs`<br>`BedManagementComponentTests.cs`<br>`InpatientDomainUnitTests.cs` |
| **SC-08** | Acil, ameliyat, yoğun bakım ve uzmanlık modüllerinin ana akışları sentetik veriyle çalışır. | E2E, Integration, Component, Unit | `Phase7ProductGateEndToEndTests.cs`<br>`Phase9ProductGateEndToEndTests.cs`<br>`EmergencyEncounterFlowIntegrationTests.cs`<br>`SurgeryDomainUnitTests.cs`<br>`DentalCareIntegrationTests.cs`<br>`DeliveryRecordIntegrationTests.cs` |
| **SC-09** | Yetkisiz bir kullanıcı başka hastanın klinik verisini API veya arayüz üzerinden göremez. | Security, Integration, Architecture | `AuthorizationPolicyTests.cs`<br>`NativeDeepLinkAndNotificationSecurityTests.cs`<br>`ResourceScopeContractTests.cs`<br>`HospitalPermissionCatalogTests.cs` |
| **SC-10** | Hassas işlemler denetim kaydı üretir; raporlar değişiklikleri gerçek zamanlı yansıtır. | Integration, Component, Unit | `AuditLogIntegrationTests.cs`<br>`AuditLogUnitTests.cs`<br>`Phase11ReportingGateIntegrationTests.cs`<br>`ReportingReadModelsAndProjectionsTests.cs` |
| **SC-11** | Aynı API, web ile sonraki Windows/Android istemcilerine hizmet eder. | Gate, Component, Build | `Phase12MultiClientGateTests.cs`<br>`NativeAuthenticationHandlerTests.cs`<br>`PatientMobileWorkspaceComponentTests.cs`<br>`StaffDesktopWorkspaceComponentTests.cs`<br>`verify-platform-packaging.ps1` |
| **SC-12** | Temiz kurulum, test ve demo adımları GitHub README.md üzerinden tekrarlanabilir. | Architecture, Script, Gate | `CiWorkflowSecurityTests.cs`<br>`validate-phase0.ps1`<br>`start-local-infrastructure.ps1`<br>`apply-local-database-foundation.ps1` |

---

## 2. Rol Bazlı İzin ve Test Kapsamı Matrisi

| Rol | Ana Görev Alanı | Birim & Bileşen Testleri | Entegrasyon & E2E Testleri |
| :--- | :--- | :--- | :--- |
| **Hasta (Patient)** | Profil, randevu alma, reçete ve sonuç görüntüleme | `PatientMobileWorkspaceComponentTests`<br>`MyPrescriptionsComponentTests`<br>`MyDiagnosticResultsComponentTests` | `Phase3ProductGateEndToEndTests`<br>`Phase9ProductGateEndToEndTests`<br>`AppointmentLifecycleIntegrationTests` |
| **Kayıt Personeli** | Hasta arama, kabul ve randevu check-in | `DailyAppointmentQueueComponentTests`<br>`PatientManagementComponentTests` | `DailyAppointmentQueueIntegrationTests`<br>`PatientDomainUnitTests` |
| **Hekim (Doctor)** | Muayene, vital bulgu, tanı, reçete ve tetkik istemi | `StaffDesktopWorkspaceComponentTests`<br>`DoctorPrescriptionEditorComponentTests`<br>`DoctorDiagnosticOrderEditorComponentTests` | `Phase5ProductGateEndToEndTests`<br>`ClinicalEncounterIntegrationTests`<br>`DiagnosticOrderIntegrationTests` |
| **Hemşire (Nurse)** | Vital bulgu girişi, bakım planı ve ilaç uygulama (eMAR) | `NursingCareComponentTests`<br>`EmarComponentTests` | `ClinicalVitalSignsIntegrationTests`<br>`EmarIntegrationTests` |
| **Eczacı (Pharmacist)** | Reçete karşılama ve ilaç teslim kaydı | `PharmacyWorklistDispenseComponentTests`<br>`PharmacyWorklistComponentTests` | `PharmacyWorklistIntegrationTests`<br>`PharmacyDomainUnitTests` |
| **Laboratuvar Personeli** | Numune kabul, barkod ve tahlil sonuç onayı | `LabResultEntryAndApprovalComponentTests` | `DiagnosticOrderIntegrationTests`<br>`CriticalResultIntegrationTests` |
| **Radyoloji Personeli** | Modalite iş listesi ve radyoloji raporlama | `DoctorDiagnosticOrderEditorComponentTests` | `DicomPacsIntegrationTests`<br>`DicomSimulationIntegrationTests` |
| **Başhekim (CMO)** | Konsültasyon onayı, klinik denetim ve operasyonel özet | `StaffDesktopWorkspaceComponentTests`<br>`InAppNotificationCenterComponentTests` | `Phase11ReportingGateIntegrationTests`<br>`AuthorizationPolicyTests` |
| **Sistem Yöneticisi** | Kullanıcı, rol/izin yönetimi ve sistem logları | `AdminUserManagementUnitTests` | `AdminUserManagementIntegrationTests`<br>`IdentityLifecycleEndToEndTests` |

---

## 3. Flaky Test Önleme ve İzolasyon Standartları

1. **İzole Veri Tabanı**: Tüm entegrasyon testleri test bazlı rastgele şema (`Testcontainers PostgreSQL`) kullanır; testler birbirinin verisine bağımlı değildir.
2. **Saat ve Zaman Bağımsızlığı**: Statik `DateTime.Now` yerine yapılandırılabilir UTC zaman sağlayıcıları kullanılır.
3. **Deterministik Sıralama**: Sayfalama ve liste sorgularında `OrderBy` zorunludur.
4. **Sıfır Skip Kuralı**: Test paketlerinde gerekçesiz veya kalıcı `[Fact(Skip = "...")]` niteliği bulunmaz.
