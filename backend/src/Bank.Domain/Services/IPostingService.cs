using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Services;

public interface IPostingService
{
    LedgerTransaction Post(
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
        DateOnly? valueDate = null);
}
