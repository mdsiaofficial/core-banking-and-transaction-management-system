using Bank.Api.IntegrationTests.Fixtures;
using Npgsql;

namespace Bank.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseCollection.Name)]
public class DatabaseTriggerImmutabilityTests
{
    private readonly PostgreSqlTestFixture _fixture;

    public DatabaseTriggerImmutabilityTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Update_LedgerEntries_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        // Find an existing ledger entry id from seed data
        await using var selectCmd = new NpgsqlCommand("SELECT id FROM ledger_entries LIMIT 1;", conn);
        var entryId = (Guid?)await selectCmd.ExecuteScalarAsync();
        entryId.Should().NotBeNull();

        // Attempting to UPDATE amount must throw PostgresException ERRCODE 55000
        await using var updateCmd = new NpgsqlCommand(
            "UPDATE ledger_entries SET amount = 999999.0000 WHERE id = @id;", conn);
        updateCmd.Parameters.AddWithValue("id", entryId!.Value);

        var act = async () => await updateCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table ledger_entries");
    }

    [Fact]
    public async Task Delete_LedgerEntries_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        await using var selectCmd = new NpgsqlCommand("SELECT id FROM ledger_entries LIMIT 1;", conn);
        var entryId = (Guid?)await selectCmd.ExecuteScalarAsync();
        entryId.Should().NotBeNull();

        await using var deleteCmd = new NpgsqlCommand(
            "DELETE FROM ledger_entries WHERE id = @id;", conn);
        deleteCmd.Parameters.AddWithValue("id", entryId!.Value);

        var act = async () => await deleteCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table ledger_entries");
    }

    [Fact]
    public async Task Update_Transactions_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        await using var selectCmd = new NpgsqlCommand("SELECT id FROM transactions LIMIT 1;", conn);
        var txnId = (Guid?)await selectCmd.ExecuteScalarAsync();
        txnId.Should().NotBeNull();

        await using var updateCmd = new NpgsqlCommand(
            "UPDATE transactions SET amount = 999999.0000 WHERE id = @id;", conn);
        updateCmd.Parameters.AddWithValue("id", txnId!.Value);

        var act = async () => await updateCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table transactions");
    }

    [Fact]
    public async Task Delete_Transactions_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        await using var selectCmd = new NpgsqlCommand("SELECT id FROM transactions LIMIT 1;", conn);
        var txnId = (Guid?)await selectCmd.ExecuteScalarAsync();
        txnId.Should().NotBeNull();

        await using var deleteCmd = new NpgsqlCommand(
            "DELETE FROM transactions WHERE id = @id;", conn);
        deleteCmd.Parameters.AddWithValue("id", txnId!.Value);

        var act = async () => await deleteCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table transactions");
    }

    [Fact]
    public async Task Update_AuditLogs_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        // Insert an audit log record first
        var logId = Guid.CreateVersion7();
        await using var insertCmd = new NpgsqlCommand(@"
            INSERT INTO audit_logs (id, action, entity_type, entity_id, outcome, occurred_at)
            VALUES (@id, 'TEST_ACTION', 'TestEntity', '123', 'Success', CURRENT_TIMESTAMP);", conn);
        insertCmd.Parameters.AddWithValue("id", logId);
        await insertCmd.ExecuteNonQueryAsync();

        // Attempting to UPDATE must throw PostgresException ERRCODE 55000
        await using var updateCmd = new NpgsqlCommand(
            "UPDATE audit_logs SET outcome = 'Failed' WHERE id = @id;", conn);
        updateCmd.Parameters.AddWithValue("id", logId);

        var act = async () => await updateCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table audit_logs");
    }

    [Fact]
    public async Task Delete_AuditLogs_ShouldThrowTriggerException_ERRCODE_55000()
    {
        await using var conn = _fixture.CreateConnection();
        await conn.OpenAsync();

        var logId = Guid.CreateVersion7();
        await using var insertCmd = new NpgsqlCommand(@"
            INSERT INTO audit_logs (id, action, entity_type, entity_id, outcome, occurred_at)
            VALUES (@id, 'TEST_ACTION_DELETE', 'TestEntity', '456', 'Success', CURRENT_TIMESTAMP);", conn);
        insertCmd.Parameters.AddWithValue("id", logId);
        await insertCmd.ExecuteNonQueryAsync();

        await using var deleteCmd = new NpgsqlCommand(
            "DELETE FROM audit_logs WHERE id = @id;", conn);
        deleteCmd.Parameters.AddWithValue("id", logId);

        var act = async () => await deleteCmd.ExecuteNonQueryAsync();

        var ex = await act.Should().ThrowAsync<PostgresException>();
        ex.Which.SqlState.Should().Be("55000");
        ex.Which.Message.Should().Contain("UPDATE and DELETE operations are not permitted on table audit_logs");
    }
}
