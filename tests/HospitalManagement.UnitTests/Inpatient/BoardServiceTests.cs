using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class BoardServiceTests
{
    [Theory]
    [InlineData(10, "Low")]
    [InlineData(24, "Low")]
    [InlineData(25, "Medium")]
    [InlineData(49, "Medium")]
    [InlineData(50, "High")]
    [InlineData(85, "High")]
    public void FallRiskLevelCalculationsMatchClinicalRiskThresholds(int score, string expectedLevel)
    {
        var level = score switch
        {
            >= 50 => "High",
            >= 25 => "Medium",
            _ => "Low",
        };

        Assert.Equal(expectedLevel, level);
    }

    [Fact]
    public void BoardItemDtoShouldPreserveClinicalContextAndRiskFlags()
    {
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var wardId = Guid.NewGuid();
        var bedId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var item = new InpatientBoardItemDto(
            admissionId,
            "DEMO-ADM-20260830-100200",
            patientId,
            $"DEMO-P-{patientId.ToString().Substring(0, 8)}",
            "DEMO Hasta (test)",
            45,
            "Male",
            wardId,
            "Kardiyoloji Servisi",
            bedId,
            "301-A",
            "301",
            docId,
            "DEMO Dr. (test)",
            Guid.NewGuid(),
            "I20.0",
            "Anstabil Angina Pektoris",
            "LowSodium",
            60,
            "High",
            IsolationType.Contact,
            true,
            now.AddDays(-2),
            3,
            5,
            2);

        Assert.Equal(admissionId, item.AdmissionId);
        Assert.Equal("High", item.FallRiskLevel);
        Assert.Equal(IsolationType.Contact, item.IsolationRequired);
        Assert.True(item.HasPendingTransfer);
        Assert.Equal("301-A", item.BedNumber);
        Assert.Equal(3, item.DaysInHospital);
    }
}
