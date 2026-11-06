using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string AccountNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public AccountKind Kind { get; set; } = AccountKind.Customer;
    public AccountType ProductType { get; set; } = AccountType.Savings;
    public LedgerClass LedgerClass { get; set; } = LedgerClass.Liability;
    public string Currency { get; set; } = Money.DefaultCurrency;
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public decimal Balance { get; set; }
    public decimal HeldAmount { get; set; }
    public decimal OverdraftLimit { get; set; }
    public decimal MinBalance { get; set; }
    public decimal? DailyDebitLimit { get; set; }
    public long LastEntrySeq { get; set; }

    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public uint Version { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();

    public Money CurrentBalance => new(Balance, Currency);
    public Money GetBalance() => CurrentBalance;
    public Money CurrentHeldAmount => new(HeldAmount, Currency);
    public Money CurrentOverdraftLimit => new(OverdraftLimit, Currency);
    public Money CurrentMinBalance => new(MinBalance, Currency);
    public Money? CurrentDailyDebitLimit => DailyDebitLimit.HasValue ? new(DailyDebitLimit.Value, Currency) : null;
    public Money AvailableBalance => new((Balance + OverdraftLimit) - HeldAmount, Currency);

    public AccountNumber GetAccountNumber() => new(AccountNumber);

    public void Freeze()
    {
        if (Status == AccountStatus.Closed)
            throw new AccountClosedException("Cannot freeze a closed account.");

        Status = AccountStatus.Frozen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Unfreeze()
    {
        if (Status == AccountStatus.Closed)
            throw new AccountClosedException("Cannot unfreeze a closed account.");

        Status = AccountStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Balance != 0m)
            throw new DomainException($"Cannot close account with non-zero balance ({Balance} {Currency}).", "NON_ZERO_BALANCE");

        if (HeldAmount != 0m)
            throw new DomainException("Cannot close account with pending holds.", "PENDING_HOLDS");

        Status = AccountStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool CanDebit(Money amount, out string? reason)
    {
        amount.EnsureSameCurrency(new Money(0m, Currency));

        if (Status == AccountStatus.Closed)
        {
            reason = "Account is closed.";
            return false;
        }

        if (Status == AccountStatus.Frozen)
        {
            reason = "Account is frozen.";
            return false;
        }

        if (Kind == AccountKind.Customer)
        {
            decimal available = (Balance + OverdraftLimit) - HeldAmount;
            if (available - amount.Amount < MinBalance)
            {
                reason = $"Insufficient funds: available {available} {Currency}, required {amount.Amount + MinBalance} {Currency} (min balance {MinBalance}).";
                return false;
            }
        }

        reason = null;
        return true;
    }

    public bool CanCredit(Money amount, out string? reason)
    {
        amount.EnsureSameCurrency(new Money(0m, Currency));

        if (Status == AccountStatus.Closed)
        {
            reason = "Account is closed.";
            return false;
        }

        reason = null;
        return true;
    }

    public void ApplyEntry(EntryDirection direction, Money amount, long expectedSeq)
    {
        amount.EnsureSameCurrency(new Money(0m, Currency));

        if (amount.Amount <= 0m)
        {
            throw new InvalidMoneyException("Entry amount must be strictly positive.");
        }

        if (expectedSeq != LastEntrySeq + 1)
        {
            throw new DomainException(
                $"Account sequence violation: expected {LastEntrySeq + 1}, got {expectedSeq}.",
                "SEQUENCE_VIOLATION");
        }

        switch (LedgerClass)
        {
            case LedgerClass.Liability:
            case LedgerClass.Income:
            case LedgerClass.Equity:
                if (direction == EntryDirection.Credit)
                    Balance += amount.Amount;
                else
                    Balance -= amount.Amount;
                break;

            case LedgerClass.Asset:
            case LedgerClass.Expense:
                if (direction == EntryDirection.Debit)
                    Balance += amount.Amount;
                else
                    Balance -= amount.Amount;
                break;

            default:
                throw new DomainException($"Unsupported ledger class {LedgerClass}.", "UNSUPPORTED_LEDGER_CLASS");
        }

        // Enforce overdraft invariant for customer accounts
        if (Kind == AccountKind.Customer && Balance + OverdraftLimit < 0m)
        {
            throw new InsufficientFundsException(
                $"Transaction violates overdraft limit for account {AccountNumber}. Balance: {Balance}, Overdraft: {OverdraftLimit}.");
        }

        LastEntrySeq = expectedSeq;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
