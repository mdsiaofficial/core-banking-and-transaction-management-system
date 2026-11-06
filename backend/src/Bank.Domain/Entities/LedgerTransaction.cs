using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Entities;

public class LedgerTransaction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Reference { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Posted;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Money.DefaultCurrency;
    public string Description { get; set; } = string.Empty;
    public string Channel { get; set; } = "SYSTEM";
    public string InitiatedByUserId { get; set; } = string.Empty;
    public bool OnBehalfOfAdmin { get; set; }
    public Guid? IdempotencyKeyId { get; set; }
    public Guid? ReversesTransactionId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly ValueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();
    public LedgerTransaction? ReversesTransaction { get; set; }

    public Money GetMoney() => new(Amount, Currency);
    public TransactionReference GetReference() => new(Reference);

    public void Validate()
    {
        if (Entries.Count < 2)
        {
            throw new UnbalancedTransactionException(
                $"Transaction must contain at least 2 ledger entries, but has {Entries.Count}.");
        }

        decimal debits = 0m;
        decimal credits = 0m;

        foreach (var entry in Entries)
        {
            if (entry.Amount <= 0m)
            {
                throw new InvalidMoneyException(
                    $"Entry amount must be strictly positive. Found: {entry.Amount}.");
            }

            if (!string.Equals(entry.Currency, Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new CurrencyMismatchException(Currency, entry.Currency);
            }

            if (entry.Direction == EntryDirection.Debit)
            {
                debits += entry.Amount;
            }
            else if (entry.Direction == EntryDirection.Credit)
            {
                credits += entry.Amount;
            }
        }

        if (debits != credits)
        {
            throw new UnbalancedTransactionException(
                $"Transaction is unbalanced. Debits: {debits} {Currency}, Credits: {credits} {Currency}. Difference: {Math.Abs(debits - credits)}.");
        }
    }
}
