using Bank.Domain.Enums;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public EntryDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Money.DefaultCurrency;
    public long AccountSeq { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;

    public LedgerTransaction? Transaction { get; set; }
    public Account? Account { get; set; }

    public Money GetMoney() => new(Amount, Currency);
    public Money GetBalanceAfter() => new(BalanceAfter, Currency);
}
