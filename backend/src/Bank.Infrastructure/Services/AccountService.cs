using System.Globalization;
using System.Text;
using Bank.Application.Accounts;
using Bank.Application.Common;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly BankDbContext _db;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        BankDbContext db,
        IAccountNumberGenerator accountNumberGenerator,
        ILogger<AccountService> logger)
    {
        _db = db;
        _accountNumberGenerator = accountNumberGenerator;
        _logger = logger;
    }

    public async Task<AccountDto> OpenAccountAsync(
        Guid customerId,
        AccountType productType,
        decimal overdraftLimit,
        decimal minBalance,
        decimal? dailyDebitLimit,
        CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct);
        if (customer is null)
        {
            throw new DomainException($"Customer with ID {customerId} not found.", "CUSTOMER_NOT_FOUND");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            throw new DomainException("Cannot open account for non-active customer.", "CUSTOMER_NOT_ACTIVE");
        }

        // Generate account sequence based on account count
        long count = await _db.Accounts.LongCountAsync(ct) + 1000;
        string accountNumber = _accountNumberGenerator.Generate(count, "101");

        // Validate product type rules
        if (productType == AccountType.Savings && overdraftLimit > 0m)
        {
            throw new DomainException("Savings accounts do not permit overdraft limits.", "OVERDRAFT_NOT_ALLOWED");
        }

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Kind = AccountKind.Customer,
            ProductType = productType,
            LedgerClass = LedgerClass.Liability,
            Currency = Money.DefaultCurrency,
            Status = AccountStatus.Active,
            Balance = 0.0000m,
            HeldAmount = 0.0000m,
            OverdraftLimit = overdraftLimit,
            MinBalance = minBalance,
            DailyDebitLimit = dailyDebitLimit,
            LastEntrySeq = 0,
            OpenedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Opened account {AccountNumber} for customer {CustomerId}", accountNumber, customerId);
        return MapToDto(account);
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        return account is null ? null : MapToDto(account);
    }

    public async Task<AccountDto?> GetAccountByNumberAsync(string accountNumber, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, ct);

        return account is null ? null : MapToDto(account);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(string? status = null, int limit = 50, CancellationToken ct = default)
    {
        var query = _db.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountStatus>(status, true, out var st))
        {
            query = query.Where(a => a.Status == st);
        }

        int clampedLimit = Math.Clamp(limit, 1, 100);
        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(ct);

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<AccountBalanceDto?> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        if (account is null) return null;

        return new AccountBalanceDto(
            account.Id,
            account.AccountNumber,
            account.Balance,
            (account.Balance + account.OverdraftLimit) - account.HeldAmount,
            account.HeldAmount,
            account.OverdraftLimit,
            account.Currency,
            DateTimeOffset.UtcNow);
    }

    public async Task FreezeAccountAsync(Guid accountId, string reason, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FindAsync(new object[] { accountId }, ct);
        if (account is null)
            throw new DomainException($"Account {accountId} not found.", "ACCOUNT_NOT_FOUND");

        account.Freeze();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Account {AccountNumber} frozen. Reason: {Reason}", account.AccountNumber, reason);
    }

    public async Task UnfreezeAccountAsync(Guid accountId, string reason, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FindAsync(new object[] { accountId }, ct);
        if (account is null)
            throw new DomainException($"Account {accountId} not found.", "ACCOUNT_NOT_FOUND");

        account.Unfreeze();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Account {AccountNumber} unfrozen. Reason: {Reason}", account.AccountNumber, reason);
    }

    public async Task CloseAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FindAsync(new object[] { accountId }, ct);
        if (account is null)
            throw new DomainException($"Account {accountId} not found.", "ACCOUNT_NOT_FOUND");

        account.Close();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Account {AccountNumber} closed successfully.", account.AccountNumber);
    }

    public async Task<PagedResult<LedgerEntryDto>> GetAccountEntriesKeysetAsync(
        Guid accountId,
        string? cursor = null,
        int limit = 20,
        DateTimeOffset? from = null,
        DateTimeOffset? toDate = null,
        string? direction = null,
        CancellationToken ct = default)
    {
        int clampedLimit = Math.Clamp(limit, 1, 100);

        var query = _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == accountId);

        if (from.HasValue)
            query = query.Where(e => e.PostedAt >= from.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.PostedAt <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<EntryDirection>(direction, true, out var dir))
            query = query.Where(e => e.Direction == dir);

        // Keyset cursor pagination: cursor is "{unix_ms}_{id}"
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (TryDecodeCursor(cursor, out DateTimeOffset cursorTime, out Guid cursorId))
            {
                query = query.Where(e => e.PostedAt < cursorTime || (e.PostedAt == cursorTime && e.Id < cursorId));
            }
        }

        var entries = await query
            .OrderByDescending(e => e.PostedAt)
            .ThenByDescending(e => e.Id)
            .Take(clampedLimit + 1)
            .ToListAsync(ct);

        bool hasMore = entries.Count > clampedLimit;
        var pagedEntries = entries.Take(clampedLimit).ToList();

        string? nextCursor = null;
        if (hasMore && pagedEntries.Count > 0)
        {
            var last = pagedEntries[^1];
            nextCursor = EncodeCursor(last.PostedAt, last.Id);
        }

        var dtos = pagedEntries.Select(e => new LedgerEntryDto(
            e.Id,
            e.TransactionId,
            e.AccountId,
            e.Direction.ToString(),
            e.Amount,
            e.Currency,
            e.AccountSeq,
            e.BalanceAfter,
            e.PostedAt)).ToList();

        return new PagedResult<LedgerEntryDto>(dtos, nextCursor, hasMore);
    }

    public async Task<byte[]> ExportStatementCsvAsync(
        Guid accountId,
        DateTimeOffset? from = null,
        DateTimeOffset? toDate = null,
        CancellationToken ct = default)
    {
        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null)
            throw new DomainException($"Account {accountId} not found.", "ACCOUNT_NOT_FOUND");

        var query = _db.LedgerEntries
            .AsNoTracking()
            .Include(e => e.Transaction)
            .Where(e => e.AccountId == accountId);

        if (from.HasValue)
            query = query.Where(e => e.PostedAt >= from.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.PostedAt <= toDate.Value);

        var entries = await query
            .OrderBy(e => e.AccountSeq)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Sequence,TransactionReference,Type,Direction,Amount,Currency,BalanceAfter,Description");

        foreach (var e in entries)
        {
            string ts = e.PostedAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            string reference = e.Transaction?.Reference ?? string.Empty;
            string type = e.Transaction?.Type.ToString() ?? string.Empty;
            string direction = e.Direction.ToString();
            string amount = e.Amount.ToString("0.0000", CultureInfo.InvariantCulture);
            string currency = e.Currency;
            string balanceAfter = e.BalanceAfter.ToString("0.0000", CultureInfo.InvariantCulture);
            string description = (e.Transaction?.Description ?? string.Empty).Replace("\"", "\"\"");

            sb.AppendLine(CultureInfo.InvariantCulture, $"{ts},{e.AccountSeq},{reference},{type},{direction},{amount},{currency},{balanceAfter},\"{description}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EncodeCursor(DateTimeOffset timestamp, Guid id)
    {
        string raw = $"{timestamp.ToUnixTimeMilliseconds()}_{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodeCursor(string cursor, out DateTimeOffset timestamp, out Guid id)
    {
        timestamp = default;
        id = default;

        try
        {
            byte[] bytes = Convert.FromBase64String(cursor);
            string decoded = Encoding.UTF8.GetString(bytes);
            string[] parts = decoded.Split('_');
            if (parts.Length == 2 &&
                long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ms) &&
                Guid.TryParse(parts[1], out id))
            {
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                return true;
            }
        }
        catch
        {
            // invalid cursor format
        }

        return false;
    }

    private static AccountDto MapToDto(Account a) => new(
        a.Id,
        a.AccountNumber,
        a.CustomerId,
        a.Kind.ToString(),
        a.ProductType.ToString(),
        a.LedgerClass.ToString(),
        a.Currency,
        a.Status.ToString(),
        a.Balance,
        a.HeldAmount,
        a.OverdraftLimit,
        a.MinBalance,
        (a.Balance + a.OverdraftLimit) - a.HeldAmount,
        a.DailyDebitLimit,
        a.LastEntrySeq,
        a.OpenedAt,
        a.ClosedAt);
}
