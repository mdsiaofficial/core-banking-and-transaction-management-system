using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bank.Api.IntegrationTests.Fixtures;
using Bank.Application.Accounts;
using Bank.Application.Payments;
using Npgsql;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class IdempotencyIntegrationTests : IntegrationTestBase
{
    public IdempotencyIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Transfer_WithoutIdempotencyKey_ShouldReturnBadRequest_400()
    {
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var client = CreateAuthenticatedClient(aliceAuth.AccessToken);

        // Fetch accounts
        var accountsResp = await client.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        accountsResp.Should().NotBeNull();
        var aliceAcc = accountsResp!.First();

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var bobAcc = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.Id != aliceAcc.Id);

        var transferPayload = new TransferRequest(
            SourceAccountId: aliceAcc.Id,
            DestinationAccountId: bobAcc.Id,
            Amount: 100.0000m,
            Currency: "BDT",
            Description: "Transfer missing key");

        // Do not add Idempotency-Key header
        var response = await client.PostAsJsonAsync("/api/v1/transfers", transferPayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("IDEMPOTENCY_KEY_REQUIRED");
    }

    [Fact]
    public async Task Transfer_WithSameIdempotencyKey_ShouldReplayWithoutDuplicateLedgerEntriesOrBalanceDeductions()
    {
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var client = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var accountsResp = await client.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var aliceAcc = accountsResp!.First();

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var bobAcc = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.Id != aliceAcc.Id);

        decimal aliceInitialBalance = aliceAcc.Balance;
        decimal bobInitialBalance = bobAcc.Balance;

        string idempotencyKey = Guid.NewGuid().ToString();
        var transferPayload = new TransferRequest(
            SourceAccountId: aliceAcc.Id,
            DestinationAccountId: bobAcc.Id,
            Amount: 250.0000m,
            Currency: "BDT",
            Description: "Idempotent transfer test");

        // 1. First execution
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        firstRequest.Content = JsonContent.Create(transferPayload);

        var firstResponse = await client.SendAsync(firstRequest);
        var responseBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, because: responseBody);

        var firstTxn = await firstResponse.Content.ReadFromJsonAsync<TransactionDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        firstTxn.Should().NotBeNull();
        firstTxn!.Amount.Should().Be(250.0000m);

        // Verify database entry count after first request
        await using var conn = Fixture.CreateConnection();
        await conn.OpenAsync();
        await using var countCmd1 = new NpgsqlCommand(
            "SELECT COUNT(*) FROM ledger_entries WHERE transaction_id = @tid;", conn);
        countCmd1.Parameters.AddWithValue("tid", firstTxn.Id);
        long entriesCount1 = (long)(await countCmd1.ExecuteScalarAsync())!;
        entriesCount1.Should().Be(2); // Exactly 1 debit and 1 credit

        // 2. Replay with identical Idempotency-Key and payload
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Content = JsonContent.Create(transferPayload);

        var secondResponse = await client.SendAsync(secondRequest);

        // Replayed response
        secondResponse.IsSuccessStatusCode.Should().BeTrue();
        secondResponse.Headers.Contains("Idempotent-Replayed").Should().BeTrue();

        var replayedTxn = await secondResponse.Content.ReadFromJsonAsync<TransactionDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        replayedTxn!.Id.Should().Be(firstTxn.Id);
        replayedTxn.Reference.Should().Be(firstTxn.Reference);

        // Verify database entry count after replay: NO DUPLICATE ENTRIES
        await using var countCmd2 = new NpgsqlCommand(
            "SELECT COUNT(*) FROM ledger_entries WHERE transaction_id = @tid;", conn);
        countCmd2.Parameters.AddWithValue("tid", firstTxn.Id);
        long entriesCount2 = (long)(await countCmd2.ExecuteScalarAsync())!;
        entriesCount2.Should().Be(2);

        // Verify balances in database: exactly ONE deduction of 250, NOT two
        await using var balCmdAlice = new NpgsqlCommand("SELECT balance FROM accounts WHERE id = @id;", conn);
        balCmdAlice.Parameters.AddWithValue("id", aliceAcc.Id);
        decimal aliceFinalBalance = (decimal)(await balCmdAlice.ExecuteScalarAsync())!;
        aliceFinalBalance.Should().Be(aliceInitialBalance - 250.0000m);

        await using var balCmdBob = new NpgsqlCommand("SELECT balance FROM accounts WHERE id = @id;", conn);
        balCmdBob.Parameters.AddWithValue("id", bobAcc.Id);
        decimal bobFinalBalance = (decimal)(await balCmdBob.ExecuteScalarAsync())!;
        bobFinalBalance.Should().Be(bobInitialBalance + 250.0000m);
    }

    [Fact]
    public async Task Transfer_WithReusedKeyAndDifferentPayload_ShouldReturnUnprocessableEntity_422()
    {
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var client = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var accountsResp = await client.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var aliceAcc = accountsResp!.First();

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);
        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var bobAcc = allAccounts!.First(a => a.AccountNumber.StartsWith("101") && a.Id != aliceAcc.Id);

        string idempotencyKey = Guid.NewGuid().ToString();

        // 1. Initial request with 100 BDT
        var payload1 = new TransferRequest(aliceAcc.Id, bobAcc.Id, 100.0000m, "BDT", "Original payload");
        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        request1.Content = JsonContent.Create(payload1);

        var response1 = await client.SendAsync(request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Reuse SAME key with DIFFERENT payload (500 BDT instead of 100)
        var payload2 = new TransferRequest(aliceAcc.Id, bobAcc.Id, 500.0000m, "BDT", "Different amount payload");
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        request2.Content = JsonContent.Create(payload2);

        var response2 = await client.SendAsync(request2);

        response2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response2.Content.ReadAsStringAsync();
        content.Should().Contain("IDEMPOTENCY_KEY_REUSED");
    }
}
