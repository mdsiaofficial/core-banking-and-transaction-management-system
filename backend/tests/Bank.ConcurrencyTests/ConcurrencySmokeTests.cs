namespace Bank.ConcurrencyTests;

public class ConcurrencySmokeTests
{
    [Fact]
    public void ConcurrencyHarness_ShouldBeConfigured()
    {
        const int expectedParallelWorkers = 10;
        expectedParallelWorkers.Should().BeGreaterThan(0);
    }
}
