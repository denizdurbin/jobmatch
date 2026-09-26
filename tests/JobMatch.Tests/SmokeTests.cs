namespace JobMatch.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_assembly_loads()
    {
        typeof(JobMatch.Core.Entities.PostingStatus).Assembly.ShouldNotBeNull();
    }
}
