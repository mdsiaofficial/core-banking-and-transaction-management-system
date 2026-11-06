using Bank.Api.IntegrationTests.Fixtures;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class ApiIntegrationSmokeTests : IntegrationTestBase
{
    public ApiIntegrationSmokeTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturnHealthy()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
