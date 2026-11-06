using Bank.Domain.Enums;

namespace Bank.Application.Accounts;

public record AccountDto(
    Guid Id,
    string AccountNumber,
    Guid? CustomerId,
    string Kind,
    string ProductType,
    string LedgerClass,
    string Currency,
    string Status,
    decimal Balance,
    decimal HeldAmount,
    decimal OverdraftLimit,
    decimal MinBalance,
    decimal AvailableBalance,
    decimal? DailyDebitLimit,
    long LastEntrySeq,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public record AccountBalanceDto(
    Guid AccountId,
    string AccountNumber,
    decimal LedgerBalance,
    decimal AvailableBalance,
    decimal HeldAmount,
    decimal OverdraftLimit,
    string Currency,
    DateTimeOffset AsOf);

public record LedgerEntryDto(
    Guid Id,
    Guid TransactionId,
    Guid AccountId,
    string Direction,
    decimal Amount,
    string Currency,
    long AccountSeq,
    decimal BalanceAfter,
    DateTimeOffset PostedAt);

public record OpenAccountRequest(
    Guid? CustomerId,
    AccountType ProductType,
    decimal OverdraftLimit = 0m,
    decimal MinBalance = 0m,
    decimal? DailyDebitLimit = null);
