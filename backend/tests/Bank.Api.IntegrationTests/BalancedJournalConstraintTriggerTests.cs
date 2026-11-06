using Bank.Api.IntegrationTests.Fixtures;
using Npgsql;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class BalancedJournalConstraintTriggerTests
{
    private readonly PostgreSqlTestFixture _fixture;

    public BalancedJournalConstraintTriggerTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Commit_UnbalancedTransaction_ShouldFailWithConstraintTriggerException_ERRCODE_23514()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        // Fetch two accounts
        await using var cmdAccounts = new NpgsqlCommand("SELECT id FROM accounts LIMIT 2;", conn);
        await using var reader = await cmdAccounts.ExecuteReaderAsync();
        await reader.ReadAsync();
        var acc1 = reader.GetGuid(0);
        await reader.ReadAsync();
        var acc2 = reader.GetGuid(0);
        await reader.CloseAsync();

        await using var tx = await conn.BeginTransactionAsync();

        var txnId = Guid.CreateVersion7();
        await using var cmdTxn = new NpgsqlCommand(@"
            INSERT INTO transactions (id, reference, type, status, amount, currency, description, channel, initiated_by_user_id, value_date)
            VALUES (@id, @ref, 'Transfer', 'Posted', 100.0000, 'BDT', 'Unbalanced test', 'API', 'tester', CURRENT_DATE);", conn, tx);
        cmdTxn.Parameters.AddWithValue("id", txnId);
        cmdTxn.Parameters.AddWithValue("ref", $"TXN-UNBAL-{Guid.NewGuid():N}");
        await cmdTxn.ExecuteNonQueryAsync();

        // Leg 1: Debit 100.0000
        await using var cmdLeg1 = new NpgsqlCommand(@"
            INSERT INTO ledger_entries (id, transaction_id, account_id, direction, amount, currency, account_seq, balance_after)
            VALUES (@id, @txnId, @accId, 'Debit', 100.0000, 'BDT', 99991, 100.0000);", conn, tx);
        cmdLeg1.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmdLeg1.Parameters.AddWithValue("txnId", txnId);
        cmdLeg1.Parameters.AddWithValue("accId", acc1);
        await cmdLeg1.ExecuteNonQueryAsync();

        // Leg 2: Credit 90.0000 (UNBALANCED by 10.0000 BDT)
        await using var cmdLeg2 = new NpgsqlCommand(@"
            INSERT INTO ledger_entries (id, transaction_id, account_id, direction, amount, currency, account_seq, balance_after)
            VALUES (@id, @txnId, @accId, 'Credit', 90.0000, 'BDT', 99992, 90.0000);", conn, tx);
        cmdLeg2.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmdLeg2.Parameters.AddWithValue("txnId", txnId);
        cmdLeg2.Parameters.AddWithValue("accId", acc2);
        await cmdLeg2.ExecuteNonQueryAsync();

        // Committing the transaction triggers the DEFERRED constraint trigger check_transaction_balanced()
        var act = async () => await tx.CommitAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("23514");
        ex.Which.Message.Should().Contain("is unbalanced for currency BDT");
    }

    [Fact]
    public async Task Commit_SingleLegTransaction_ShouldFailWithConstraintTriggerException_ERRCODE_23514()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        await using var cmdAccount = new NpgsqlCommand("SELECT id FROM accounts LIMIT 1;", conn);
        var accId = (Guid)(await cmdAccount.ExecuteScalarAsync())!;

        await using var tx = await conn.BeginTransactionAsync();

        var txnId = Guid.CreateVersion7();
        await using var cmdTxn = new NpgsqlCommand(@"
            INSERT INTO transactions (id, reference, type, status, amount, currency, description, channel, initiated_by_user_id, value_date)
            VALUES (@id, @ref, 'Deposit', 'Posted', 50.0000, 'BDT', 'Single leg test', 'API', 'tester', CURRENT_DATE);", conn, tx);
        cmdTxn.Parameters.AddWithValue("id", txnId);
        cmdTxn.Parameters.AddWithValue("ref", $"TXN-SINGLE-{Guid.NewGuid():N}");
        await cmdTxn.ExecuteNonQueryAsync();

        // Insert ONLY 1 entry
        await using var cmdLeg1 = new NpgsqlCommand(@"
            INSERT INTO ledger_entries (id, transaction_id, account_id, direction, amount, currency, account_seq, balance_after)
            VALUES (@id, @txnId, @accId, 'Credit', 50.0000, 'BDT', 99993, 50.0000);", conn, tx);
        cmdLeg1.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmdLeg1.Parameters.AddWithValue("txnId", txnId);
        cmdLeg1.Parameters.AddWithValue("accId", accId);
        await cmdLeg1.ExecuteNonQueryAsync();

        var act = async () => await tx.CommitAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("23514");
        ex.Which.Message.Should().Contain("must have at least 2 ledger entries");
    }

    [Fact]
    public async Task Commit_BalancedJournal_ShouldSucceed()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        await using var cmdAccounts = new NpgsqlCommand("SELECT id FROM accounts LIMIT 2;", conn);
        await using var reader = await cmdAccounts.ExecuteReaderAsync();
        await reader.ReadAsync();
        var acc1 = reader.GetGuid(0);
        await reader.ReadAsync();
        var acc2 = reader.GetGuid(0);
        await reader.CloseAsync();

        await using var tx = await conn.BeginTransactionAsync();

        var txnId = Guid.CreateVersion7();
        await using var cmdTxn = new NpgsqlCommand(@"
            INSERT INTO transactions (id, reference, type, status, amount, currency, description, channel, initiated_by_user_id, value_date)
            VALUES (@id, @ref, 'Transfer', 'Posted', 250.0000, 'BDT', 'Balanced test', 'API', 'tester', CURRENT_DATE);", conn, tx);
        cmdTxn.Parameters.AddWithValue("id", txnId);
        cmdTxn.Parameters.AddWithValue("ref", $"TXN-BAL-{Guid.NewGuid():N}");
        await cmdTxn.ExecuteNonQueryAsync();

        // Leg 1: Debit 250.0000
        await using var cmdLeg1 = new NpgsqlCommand(@"
            INSERT INTO ledger_entries (id, transaction_id, account_id, direction, amount, currency, account_seq, balance_after)
            VALUES (@id, @txnId, @accId, 'Debit', 250.0000, 'BDT', 99994, 250.0000);", conn, tx);
        cmdLeg1.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmdLeg1.Parameters.AddWithValue("txnId", txnId);
        cmdLeg1.Parameters.AddWithValue("accId", acc1);
        await cmdLeg1.ExecuteNonQueryAsync();

        // Leg 2: Credit 250.0000 (BALANCED)
        await using var cmdLeg2 = new NpgsqlCommand(@"
            INSERT INTO ledger_entries (id, transaction_id, account_id, direction, amount, currency, account_seq, balance_after)
            VALUES (@id, @txnId, @accId, 'Credit', 250.0000, 'BDT', 99995, 250.0000);", conn, tx);
        cmdLeg2.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmdLeg2.Parameters.AddWithValue("txnId", txnId);
        cmdLeg2.Parameters.AddWithValue("accId", acc2);
        await cmdLeg2.ExecuteNonQueryAsync();

        // Must succeed without throwing
        var act = async () => await tx.CommitAsync();
        await act.Should().NotThrowAsync();
    }
}
