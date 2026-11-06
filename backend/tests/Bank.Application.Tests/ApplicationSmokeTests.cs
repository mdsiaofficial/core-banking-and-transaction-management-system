namespace Bank.Application.Tests;

public class ApplicationSmokeTests
{
    [Fact]
    public void ApplicationAssembly_ShouldBeAvailable()
    {
        ApplicationMarker.AssemblyName.Should().Be("Bank.Application");
    }
}
