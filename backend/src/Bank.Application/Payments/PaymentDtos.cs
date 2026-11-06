using Bank.Application.Accounts;

namespace Bank.Application.Payments;

public record DepositRequest(
    Guid AccountId,
    decimal Amount,
    string Currency = "BDT",
    string Description = "Deposit",
    string Channel = "API");

public record WithdrawalRequest(
    Guid AccountId,
    decimal Amount,
    string Currency = "BDT",
    string Description = "Withdrawal",
    string Channel = "API");

public record TransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency = "BDT",
    string Description = "Transfer",
    string Channel = "API");

public record ReversalRequest(
    string Reason,
    string Channel = "ADMIN");

public record TransactionDto(
    Guid Id,
    string Reference,
    string Type,
    string Status,
    decimal Amount,
    string Currency,
    string Description,
    string Channel,
    string InitiatedByUserId,
    Guid? ReversesTransactionId,
    DateTimeOffset PostedAt,
    DateOnly ValueDate,
    IReadOnlyList<LedgerEntryDto> Entries);

public record TrialBalanceEntryDto(
    Guid AccountId,
    string AccountNumber,
    string Kind,
    string ProductType,
    string LedgerClass,
    decimal DebitBalance,
    decimal CreditBalance,
    string Currency);

public record TrialBalanceDto(
    DateTimeOffset AsOf,
    IReadOnlyList<TrialBalanceEntryDto> Accounts,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced);
