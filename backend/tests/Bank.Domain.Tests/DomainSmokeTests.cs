namespace Bank.Domain.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void DomainAssembly_ShouldBeAvailable()
    {
        DomainMarker.AssemblyName.Should().Be("Bank.Domain");
    }
}
