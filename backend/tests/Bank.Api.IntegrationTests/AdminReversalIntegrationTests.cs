using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bank.Api.Endpoints;
using Bank.Api.IntegrationTests.Fixtures;
using Bank.Application.Accounts;
using Bank.Application.Payments;
using Npgsql;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class AdminReversalIntegrationTests : IntegrationTestBase
{
    public AdminReversalIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AdminReversal_ShouldCreateCompensatingTransactionAndRestoreBalances()
    {
        // 1. Setup users & accounts
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);

        var allAccounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var aliceAcc = allAccounts!.First(a => a.CustomerId == aliceAuth.User.CustomerId);
        var bobAcc = allAccounts!.First(a => a.CustomerId != aliceAuth.User.CustomerId && a.AccountNumber.StartsWith("101"));

        decimal aliceInitialBalance = aliceAcc.Balance;
        decimal bobInitialBalance = bobAcc.Balance;

        // 2. Perform a transfer of 1,000 BDT from Alice to Bob
        var transferPayload = new TransferRequest(
            aliceAcc.Id,
            bobAcc.Id,
            1000.0000m,
            "BDT",
            "Transfer to be reversed");

        using var transferReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers");
        transferReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        transferReq.Content = JsonContent.Create(transferPayload);

        var transferResp = await aliceClient.SendAsync(transferReq);
        transferResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var originalTxn = await transferResp.Content.ReadFromJsonAsync<TransactionDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        originalTxn.Should().NotBeNull();

        // 3. Admin issues reversal
        var reversalPayload = new ReasonDto("Customer requested cancellation");
        using var reversalReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/transactions/{originalTxn!.Id}/reversal");
        reversalReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        reversalReq.Content = JsonContent.Create(reversalPayload);

        var reversalResp = await adminClient.SendAsync(reversalReq);
        reversalResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var reversalTxn = await reversalResp.Content.ReadFromJsonAsync<TransactionDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        reversalTxn.Should().NotBeNull();
        reversalTxn!.Type.Should().Be("Reversal");
        reversalTxn.ReversesTransactionId.Should().Be(originalTxn.Id);
        reversalTxn.Amount.Should().Be(originalTxn.Amount);

        // Verify Compensating Ledger Entries: Bob is Debited, Alice is Credited
        reversalTxn.Entries.Should().HaveCount(2);

        var bobReversalEntry = reversalTxn.Entries.Single(e => e.AccountId == bobAcc.Id);
        bobReversalEntry.Direction.Should().Be("Debit");
        bobReversalEntry.Amount.Should().Be(1000.0000m);

        var aliceReversalEntry = reversalTxn.Entries.Single(e => e.AccountId == aliceAcc.Id);
        aliceReversalEntry.Direction.Should().Be("Credit");
        aliceReversalEntry.Amount.Should().Be(1000.0000m);

        // 4. Verify balances in database: perfectly restored to initial
        await using var conn = Fixture.CreateConnection();
        await conn.OpenAsync();

        await using var cmdAlice = new NpgsqlCommand("SELECT balance FROM accounts WHERE id = @id;", conn);
        cmdAlice.Parameters.AddWithValue("id", aliceAcc.Id);
        decimal aliceRestoredBalance = (decimal)(await cmdAlice.ExecuteScalarAsync())!;
        aliceRestoredBalance.Should().Be(aliceInitialBalance);

        await using var cmdBob = new NpgsqlCommand("SELECT balance FROM accounts WHERE id = @id;", conn);
        cmdBob.Parameters.AddWithValue("id", bobAcc.Id);
        decimal bobRestoredBalance = (decimal)(await cmdBob.ExecuteScalarAsync())!;
        bobRestoredBalance.Should().Be(bobInitialBalance);

        // 5. Verify original transaction in database was NOT modified or deleted (immutable)
        await using var cmdOrig = new NpgsqlCommand("SELECT status, type FROM transactions WHERE id = @id;", conn);
        cmdOrig.Parameters.AddWithValue("id", originalTxn.Id);
        await using var reader = await cmdOrig.ExecuteReaderAsync();
        await reader.ReadAsync();
        reader.GetString(0).Should().Be("Posted");
        reader.GetString(1).Should().Be("Transfer");
        await reader.CloseAsync();

        // 6. Attempting to reverse again MUST fail with 400
        using var secondReversalReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/transactions/{originalTxn.Id}/reversal");
        secondReversalReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        secondReversalReq.Content = JsonContent.Create(new ReasonDto("Attempt duplicate reversal"));

        var secondReversalResp = await adminClient.SendAsync(secondReversalReq);
        secondReversalResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var secondContent = await secondReversalResp.Content.ReadAsStringAsync();
        secondContent.Should().Contain("TRANSACTION_ALREADY_REVERSED");

        // 7. Attempting to reverse a reversal transaction itself MUST fail with 400 CANNOT_REVERSE_REVERSAL
        using var reverseReversalReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/transactions/{reversalTxn.Id}/reversal");
        reverseReversalReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        reverseReversalReq.Content = JsonContent.Create(new ReasonDto("Attempt reversal of a reversal"));

        var reverseReversalResp = await adminClient.SendAsync(reverseReversalReq);
        reverseReversalResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reverseReversalContent = await reverseReversalResp.Content.ReadAsStringAsync();
        reverseReversalContent.Should().Contain("CANNOT_REVERSE_REVERSAL");
    }

    [Fact]
    public async Task Customer_AttemptingReversal_ShouldReturnForbidden_403()
    {
        var aliceAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var aliceClient = CreateAuthenticatedClient(aliceAuth.AccessToken);

        using var reversalReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/transactions/{Guid.NewGuid()}/reversal");
        reversalReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        reversalReq.Content = JsonContent.Create(new ReasonDto("Malicious customer reversal attempt"));

        var response = await aliceClient.SendAsync(reversalReq);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
