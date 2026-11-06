using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Services;

public record PostingInstruction(
    Account TargetAccount,
    EntryDirection Direction,
    Money Amount);

public class PostingService : IPostingService
{
    public LedgerTransaction Post(
        TransactionReference reference,
        TransactionType type,
        Money amount,
        string description,
        string channel,
        string initiatedByUserId,
        bool onBehalfOfAdmin,
        IReadOnlyList<PostingInstruction> instructions,
        Guid? idempotencyKeyId = null,
        Guid? reversesTransactionId = null,
        string? metadataJson = null,
        DateTimeOffset? postedAt = null,
        DateOnly? valueDate = null)
    {
        if (instructions.Count < 2)
        {
            throw new UnbalancedTransactionException(
                $"Double-entry posting requires at least 2 entries, but {instructions.Count} provided.");
        }

        DateTimeOffset effectivePostedAt = postedAt ?? DateTimeOffset.UtcNow;
        DateOnly effectiveValueDate = valueDate ?? DateOnly.FromDateTime(effectivePostedAt.UtcDateTime);

        var transaction = new LedgerTransaction
        {
            Id = Guid.CreateVersion7(),
            Reference = reference.Value,
            Type = type,
            Status = TransactionStatus.Posted,
            Amount = amount.Amount,
            Currency = amount.Currency,
            Description = description,
            Channel = channel,
            InitiatedByUserId = initiatedByUserId,
            OnBehalfOfAdmin = onBehalfOfAdmin,
            IdempotencyKeyId = idempotencyKeyId,
            ReversesTransactionId = reversesTransactionId,
            MetadataJson = metadataJson,
            PostedAt = effectivePostedAt,
            ValueDate = effectiveValueDate
        };

        decimal debits = 0m;
        decimal credits = 0m;

        foreach (var instruction in instructions)
        {
            var acc = instruction.TargetAccount;
            var entryAmount = instruction.Amount;

            if (entryAmount.Amount <= 0m)
            {
                throw new InvalidMoneyException("All ledger entry amounts must be strictly greater than zero.");
            }

            if (!string.Equals(entryAmount.Currency, amount.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new CurrencyMismatchException(amount.Currency, entryAmount.Currency);
            }

            if (!string.Equals(acc.Currency, entryAmount.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new CurrencyMismatchException(acc.Currency, entryAmount.Currency);
            }

            if (instruction.Direction == EntryDirection.Debit)
            {
                debits += entryAmount.Amount;
            }
            else
            {
                credits += entryAmount.Amount;
            }

            long nextSeq = acc.LastEntrySeq + 1;
            acc.ApplyEntry(instruction.Direction, entryAmount, nextSeq);

            var entry = new LedgerEntry
            {
                Id = Guid.CreateVersion7(),
                TransactionId = transaction.Id,
                AccountId = acc.Id,
                Direction = instruction.Direction,
                Amount = entryAmount.Amount,
                Currency = entryAmount.Currency,
                AccountSeq = nextSeq,
                BalanceAfter = acc.Balance,
                PostedAt = effectivePostedAt,
                Account = acc,
                Transaction = transaction
            };

            transaction.Entries.Add(entry);
        }

        if (debits != credits)
        {
            throw new UnbalancedTransactionException(
                $"Double-entry invariant violated: total debits ({debits} {amount.Currency}) != total credits ({credits} {amount.Currency}).");
        }

        transaction.Validate();
        return transaction;
    }
}
