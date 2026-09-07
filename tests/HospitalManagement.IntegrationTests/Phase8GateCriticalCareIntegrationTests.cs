using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class Phase8GateCriticalCareIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8WriteEndpointsRejectUnknownClinicalEnumValuesInsteadOfSilentlyDefaulting()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(database.ConnectionString);
        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        var emergency = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/emergency/admissions",
            new CreateEmergencyAdmissionRequest
            {
                PatientId = patientId,
                ArrivalType = "Teleportation",
                ChiefComplaint = "DEMO doğrulama girdisi",
            });
        Assert.Equal(HttpStatusCode.BadRequest, emergency.StatusCode);

        var rooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        var surgery = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/surgery/bookings",
            new CreateSurgeryBookingRequest
            {
                PatientId = patientId,
                DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000009"),
                DepartmentName = "DEMO Genel Cerrahi Bölümü",
                LeadSurgeonDoctorId = doctorId,
                AnesthesiologistDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000111"),
                OperatingRoomId = rooms!.First().Id,
                ProcedureCode = "DEMO-INVALID-ENUM",
                ProcedureName = "DEMO doğrulama işlemi",
                ScheduledStartTimeUtc = DateTime.UtcNow.AddDays(14),
                ScheduledEndTimeUtc = DateTime.UtcNow.AddDays(14).AddHours(1),
                Urgency = "HyperEmergency",
            });
        Assert.Equal(HttpStatusCode.BadRequest, surgery.StatusCode);

        var beds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        var icu = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/icu/admissions",
            new CreateIcuAdmissionRequest(
                Guid.NewGuid(),
                patientId,
                null,
                beds!.First().Id,
                doctorId,
                null,
                "DEMO doğrulama girdisi",
                "ImpossibleAcuity",
                30,
                "NoneSpontaneous",
                null));
        Assert.Equal(HttpStatusCode.BadRequest, icu.StatusCode);

        var handoff = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-handoffs",
            new InitiateClinicalHandoffRequest(
                patientId,
                null,
                null,
                "TeleportationBay",
                string.Empty,
                "Emergency",
                string.Empty,
                "DEMO durum",
                "DEMO arka plan",
                "DEMO değerlendirme",
                "DEMO öneri",
                null));
        Assert.Equal(HttpStatusCode.BadRequest, handoff.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8GateFullRepresentativeFlowEmergencyToSurgeryToIcuToHandoffAndDischargeSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var anesthesiologistPersonId = Guid.Parse("00000000-0000-0000-0000-000000000111");
        var surgeonPersonId = doctorPersonId;
        var generalSurgeryDeptId = Guid.Parse("30000000-0000-0000-0000-000000000009");
        var stayId = Guid.NewGuid();

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        // -------------------------------------------------------------
        // Step 1: Emergency Admission & Triage (F08-G01)
        // -------------------------------------------------------------
        var admissionReq = new CreateEmergencyAdmissionRequest
        {
            PatientId = patientId,
            ArrivalType = "Ambulance",
            ChiefComplaint = "Şiddetli sağ alt kadran karın ağrısı ve yüksek ateş",
            AdmissionNotes = "Akut batın şüphesi",
        };

        var admitResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/emergency/admissions", admissionReq);
        Assert.Equal(HttpStatusCode.Created, admitResp.StatusCode);
        var emergencyAdm = await admitResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(emergencyAdm);

        var triageReq = new RecordTriageRequest
        {
            TriageLevel = "Red1Resuscitation",
            TriageCategoryReason = "Akut perfore batın şüphesi ve taşikardi",
            HeartRate = 115,
            SystolicBp = 100,
            DiastolicBp = 60,
            RespiratoryRate = 22,
            OxygenSaturationPercent = 96,
            BodyTemperatureCelsius = 38.8m,
            Consciousness = "Alert",
            PainScale = 9,
            ClinicalNotes = "Acil cerrahi konsültasyonu önerildi",
        };

        var triageResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/emergency/admissions/{emergencyAdm.Id}/triage",
            triageReq);
        Assert.Equal(HttpStatusCode.OK, triageResp.StatusCode);

        // -------------------------------------------------------------
        // Step 2: Emergency Tracking Board (F08-G02)
        // -------------------------------------------------------------
        var board = await doctorClient.GetFromJsonAsync<EmergencyBoardSummaryResponse>("/api/v1/emergency/board/summary");
        Assert.NotNull(board);
        Assert.True(board.TotalActiveAdmissions >= 1);

        // -------------------------------------------------------------
        // Step 3: Emergency Encounter & Disposition (F08-G03)
        // -------------------------------------------------------------
        var dispReq = new RecordEmergencyDispositionRequest
        {
            DispositionType = "DirectToSurgery",
            TargetDepartmentName = "Genel Cerrahi Ameliyathanesi",
            DispositionSummaryNotes = "Akut perfore apandisit ön tanısıyla acil ameliyathaneye transfer ediliyor.",
            FollowUpInstructions = "Pre-op hidrasyon devam",
        };

        var dispResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/admissions/{emergencyAdm.Id}/disposition",
            dispReq);
        Assert.Equal(HttpStatusCode.OK, dispResp.StatusCode);

        // -------------------------------------------------------------
        // Step 4: Emergency -> Surgery Clinical Handoff (ISBAR - F08-G08)
        // -------------------------------------------------------------
        var handoff1Req = new InitiateClinicalHandoffRequest(
            patientId: patientId,
            inpatientStayId: stayId,
            encounterId: null,
            sourceArea: "Emergency",
            sourceLocationDetails: "Kırmızı Alan Yatak 1",
            destinationArea: "OperatingRoom",
            destinationLocationDetails: "Salon 1",
            situation: "Perfore apandisit, akut peritonit",
            background: "DM, HT, penisilin alerjisi",
            assessment: "TA: 100/60, HR: 115, Batın defans+, Laktat 2.8",
            recommendation: "Acil lap. apandektomi, pre-op antibiyotik tamamlandı",
            criticalAlerts: "Alerji: Penisilin");

        var handoff1Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-handoffs", handoff1Req);
        Assert.Equal(HttpStatusCode.Created, handoff1Resp.StatusCode);
        var handoff1 = await handoff1Resp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(handoff1);

        // Surgery Nurse accepts the handoff
        var acceptHandoff1Resp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-handoffs/{handoff1.Id}/accept",
            new AcceptClinicalHandoffRequest("Hasta ameliyathane giriş masasına kabul edildi."));
        Assert.Equal(HttpStatusCode.OK, acceptHandoff1Resp.StatusCode);

        // -------------------------------------------------------------
        // Step 5: Surgery Planning & Pre-Op Checklist (F08-G04)
        // -------------------------------------------------------------
        var orRooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        Assert.NotNull(orRooms);
        var orRoom = orRooms.First(r => r.RoomCode == "DEMO-OR-01");

        var startTime = DateTime.UtcNow.AddMinutes(30);
        var endTime = startTime.AddHours(2);

        var bookingReq = new CreateSurgeryBookingRequest
        {
            PatientId = patientId,
            EncounterId = null,
            DepartmentId = generalSurgeryDeptId,
            DepartmentName = "Genel Cerrahi Anabilim Dalı",
            LeadSurgeonDoctorId = surgeonPersonId,
            AnesthesiologistDoctorId = anesthesiologistPersonId,
            OperatingNurseStaffId = null,
            ProcedureCode = "PRC-LAP-APP",
            ProcedureName = "Laparoskopik Apandektomi",
            ScheduledStartTimeUtc = startTime,
            ScheduledEndTimeUtc = endTime,
            Urgency = "Emergency",
            OperatingRoomId = orRoom.Id,
            ClinicalNotes = "Acilden perfore apandisit",
        };

        var bookResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", bookingReq);
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var booking = await bookResp.Content.ReadFromJsonAsync<SurgeryBookingResponse>();
        Assert.NotNull(booking);

        // Verify Pre-Op Checklist completion
        var chkReq = new RecordPreOpChecklistRequest
        {
            ConsentSigned = true,
            SiteMarked = true,
            AnesthesiaClearance = true,
            NpoConfirmed = true,
            BloodProductsReserved = true,
            AllergyChecked = true,
            Notes = "Tüm pre-op güvenlik kriterleri tamamlandı.",
        };

        var chkResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/surgery/bookings/{booking.Id}/pre-op-checklist",
            chkReq);
        Assert.Equal(HttpStatusCode.OK, chkResp.StatusCode);

        // -------------------------------------------------------------
        // Step 6: Perioperative Record Flow (F08-G05)
        // -------------------------------------------------------------
        var periReq = new SavePerioperativeRecordRequest
        {
            SurgeryBookingId = booking.Id,
            AnesthesiaType = "General",
            RoomEntryTimeUtc = startTime,
            AnesthesiaStartTimeUtc = startTime.AddMinutes(10),
            IncisionTimeUtc = startTime.AddMinutes(20),
            ClosureTimeUtc = endTime.AddMinutes(-20),
            AnesthesiaEndTimeUtc = endTime.AddMinutes(-10),
            RoomExitTimeUtc = endTime,
            IntraoperativeFindings = "Retroçekal yerleşimli perfore apandiks, 50ml pürülan mayi aspire edildi.",
            IntraoperativeComplications = "Yok",
            EstimatedBloodLossMl = 50,
            SpecimensCollected = "Apandiks materyali patolojiye gönderildi.",
            CountsConfirmed = true,
            PostOpDisposition = "ICU",
            PostOpInstructions = "Post-op YBÜ takibi, 24 saat antibiyotik idamesi",
        };

        var periResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/perioperative-records", periReq);
        Assert.Equal(HttpStatusCode.OK, periResp.StatusCode);
        var periRecord = await periResp.Content.ReadFromJsonAsync<PerioperativeRecordResponse>();
        Assert.NotNull(periRecord);

        // Surgeon signs record (immutability)
        var signResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/surgery/perioperative-records/{periRecord.Id}/sign",
            new SignPerioperativeRecordRequest());
        Assert.Equal(HttpStatusCode.OK, signResp.StatusCode);

        // -------------------------------------------------------------
        // Step 7: PACU/OR -> ICU Clinical Handoff (ISBAR - F08-G08)
        // -------------------------------------------------------------
        var handoff2Req = new InitiateClinicalHandoffRequest(
            patientId: patientId,
            inpatientStayId: stayId,
            encounterId: null,
            sourceArea: "OperatingRoom",
            sourceLocationDetails: "Salon 1 / PACU",
            destinationArea: "IntensiveCareUnit",
            destinationLocationDetails: "ICU Yatak 1",
            situation: "Post-Op Lap. Apandektomi, genel anestezi sonrası ekstübe",
            background: "Perfore apandisit, retroçekal",
            assessment: "TA: 115/70, HR: 88, SpO2: 98 (Nazal O2), Batın dreni aktif",
            recommendation: "YBÜ vital takibi, batın dreni takibi, analjezi",
            criticalAlerts: "Alerji: Penisilin");

        var handoff2Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-handoffs", handoff2Req);
        Assert.Equal(HttpStatusCode.Created, handoff2Resp.StatusCode);
        var handoff2 = await handoff2Resp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(handoff2);

        // ICU Nurse accepts the handoff
        var acceptHandoff2Resp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-handoffs/{handoff2.Id}/accept",
            new AcceptClinicalHandoffRequest("Hasta YBÜ yatağına kabul edildi, monitörizasyon bağlandı."));
        Assert.Equal(HttpStatusCode.OK, acceptHandoff2Resp.StatusCode);

        // -------------------------------------------------------------
        // Step 8: ICU Admission & Bed Management (F08-G06)
        // -------------------------------------------------------------
        var icuBeds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        Assert.NotNull(icuBeds);
        var icuBed = icuBeds.First(b => b.BedCode == "DEMO-ICU-01");

        var icuAdmitReq = new CreateIcuAdmissionRequest(
            inpatientStayId: stayId,
            patientId: patientId,
            encounterId: null,
            icuBedId: icuBed.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "Post-Op Perfore Apandisit & Yakın İzlem",
            acuityLevel: "Level2IntensiveMonitoring",
            monitoringFrequencyMinutes: 30,
            ventilationMode: "HighFlowNasalCannula",
            carePlanNotes: "Dren ve sıvı takibi");

        var icuAdmitResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/icu/admissions", icuAdmitReq);
        Assert.Equal(HttpStatusCode.Created, icuAdmitResp.StatusCode);
        var icuAdm = await icuAdmitResp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(icuAdm);

        // -------------------------------------------------------------
        // Step 9: ICU Flowsheet Observation & Fluid Balance (F08-G07)
        // -------------------------------------------------------------
        var flowReq = new CreateIcuFlowsheetEntryRequest
        {
            HeartRateBpm = 82,
            SystolicBpMmHg = 120,
            DiastolicBpMmHg = 75,
            RespiratoryRateBpm = 16,
            OxygenSaturationPct = 98.5m,
            BodyTemperatureCelsius = 37.0m,
            GlasgowComaScale = 15,
            RichmondAgitationSedationScale = 0,
            VentilationMode = "HighFlowNasalCannula",
            FractionOfInspiredOxygenPct = 35,
            IvFluidIntakeMl = 150,
            EnteralNutritionIntakeMl = 0,
            UrineOutputMl = 100,
            DrainOutputMl = 25,
            ClinicalNotes = "Hasta oryante ve kooperatif, dren seröz akıyor.",
        };

        var flowResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{icuAdm.Id}/flowsheet",
            flowReq);
        Assert.Equal(HttpStatusCode.Created, flowResp.StatusCode);
        var flowEntry = await flowResp.Content.ReadFromJsonAsync<IcuFlowsheetEntryResponse>();
        Assert.NotNull(flowEntry);
        Assert.Equal(90, flowEntry.MeanArterialPressureMmHg);
        Assert.Equal(25, flowEntry.NetFluidBalanceMl); // 150 - 125

        var fluidSummary = await doctorClient.GetFromJsonAsync<IcuFluidBalanceSummaryResponse>(
            $"/api/v1/icu/admissions/{icuAdm.Id}/flowsheet/fluid-balance");
        Assert.NotNull(fluidSummary);
        Assert.Equal(1, fluidSummary.EntryCount);
        Assert.Equal(150, fluidSummary.TotalIntakeMl);
        Assert.Equal(125, fluidSummary.TotalOutputMl);
        Assert.Equal(25, fluidSummary.NetBalanceMl);

        // -------------------------------------------------------------
        // Step 10: ICU Discharge / Step-Down Transfer (F08-G06)
        // -------------------------------------------------------------
        var dischargeReq = new IcuDischargeOrTransferRequest
        {
            DestinationStatus = "Discharged",
            DischargeNotes = "Hemodinami ve vital bulgular stabil, batın rahat, genel cerrahi servisine devredildi.",
        };

        var dischargeResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{icuAdm.Id}/discharge-or-transfer",
            dischargeReq);
        Assert.Equal(HttpStatusCode.OK, dischargeResp.StatusCode);
        var dischargedAdm = await dischargeResp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(dischargedAdm);
        Assert.Equal("Discharged", dischargedAdm.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8GateRaceAndConcurrencyCollisionsEnforceAtomicConflicts()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var concurrentDoctorClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(concurrentDoctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);

        var patient1 = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var patient2 = Guid.Parse("00000000-0000-0000-0000-000000000122");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var anesthesiologistId = Guid.Parse("00000000-0000-0000-0000-000000000111");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000009");

        // 1. Operating Room Time Collision
        var orRooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        Assert.NotNull(orRooms);
        var orRoom = orRooms.First();

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(2);

        var book1 = new CreateSurgeryBookingRequest
        {
            PatientId = patient1,
            DepartmentId = deptId,
            DepartmentName = "Genel Cerrahi",
            LeadSurgeonDoctorId = doctorPersonId,
            AnesthesiologistDoctorId = anesthesiologistId,
            OperatingRoomId = orRoom.Id,
            ProcedureCode = "P1",
            ProcedureName = "Ameliyat 1",
            ScheduledStartTimeUtc = startTime,
            ScheduledEndTimeUtc = endTime,
            Urgency = "Elective",
        };

        // Conflicting booking in same OR at overlapping time.
        var book2 = new CreateSurgeryBookingRequest
        {
            PatientId = patient2,
            DepartmentId = deptId,
            DepartmentName = "Genel Cerrahi",
            LeadSurgeonDoctorId = doctorPersonId,
            AnesthesiologistDoctorId = anesthesiologistId,
            OperatingRoomId = orRoom.Id,
            ProcedureCode = "P2",
            ProcedureName = "Ameliyat 2",
            ScheduledStartTimeUtc = startTime.AddMinutes(30),
            ScheduledEndTimeUtc = endTime.AddMinutes(30),
            Urgency = "Elective",
        };

        var surgeryRace = await Task.WhenAll(
            PostWithAntiforgeryAsync(doctorClient, "/api/v1/surgery/bookings", book1),
            PostWithAntiforgeryAsync(concurrentDoctorClient, "/api/v1/surgery/bookings", book2));
        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            surgeryRace.Select(response => response.StatusCode).Order().ToArray());

        // 2. ICU Bed Occupancy Collision
        var icuBeds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        Assert.NotNull(icuBeds);
        var icuBed = icuBeds.First();

        var icuAdmit1 = new CreateIcuAdmissionRequest(
            inpatientStayId: Guid.NewGuid(),
            patientId: patient1,
            encounterId: null,
            icuBedId: icuBed.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "ARDS",
            acuityLevel: "Level3MultiOrganSupport",
            monitoringFrequencyMinutes: 15,
            ventilationMode: "InvasiveMechanical",
            carePlanNotes: null);

        // Second concurrent admission targets the same ICU bed.
        var icuAdmit2 = new CreateIcuAdmissionRequest(
            inpatientStayId: Guid.NewGuid(),
            patientId: patient2,
            encounterId: null,
            icuBedId: icuBed.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "Sepsis",
            acuityLevel: "Level3MultiOrganSupport",
            monitoringFrequencyMinutes: 15,
            ventilationMode: "InvasiveMechanical",
            carePlanNotes: null);

        var icuRace = await Task.WhenAll(
            PostWithAntiforgeryAsync(doctorClient, "/api/v1/icu/admissions", icuAdmit1),
            PostWithAntiforgeryAsync(concurrentDoctorClient, "/api/v1/icu/admissions", icuAdmit2));
        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            icuRace.Select(response => response.StatusCode).Order().ToArray());
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8GateAuthorizationMatrixSystemAdminCannotExecuteClinicalWorkflows()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");

        // 1. Emergency admission attempt -> 403 Forbidden
        var emgResp = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/emergency/admissions",
            new CreateEmergencyAdmissionRequest
            {
                PatientId = patientId,
                ArrivalType = "WalkIn",
                ChiefComplaint = "Ağrı",
            });
        Assert.Equal(HttpStatusCode.Forbidden, emgResp.StatusCode);

        // 2. Surgery booking attempt -> 403 Forbidden
        var orResp = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/surgery/bookings",
            new CreateSurgeryBookingRequest
            {
                PatientId = patientId,
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Dept",
                LeadSurgeonDoctorId = Guid.NewGuid(),
                AnesthesiologistDoctorId = Guid.NewGuid(),
                OperatingRoomId = Guid.NewGuid(),
                ProcedureCode = "PRC",
                ProcedureName = "Proc",
                ScheduledStartTimeUtc = DateTime.UtcNow,
                ScheduledEndTimeUtc = DateTime.UtcNow.AddHours(1),
                Urgency = "Elective",
            });
        Assert.Equal(HttpStatusCode.Forbidden, orResp.StatusCode);

        // 3. ICU admission attempt -> 403 Forbidden
        var icuResp = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/icu/admissions",
            new CreateIcuAdmissionRequest(
                inpatientStayId: Guid.NewGuid(),
                patientId: patientId,
                encounterId: null,
                icuBedId: Guid.NewGuid(),
                attendingDoctorId: Guid.NewGuid(),
                primaryNurseId: null,
                admissionReason: "Reason",
                acuityLevel: "Level1Monitoring",
                monitoringFrequencyMinutes: 60,
                ventilationMode: "NoneSpontaneous",
                carePlanNotes: null));
        Assert.Equal(HttpStatusCode.Forbidden, icuResp.StatusCode);

        // 4. Clinical handoff attempt -> 403 Forbidden
        var handoffResp = await PostWithAntiforgeryAsync(
            adminClient,
            "/api/v1/clinical-handoffs",
            new InitiateClinicalHandoffRequest(
                patientId, null, null, "Emergency", "Acil", "InpatientWard", "Servis",
                "S", "B", "A", "R", null));
        Assert.Equal(HttpStatusCode.Forbidden, handoffResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8ReadEndpointsRejectAnonymousAndNonClinicalAdministrator()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var anonymousClient = CreateSecureClient(application);
        using var anonymousResponse = await anonymousClient.GetAsync("/api/v1/emergency/board/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        string[] protectedUris =
        [
            "/api/v1/emergency/admissions",
            "/api/v1/emergency/board/summary",
            "/api/v1/emergency/board/worklist",
            "/api/v1/surgery/operating-rooms",
            "/api/v1/surgery/bookings",
            "/api/v1/icu/beds",
            "/api/v1/icu/admissions/active",
            "/api/v1/clinical-handoffs/pending",
        ];

        foreach (var uri in protectedUris)
        {
            using var response = await adminClient.GetAsync(uri);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8SurgeryRecordRequiresDepartmentAssignmentCareRelationshipOrTeamMembership()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var chiefClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(chiefClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1")).StatusCode);

        var rooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        var room = Assert.Single(rooms!, candidate => candidate.RoomCode == "DEMO-OR-01");
        var startUtc = DateTime.UtcNow.AddDays(2);

        var createResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/surgery/bookings",
            new CreateSurgeryBookingRequest
            {
                PatientId = Guid.Parse("00000000-0000-0000-0000-000000000121"),
                DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000009"),
                DepartmentName = "DEMO Genel Cerrahi Bölümü",
                LeadSurgeonDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                AnesthesiologistDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000111"),
                OperatingRoomId = room.Id,
                ProcedureCode = "DEMO-SCOPE",
                ProcedureName = "DEMO kapsam doğrulama işlemi",
                ScheduledStartTimeUtc = startUtc,
                ScheduledEndTimeUtc = startUtc.AddHours(1),
                Urgency = "Elective",
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var booking = await createResponse.Content.ReadFromJsonAsync<SurgeryBookingResponse>();
        Assert.NotNull(booking);

        using var outOfScopeResponse = await chiefClient.GetAsync($"/api/v1/surgery/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, outOfScopeResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-KAPI")]
    public async Task Phase8AuditMetadataDoesNotPersistClinicalFreeText()
    {
        const string clinicalCanary = "DEMO-CLINICAL-CANARY-MUST-NOT-ENTER-AUDIT";
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1")).StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var emergencyResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/emergency/admissions",
            new CreateEmergencyAdmissionRequest
            {
                PatientId = patientId,
                ArrivalType = "WalkIn",
                ChiefComplaint = clinicalCanary,
                AdmissionNotes = clinicalCanary,
            });
        Assert.Equal(HttpStatusCode.Created, emergencyResponse.StatusCode);
        var emergencyAdmission = await emergencyResponse.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(emergencyAdmission);

        var triageResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/admissions/{emergencyAdmission.Id}/triage",
            new RecordTriageRequest
            {
                TriageLevel = "YellowUrgent",
                TriageCategoryReason = clinicalCanary,
                ClinicalNotes = clinicalCanary,
                HeartRate = 92,
            });
        Assert.Equal(HttpStatusCode.OK, triageResponse.StatusCode);

        var rooms = await doctorClient.GetFromJsonAsync<List<OperatingRoomResponse>>("/api/v1/surgery/operating-rooms");
        var room = Assert.Single(rooms!, candidate => candidate.RoomCode == "DEMO-OR-01");
        var surgeryStartUtc = DateTime.UtcNow.AddDays(3);
        var bookingResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/surgery/bookings",
            new CreateSurgeryBookingRequest
            {
                PatientId = patientId,
                DepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000009"),
                DepartmentName = "DEMO Genel Cerrahi Bölümü",
                LeadSurgeonDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                AnesthesiologistDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000111"),
                OperatingRoomId = room.Id,
                ProcedureCode = "DEMO-AUDIT",
                ProcedureName = clinicalCanary,
                ClinicalNotes = clinicalCanary,
                ScheduledStartTimeUtc = surgeryStartUtc,
                ScheduledEndTimeUtc = surgeryStartUtc.AddHours(1),
                Urgency = "Elective",
            });
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        var beds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        var bed = Assert.Single(beds!, candidate => candidate.BedCode == "DEMO-ICU-01");
        var icuResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/icu/admissions",
            new CreateIcuAdmissionRequest(
                Guid.NewGuid(),
                patientId,
                null,
                bed.Id,
                Guid.Parse("00000000-0000-0000-0000-000000000102"),
                null,
                clinicalCanary,
                "Level2IntensiveMonitoring",
                30,
                "NoneSpontaneous",
                clinicalCanary));
        Assert.Equal(HttpStatusCode.Created, icuResponse.StatusCode);
        var icuAdmission = await icuResponse.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(icuAdmission);

        var flowsheetResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{icuAdmission.Id}/flowsheet",
            new CreateIcuFlowsheetEntryRequest
            {
                HeartRateBpm = 88,
                ClinicalNotes = clinicalCanary,
            });
        Assert.Equal(HttpStatusCode.Created, flowsheetResponse.StatusCode);

        var handoffResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/clinical-handoffs",
            new InitiateClinicalHandoffRequest(
                patientId,
                null,
                null,
                "Emergency",
                clinicalCanary,
                "IntensiveCareUnit",
                clinicalCanary,
                clinicalCanary,
                clinicalCanary,
                clinicalCanary,
                clinicalCanary,
                clinicalCanary));
        Assert.Equal(HttpStatusCode.Created, handoffResponse.StatusCode);

        using var scope = application.Services.CreateScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var metadata = await auditDb.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.Action.StartsWith("Emergency.") || entry.Action.StartsWith("Surgery."))
            .Select(entry => (entry.Reason ?? string.Empty) + "\n" + (entry.DetailsJson ?? string.Empty))
            .ToListAsync();

        Assert.NotEmpty(metadata);
        Assert.DoesNotContain(clinicalCanary, string.Join("\n", metadata), StringComparison.Ordinal);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost/"),
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/sessions",
            new LoginRequest { Email = email, Password = password });

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        return await client.SendAsync(request);
    }

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var auditDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var rxDb = sp.GetRequiredService<PharmacyDbContext>();
        await rxDb.Database.MigrateAsync();

        var schedDb = sp.GetRequiredService<SchedulingDbContext>();
        await schedDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var notifDb = sp.GetRequiredService<NotificationsDbContext>();
        await notifDb.Database.MigrateAsync();

        var emgDb = sp.GetRequiredService<EmergencyDbContext>();
        await emgDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var inpSeeder = sp.GetRequiredService<IInpatientDataSeeder>();
        await inpSeeder.SeedAsync();

        var emgSeeder = sp.GetRequiredService<IEmergencyDataSeeder>();
        await emgSeeder.SeedAsync();

        var surgSeeder = sp.GetRequiredService<ISurgeryDataSeeder>();
        await surgSeeder.SeedAsync();
    }
}
