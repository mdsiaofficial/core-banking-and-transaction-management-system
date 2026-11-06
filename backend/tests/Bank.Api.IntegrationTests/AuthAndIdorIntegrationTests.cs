using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bank.Api.Endpoints;
using Bank.Api.IntegrationTests.Fixtures;
using Bank.Application.Accounts;
using Bank.Application.Payments;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class AuthAndIdorIntegrationTests : IntegrationTestBase
{
    public AuthAndIdorIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task RefreshTokenRotation_And_ReuseDetection_ShouldRevokeTokenFamily()
    {
        // 1. Login as Afroza to get initial tokens
        var loginResp = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        loginResp.RefreshToken.Should().NotBeNullOrWhiteSpace();
        string initialRefreshToken = loginResp.RefreshToken;

        using var client = CreateClient();

        // 2. Normal rotation: use initial refresh token to obtain rotated token pair
        var refreshReq1 = new RefreshRequest(initialRefreshToken);
        var response1 = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        string rotatedRefreshToken = refreshBody1.GetProperty("refreshToken").GetString()!;
        rotatedRefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshToken.Should().NotBe(initialRefreshToken);

        // 3. Token Reuse Attack: replay the OLD (now consumed/rotated) initial refresh token
        var reuseReq = new RefreshRequest(initialRefreshToken);
        var responseReuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", reuseReq);
        responseReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var reuseProblem = await responseReuse.Content.ReadAsStringAsync();
        reuseProblem.Should().Contain("TOKEN_REUSE_DETECTED");

        // 4. Verification that family is revoked: trying to use the rotated token also fails
        var familyReq = new RefreshRequest(rotatedRefreshToken);
        var responseFamily = await client.PostAsJsonAsync("/api/v1/auth/refresh", familyReq);
        responseFamily.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Customer_CannotAccessForeignAccount_IDOR_ShouldReturnForbidden_403()
    {
        // Afroza login
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        // Admin login to discover Babul's account ID
        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");

        var bobAccount = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.CustomerId != aliceAuth.User.CustomerId);

        // 1. Afroza attempts to GET Babul's account details -> 403 Forbidden
        var accResponse = await aliceClient.GetAsync($"/api/v1/accounts/{bobAccount.Id}");
        accResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. Afroza attempts to GET Babul's account balance -> 403 Forbidden
        var balResponse = await aliceClient.GetAsync($"/api/v1/accounts/{bobAccount.Id}/balance");
        balResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Afroza attempts to GET Babul's transactions -> 403 Forbidden
        var txnResponse = await aliceClient.GetAsync($"/api/v1/accounts/{bobAccount.Id}/transactions");
        txnResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Afroza attempts to export Babul's statement -> 403 Forbidden
        var stmtResponse = await aliceClient.GetAsync($"/api/v1/accounts/{bobAccount.Id}/statement");
        stmtResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanAccessAnyAccount_ShouldReturnOk_200()
    {
        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);

        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        allAccounts.Should().NotBeEmpty();

        var anyAccount = allAccounts!.First();
        var response = await adminClient.GetAsync($"/api/v1/accounts/{anyAccount.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Customer_CannotWithdrawFromForeignAccount_IDOR_ShouldReturnForbidden_403()
    {
        // 1. Setup Afroza (attacker) and discover Babul's account
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");

        var bobAccount = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.CustomerId != aliceAuth.User.CustomerId);

        // 2. Afroza attempts to withdraw from Babul's account
        var withdrawPayload = new WithdrawalRequest(
            bobAccount.Id,
            50.0000m,
            "BDT",
            "Malicious IDOR withdrawal attempt");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/withdrawals");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(withdrawPayload);

        var response = await aliceClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotTransferFromForeignAccount_IDOR_ShouldReturnForbidden_403()
    {
        // 1. Setup Afroza (attacker) and discover Babul's account
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");

        var aliceAccount = allAccounts!.First(a => a.CustomerId == aliceAuth.User.CustomerId);
        var bobAccount = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.CustomerId != aliceAuth.User.CustomerId);

        // 2. Afroza attempts to transfer funds out of Babul's account into her own
        var transferPayload = new TransferRequest(
            bobAccount.Id,
            aliceAccount.Id,
            100.0000m,
            "BDT",
            "Malicious IDOR transfer attempt");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Content = JsonContent.Create(transferPayload);

        var response = await aliceClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotViewForeignTransactionDetails_IDOR_ShouldReturnForbidden_403()
    {
        // 1. Babul logs in and deposits money to create a transaction involving only Babul
        var bobAuth = await LoginAsync("babul.mia@bank.local", "Customer@Pass1234!");
        using var bobClient = CreateAuthenticatedClient(bobAuth.AccessToken);

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");

        var bobAccount = allAccounts!.First(a => a.CustomerId == bobAuth.User.CustomerId);

        var depositPayload = new DepositRequest(
            bobAccount.Id,
            250.0000m,
            "BDT",
            "Babul private deposit");

        using var depositReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deposits");
        depositReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        depositReq.Content = JsonContent.Create(depositPayload);

        var depositResp = await bobClient.SendAsync(depositReq);
        depositResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var bobTxn = await depositResp.Content.ReadFromJsonAsync<TransactionDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        bobTxn.Should().NotBeNull();

        // 2. Babul can view his own transaction
        var bobViewResp = await bobClient.GetAsync($"/api/v1/transactions/{bobTxn!.Id}");
        bobViewResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Admin can view the transaction
        var adminViewResp = await adminClient.GetAsync($"/api/v1/transactions/{bobTxn.Id}");
        adminViewResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Afroza (unrelated customer) attempts to view Babul's transaction -> 403 Forbidden
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var aliceViewResp = await aliceClient.GetAsync($"/api/v1/transactions/{bobTxn.Id}");
        aliceViewResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
