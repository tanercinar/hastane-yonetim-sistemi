using System.Reflection;

namespace HospitalManagement.EndToEndTests;

public sealed class EndToEndExecutionPolicyTests
{
    [Fact]
    public void BrowserTestCollectionsRunSequentially()
    {
        var policy = typeof(EndToEndExecutionPolicyTests).Assembly
            .GetCustomAttribute<CollectionBehaviorAttribute>();

        Assert.NotNull(policy);
        Assert.True(policy.DisableTestParallelization);
    }
}
