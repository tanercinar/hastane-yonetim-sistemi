using HospitalManagement.Modules.Interoperability.Domain.Medula;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class MedulaBoundaryDomainTests
{
    [Theory]
    [InlineData(MedulaOperationType.ProvizyonSorgu, MedulaOperationStatus.NotImplemented)]
    [InlineData(MedulaOperationType.ProvizyonTeyit, MedulaOperationStatus.OutOfScope)]
    [InlineData(MedulaOperationType.HakSahibiDogrulama, MedulaOperationStatus.DemoSuccess)]
    [InlineData(MedulaOperationType.ItsTeslimBildirimi, MedulaOperationStatus.NotImplemented)]
    [InlineData(MedulaOperationType.UtsDogrulamaSorgusu, MedulaOperationStatus.NotImplemented)]
    public void CreateDemoResponseReturnsCorrectStatusForEachOperationType(
        MedulaOperationType operationType,
        MedulaOperationStatus expectedStatus)
    {
        // Act
        var result = MedulaOperationResult.CreateDemoResponse(operationType, "DEMO test payload");

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(operationType, result.OperationType);
        Assert.Equal(expectedStatus, result.Status);
        Assert.NotEmpty(result.StatusDescription);
        Assert.Contains("DEMO", result.DemoDisclaimer);
        Assert.NotEmpty(result.ResponseSummary);
        Assert.Equal("DEMO test payload", result.RequestSummary);
    }

    [Fact]
    public void CreateDemoResponseAlwaysContainsDemoDisclaimer()
    {
        // Act & Assert
        foreach (var opType in Enum.GetValues<MedulaOperationType>())
        {
            var result = MedulaOperationResult.CreateDemoResponse(opType, "{}");
            Assert.Contains("DEMO", result.DemoDisclaimer);
            Assert.NotEmpty(result.StatusDescription);
        }
    }

    [Fact]
    public void CreateOutOfScopeRejectionReturnsExplicitBoundary()
    {
        // Act
        var result = MedulaOperationResult.CreateOutOfScopeRejection("FaturaOnay");

        // Assert
        Assert.Equal(MedulaOperationStatus.OutOfScope, result.Status);
        Assert.Contains("FaturaOnay", result.StatusDescription);
        Assert.Contains("DEMO", result.DemoDisclaimer);
        Assert.Contains("kapsam dışı", result.DemoDisclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("finans", result.DemoDisclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProvizyonTeyitIsExplicitlyOutOfScope()
    {
        // Act
        var result = MedulaOperationResult.CreateDemoResponse(MedulaOperationType.ProvizyonTeyit, "{}");

        // Assert
        Assert.Equal(MedulaOperationStatus.OutOfScope, result.Status);
        Assert.Contains("kapsam dışı", result.StatusDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("finansal", result.StatusDescription, StringComparison.OrdinalIgnoreCase);
    }
}
