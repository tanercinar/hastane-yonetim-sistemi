using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using Xunit;

namespace HospitalManagement.UnitTests.SpecialtyCare;

public sealed class PregnancyTrackingDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G01")]
    public void CreatePregnancyEpisodeCalculatesNaegeleEddAndGeneratesProtocol()
    {
        var episodeId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var lmp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        var episode = PregnancyEpisode.Create(
            episodeId,
            patientId,
            encounterId,
            gravida: 2,
            para: 1,
            abortus: 0,
            livingChildren: 1,
            lastMenstrualPeriodUtc: lmp,
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: "A Rh(+)",
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: "Standart takip",
            assignedDoctorId: Guid.NewGuid(),
            assignedMidwifeId: Guid.NewGuid(),
            nowUtc: now);

        Assert.Equal(episodeId, episode.Id);
        Assert.Equal(patientId, episode.PatientId);
        Assert.Equal(encounterId, episode.OpeningEncounterId);
        Assert.Equal(2, episode.Gravida);
        Assert.Equal(1, episode.Para);
        Assert.Equal(lmp.AddDays(280), episode.EstimatedDeliveryDateUtc); // 40 weeks Naegele rule
        Assert.Equal(PregnancyEpisodeStatus.Active, episode.Status);
        Assert.StartsWith("DEMO-OBS-20260301-", episode.EpisodeProtocolNumber);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G01")]
    public void RecordAntenatalVisitAppendsToEpisodeAndEnforcesPhysiologicalBoundaries()
    {
        var episode = PregnancyEpisode.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            gravida: 1,
            para: 0,
            abortus: 0,
            livingChildren: 0,
            lastMenstrualPeriodUtc: DateTime.UtcNow.AddDays(-100),
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: "0 Rh(+)",
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: null,
            assignedDoctorId: null,
            assignedMidwifeId: null,
            nowUtc: DateTime.UtcNow);

        var staffId = Guid.NewGuid();
        var visit = episode.RecordAntenatalVisit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            visitDateUtc: DateTime.UtcNow,
            gestationalAgeWeeks: 14,
            gestationalAgeDays: 2,
            maternalWeightKg: 62.5m,
            systolicBpMmHg: 110,
            diastolicBpMmHg: 70,
            fundalHeightCm: 14.0m,
            fetalHeartRateBpm: 152,
            fetalPresentation: FetalPresentation.Undetermined,
            edemaLevel: EdemaLevel.None,
            urineProteinPresent: false,
            urineGlucosePresent: false,
            staffId: staffId,
            clinicalNotes: "Normal fetal kalp atımı duyuldu.",
            nextVisitRecommendedDateUtc: DateTime.UtcNow.AddDays(28),
            nowUtc: DateTime.UtcNow);

        Assert.NotNull(visit);
        Assert.Single(episode.AntenatalVisits);
        Assert.Equal(14, visit.GestationalAgeWeeks);
        Assert.Equal(152, visit.FetalHeartRateBpm);

        // Invalid FHR throws ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            episode.RecordAntenatalVisit(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                14,
                2,
                62.5m,
                110,
                70,
                14.0m,
                fetalHeartRateBpm: 300, // Invalid FHR
                FetalPresentation.Undetermined,
                EdemaLevel.None,
                false,
                false,
                staffId,
                null,
                null,
                DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G01")]
    public void CompletedEpisodeCannotReceiveNewVisitsOrRiskUpdates()
    {
        var episode = PregnancyEpisode.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            gravida: 1,
            para: 0,
            abortus: 0,
            livingChildren: 0,
            lastMenstrualPeriodUtc: DateTime.UtcNow.AddDays(-280),
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: "B Rh(+)",
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: null,
            assignedDoctorId: null,
            assignedMidwifeId: null,
            nowUtc: DateTime.UtcNow);

        episode.CompleteEpisode(PregnancyEpisodeStatus.Delivered, DateTime.UtcNow);
        Assert.Equal(PregnancyEpisodeStatus.Delivered, episode.Status);

        Assert.Throws<InvalidOperationException>(() =>
            episode.RecordAntenatalVisit(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                40,
                0,
                70m,
                120,
                80,
                38m,
                140,
                FetalPresentation.Cephalic,
                EdemaLevel.None,
                false,
                false,
                Guid.NewGuid(),
                null,
                null,
                DateTime.UtcNow));

        Assert.Throws<InvalidOperationException>(() =>
            episode.CompleteEpisode(PregnancyEpisodeStatus.Delivered, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G01")]
    public void GravidaAndCounterBoundariesAreEnforced()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PregnancyEpisode.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                gravida: 0, // Gravida must be >= 1
                para: 0,
                abortus: 0,
                livingChildren: 0,
                lastMenstrualPeriodUtc: DateTime.UtcNow,
                customEstimatedDeliveryDateUtc: null,
                bloodGroupAndRh: null,
                riskCategory: PregnancyRiskCategory.LowRisk,
                riskFactorsNotes: null,
                assignedDoctorId: null,
                assignedMidwifeId: null,
                nowUtc: DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G01")]
    public void EncounterReferencesAreRequired()
    {
        Assert.Throws<ArgumentException>(() => PregnancyEpisode.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            gravida: 1,
            para: 0,
            abortus: 0,
            livingChildren: 0,
            lastMenstrualPeriodUtc: DateTime.UtcNow.AddDays(-70),
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: null,
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: null,
            assignedDoctorId: null,
            assignedMidwifeId: null,
            nowUtc: DateTime.UtcNow));

        var episode = PregnancyEpisode.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            gravida: 1,
            para: 0,
            abortus: 0,
            livingChildren: 0,
            lastMenstrualPeriodUtc: DateTime.UtcNow.AddDays(-70),
            customEstimatedDeliveryDateUtc: null,
            bloodGroupAndRh: null,
            riskCategory: PregnancyRiskCategory.LowRisk,
            riskFactorsNotes: null,
            assignedDoctorId: null,
            assignedMidwifeId: null,
            nowUtc: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => episode.RecordAntenatalVisit(
            Guid.NewGuid(),
            Guid.Empty,
            DateTime.UtcNow,
            10,
            0,
            null,
            null,
            null,
            null,
            null,
            FetalPresentation.Undetermined,
            EdemaLevel.None,
            false,
            false,
            Guid.NewGuid(),
            null,
            null,
            DateTime.UtcNow));
    }
}
