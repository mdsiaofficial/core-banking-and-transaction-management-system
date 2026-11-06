using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bank.Api.IntegrationTests.Fixtures;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserDto User);

public record UserDto(Guid Id, string Email, Guid? CustomerId, IReadOnlyList<string> Roles);

public class IntegrationTestBase
{
    protected readonly PostgreSqlTestFixture Fixture;
    protected readonly HttpClient Client;

    public IntegrationTestBase(PostgreSqlTestFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.Factory.CreateClient();
    }

    protected HttpClient CreateClient() => Fixture.Factory.CreateClient();

    protected async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result!;
    }

    protected HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = Fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
