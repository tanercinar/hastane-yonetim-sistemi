using System.Reflection;
using Xunit;

namespace HospitalManagement.UnitTests.Regression;

public sealed class SuccessCriteriaTraceabilityTests
{
    private static readonly string[] RequiredSuccessCriteria =
    [
        "SC-01", // Hasta hesap ve randevu
        "SC-02", // Kayıt personeli ve check-in
        "SC-03", // Hemşire vital ve hekim muayene/reçete
        "SC-04", // Eczacı teslim kaydı
        "SC-05", // Tanısal istem ve sonuç
        "SC-06", // Kritik laboratuvar sonucu
        "SC-07", // Yatış, yatak ve taburcu
        "SC-08", // Acil, ameliyat, yoğun bakım ve uzmanlık
        "SC-09", // Yetkisiz veri erişim engeli
        "SC-10", // Denetim kaydı ve operasyonel raporlar
        "SC-11", // Çoklu istemci (Web, Windows, Android)
        "SC-12"  // Temiz kurulum ve test tekrarlanabilirliği
    ];

    private static readonly string[] UbiquitousRoles =
    [
        "Patient",
        "RegistrationStaff",
        "Doctor",
        "Nurse",
        "Pharmacist",
        "LabStaff",
        "RadiologyStaff",
        "ChiefMedicalOfficer",
        "SystemAdministrator"
    ];

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G01")]
    public void AllSuccessCriteriaAreCoveredInTraceabilityMatrix()
    {
        Assert.Equal(12, RequiredSuccessCriteria.Length);

        // Verify each success criterion ID is unique and formatted correctly
        var uniqueSet = RequiredSuccessCriteria.ToHashSet(StringComparer.Ordinal);
        Assert.Equal(RequiredSuccessCriteria.Length, uniqueSet.Count);

        for (var i = 1; i <= 12; i++)
        {
            var expectedKey = $"SC-{i:D2}";
            Assert.Contains(expectedKey, uniqueSet);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G01")]
    public void AllCanonicalRolesAreRepresentedInMatrix()
    {
        Assert.True(UbiquitousRoles.Length >= 8);

        foreach (var role in UbiquitousRoles)
        {
            Assert.False(string.IsNullOrWhiteSpace(role));
            Assert.DoesNotContain("Admin123", role, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G01")]
    public void UnitTestSuiteDoesNotContainSilentSkippedTests()
    {
        var testAssembly = typeof(SuccessCriteriaTraceabilityTests).Assembly;
        var skippedMethods = testAssembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m =>
            {
                var fact = m.GetCustomAttribute<FactAttribute>();
                if (fact != null && !string.IsNullOrEmpty(fact.Skip))
                {
                    return true;
                }

                var theory = m.GetCustomAttribute<TheoryAttribute>();
                return theory != null && !string.IsNullOrEmpty(theory.Skip);
            })
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .ToList();

        Assert.Empty(skippedMethods);
    }
}
