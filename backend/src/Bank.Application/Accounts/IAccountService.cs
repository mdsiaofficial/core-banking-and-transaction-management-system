using Bank.Application.Common;
using Bank.Domain.Enums;

namespace Bank.Application.Accounts;

public interface IAccountService
{
    Task<AccountDto> OpenAccountAsync(
        Guid customerId,
        AccountType productType,
        decimal overdraftLimit,
        decimal minBalance,
        decimal? dailyDebitLimit,
        CancellationToken ct = default);

    Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountDto?> GetAccountByNumberAsync(string accountNumber, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDto>> GetAccountsByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(string? status = null, int limit = 50, CancellationToken ct = default);
    Task<AccountBalanceDto?> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task FreezeAccountAsync(Guid accountId, string reason, CancellationToken ct = default);
    Task UnfreezeAccountAsync(Guid accountId, string reason, CancellationToken ct = default);
    Task CloseAccountAsync(Guid accountId, CancellationToken ct = default);

    Task<PagedResult<LedgerEntryDto>> GetAccountEntriesKeysetAsync(
        Guid accountId,
        string? cursor = null,
        int limit = 20,
        DateTimeOffset? from = null,
        DateTimeOffset? toDate = null,
        string? direction = null,
        CancellationToken ct = default);

    Task<byte[]> ExportStatementCsvAsync(
        Guid accountId,
        DateTimeOffset? from = null,
        DateTimeOffset? toDate = null,
        CancellationToken ct = default);
}
