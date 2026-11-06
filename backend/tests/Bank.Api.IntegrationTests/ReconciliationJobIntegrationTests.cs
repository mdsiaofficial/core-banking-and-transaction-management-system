using System.Net;
using System.Net.Http.Json;
using Bank.Api.IntegrationTests.Fixtures;
using Bank.Application.Accounts;
using Bank.Application.Reconciliation;
using Npgsql;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class ReconciliationJobIntegrationTests : IntegrationTestBase
{
    public ReconciliationJobIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task RunReconciliation_WhenDiscrepancyInjected_ShouldRecordCriticalFinding()
    {
        var adminAuth = await LoginAsync("admin@bank.local", "Admin@Pass1234!");
        using var adminClient = CreateAuthenticatedClient(adminAuth.AccessToken);

        // Fetch demo accounts
        var accounts = await adminClient.GetFromJsonAsync<List<AccountDto>>("/api/v1/accounts");
        var targetAccount = accounts!.First(a => a.Kind == "Customer");

        // Inject artificial balance divergence bypassing ledger via direct SQL update
        await using (var conn = Fixture.CreateConnection())
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE accounts SET balance = balance + 999.0000 WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", targetAccount.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        // Trigger reconciliation run via Admin API
        var response = await adminClient.PostAsync("/api/v1/admin/reconciliation/runs", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var run = await response.Content.ReadFromJsonAsync<ReconciliationRunDto>();
        run.Should().NotBeNull();
        run!.Status.Should().Be("DISCREPANCIES_DETECTED");
        run.DiscrepanciesFound.Should().BeGreaterThan(0);

        var finding = run.Findings.FirstOrDefault(f => f.AccountId == targetAccount.Id);
        finding.Should().NotBeNull();
        finding!.Severity.Should().Be("CRITICAL");
        finding.ActualBalance.Should().Be(targetAccount.Balance + 999.0000m);
        finding.Description.Should().Contain("Balance discrepancy");

        // Verify finding was recorded in DB
        await using (var conn = Fixture.CreateConnection())
        {
            await conn.OpenAsync();
            await using var verifyCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM reconciliation_findings WHERE run_id = @runId AND account_id = @accountId", conn);
            verifyCmd.Parameters.AddWithValue("runId", run.Id);
            verifyCmd.Parameters.AddWithValue("accountId", targetAccount.Id);
            long findingCount = Convert.ToInt64(await verifyCmd.ExecuteScalarAsync());
            findingCount.Should().Be(1);

            // Revert injected discrepancy to keep DB consistent
            await using var revertCmd = new NpgsqlCommand("UPDATE accounts SET balance = balance - 999.0000 WHERE id = @id", conn);
            revertCmd.Parameters.AddWithValue("id", targetAccount.Id);
            await revertCmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task RunReconciliation_NonAdminUser_ShouldReturnForbidden_403()
    {
        var customerAuth = await LoginAsync("afroza.begum@bank.local", "Customer@Pass1234!");
        using var customerClient = CreateAuthenticatedClient(customerAuth.AccessToken);

        var response = await customerClient.PostAsync("/api/v1/admin/reconciliation/runs", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
