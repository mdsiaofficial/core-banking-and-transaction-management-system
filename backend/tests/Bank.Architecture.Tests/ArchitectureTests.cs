using Bank.Application;
using Bank.Domain;
using Bank.Infrastructure;
using NetArchTest.Rules;

namespace Bank.Architecture.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotDependOn_OtherProjects()
    {
        var result = Types.InAssembly(typeof(DomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Bank.Application", "Bank.Infrastructure", "Bank.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(ApplicationMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Bank.Infrastructure", "Bank.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(InfrastructureMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Bank.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
