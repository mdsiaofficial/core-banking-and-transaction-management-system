using System.Data;
using System.Text.Json;
using Bank.Application.Accounts;
using Bank.Application.Audit;
using Bank.Application.Payments;
using Bank.Domain.Common;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly BankDbContext _db;
    private readonly IPostingService _postingService;
    private readonly ITransferPolicy _transferPolicy;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        BankDbContext db,
        IPostingService postingService,
        ITransferPolicy transferPolicy,
        IAuditService auditService,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _postingService = postingService;
        _transferPolicy = transferPolicy;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TransactionDto> ProcessDepositAsync(
        DepositRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0m)
            throw new InvalidMoneyException("Deposit amount must be strictly greater than zero.");

        var money = new Money(request.Amount, request.Currency);

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            // Lock Cash Account and Target Account in deterministic order
            var orderedIds = new[] { SystemAccounts.CashAccountId, request.AccountId }.OrderBy(id => id).ToList();
            var lockedAccounts = await LockAccountsInOrderAsync(orderedIds, ct);

            var cashAccount = lockedAccounts.FirstOrDefault(a => a.Id == SystemAccounts.CashAccountId)
                ?? throw new DomainException("Cash vault account not found.", "CASH_ACCOUNT_NOT_FOUND");

            var targetAccount = lockedAccounts.FirstOrDefault(a => a.Id == request.AccountId)
                ?? throw new DomainException($"Account {request.AccountId} not found.", "ACCOUNT_NOT_FOUND");

            if (!targetAccount.CanCredit(money, out string? creditReason))
            {
                throw new DomainException(creditReason ?? "Account cannot receive deposits.", "ACCOUNT_CANNOT_CREDIT");
            }

            // Posting: Debit Cash (Asset increases), Credit Target Account (Liability increases)
            var instructions = new[]
            {
                new PostingInstruction(cashAccount, EntryDirection.Debit, money),
                new PostingInstruction(targetAccount, EntryDirection.Credit, money)
            };

            var reference = TransactionReference.Generate();
            var transaction = _postingService.Post(
                reference,
                TransactionType.Deposit,
                money,
                request.Description,
                request.Channel,
                userId,
                onBehalfOfAdmin: false,
                instructions,
                idempotencyKeyId: idempotencyKeyId);

            _db.Transactions.Add(transaction);

            // Outbox notification
            var outbox = new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "TransactionPosted",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    TransactionId = transaction.Id,
                    Reference = transaction.Reference,
                    Type = transaction.Type.ToString(),
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    AccountId = targetAccount.Id,
                    CustomerId = targetAccount.CustomerId
                }),
                OccurredAt = DateTimeOffset.UtcNow
            };
            _db.OutboxMessages.Add(outbox);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(new AuditLogEntry(
                ActorUserId: Guid.TryParse(userId, out var uid) ? uid : null,
                ActorRole: "Customer",
                ActorIp: null,
                UserAgent: null,
                CorrelationId: null,
                Action: "DEPOSIT_POSTED",
                EntityType: "Transaction",
                EntityId: transaction.Id.ToString(),
                Outcome: AuditOutcome.Success,
                Before: null,
                After: JsonSerializer.Serialize(new { AccountId = targetAccount.Id, NewBalance = targetAccount.Balance }),
                Metadata: JsonSerializer.Serialize(new { Reference = transaction.Reference, Amount = transaction.Amount, Currency = transaction.Currency })), ct);

            _logger.LogInformation("Deposit {Reference} of {Amount} {Currency} posted for account {AccountId}",
                transaction.Reference, transaction.Amount, transaction.Currency, targetAccount.Id);

            return MapToDto(transaction);
        });
    }

    public async Task<TransactionDto> ProcessWithdrawalAsync(
        WithdrawalRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0m)
            throw new InvalidMoneyException("Withdrawal amount must be strictly greater than zero.");

        var money = new Money(request.Amount, request.Currency);

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            var orderedIds = new[] { SystemAccounts.CashAccountId, request.AccountId }.OrderBy(id => id).ToList();
            var lockedAccounts = await LockAccountsInOrderAsync(orderedIds, ct);

            var cashAccount = lockedAccounts.FirstOrDefault(a => a.Id == SystemAccounts.CashAccountId)
                ?? throw new DomainException("Cash vault account not found.", "CASH_ACCOUNT_NOT_FOUND");

            var targetAccount = lockedAccounts.FirstOrDefault(a => a.Id == request.AccountId)
                ?? throw new DomainException($"Account {request.AccountId} not found.", "ACCOUNT_NOT_FOUND");

            if (!targetAccount.CanDebit(money, out string? debitReason))
            {
                if (targetAccount.Status == AccountStatus.Frozen)
                    throw new AccountFrozenException($"Account {targetAccount.AccountNumber} is frozen.");
                if (targetAccount.Status == AccountStatus.Closed)
                    throw new AccountClosedException($"Account {targetAccount.AccountNumber} is closed.");

                throw new InsufficientFundsException(debitReason ?? "Insufficient funds for withdrawal.");
            }

            // Daily limit check
            if (targetAccount.DailyDebitLimit.HasValue)
            {
                var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
                decimal todayDebits = await _db.LedgerEntries
                    .Where(e => e.AccountId == targetAccount.Id && e.Direction == EntryDirection.Debit && e.PostedAt >= today)
                    .SumAsync(e => e.Amount, ct);

                if (todayDebits + request.Amount > targetAccount.DailyDebitLimit.Value)
                {
                    throw new DailyLimitExceededException(
                        $"Daily limit of {targetAccount.DailyDebitLimit.Value} {targetAccount.Currency} exceeded.");
                }
            }

            // Posting: Debit Target Account (Liability decreases), Credit Cash (Asset decreases)
            var instructions = new[]
            {
                new PostingInstruction(targetAccount, EntryDirection.Debit, money),
                new PostingInstruction(cashAccount, EntryDirection.Credit, money)
            };

            var reference = TransactionReference.Generate();
            var transaction = _postingService.Post(
                reference,
                TransactionType.Withdrawal,
                money,
                request.Description,
                request.Channel,
                userId,
                onBehalfOfAdmin: false,
                instructions,
                idempotencyKeyId: idempotencyKeyId);

            _db.Transactions.Add(transaction);

            var outbox = new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "TransactionPosted",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    TransactionId = transaction.Id,
                    Reference = transaction.Reference,
                    Type = transaction.Type.ToString(),
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    AccountId = targetAccount.Id,
                    CustomerId = targetAccount.CustomerId
                }),
                OccurredAt = DateTimeOffset.UtcNow
            };
            _db.OutboxMessages.Add(outbox);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(new AuditLogEntry(
                ActorUserId: Guid.TryParse(userId, out var uid) ? uid : null,
                ActorRole: "Customer",
                ActorIp: null,
                UserAgent: null,
                CorrelationId: null,
                Action: "WITHDRAWAL_POSTED",
                EntityType: "Transaction",
                EntityId: transaction.Id.ToString(),
                Outcome: AuditOutcome.Success,
                Before: null,
                After: JsonSerializer.Serialize(new { AccountId = targetAccount.Id, NewBalance = targetAccount.Balance }),
                Metadata: JsonSerializer.Serialize(new { Reference = transaction.Reference, Amount = transaction.Amount, Currency = transaction.Currency })), ct);

            _logger.LogInformation("Withdrawal {Reference} of {Amount} {Currency} posted for account {AccountId}",
                transaction.Reference, transaction.Amount, transaction.Currency, targetAccount.Id);

            return MapToDto(transaction);
        });
    }

    public async Task<TransactionDto> ProcessTransferAsync(
        TransferRequest request,
        string userId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new DomainException("Source and destination accounts cannot be identical.", "SAME_ACCOUNT_TRANSFER");

        if (request.Amount <= 0m)
            throw new InvalidMoneyException("Transfer amount must be strictly greater than zero.");

        var money = new Money(request.Amount, request.Currency);

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            // Ordered row-locking: deterministic account ID ordering prevents deadlocks
            var orderedIds = new[] { request.SourceAccountId, request.DestinationAccountId }.OrderBy(id => id).ToList();
            var lockedAccounts = await LockAccountsInOrderAsync(orderedIds, ct);

            var sourceAccount = lockedAccounts.FirstOrDefault(a => a.Id == request.SourceAccountId)
                ?? throw new DomainException($"Source account {request.SourceAccountId} not found.", "SOURCE_ACCOUNT_NOT_FOUND");

            var destinationAccount = lockedAccounts.FirstOrDefault(a => a.Id == request.DestinationAccountId)
                ?? throw new DomainException($"Destination account {request.DestinationAccountId} not found.", "DESTINATION_ACCOUNT_NOT_FOUND");

            // Compute today's debits for daily limit validation
            decimal todayDebited = 0m;
            if (sourceAccount.DailyDebitLimit.HasValue)
            {
                var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
                todayDebited = await _db.LedgerEntries
                    .Where(e => e.AccountId == sourceAccount.Id && e.Direction == EntryDirection.Debit && e.PostedAt >= today)
                    .SumAsync(e => e.Amount, ct);
            }

            // Validate transfer policy
            _transferPolicy.ValidateTransfer(sourceAccount, destinationAccount, money, todayDebited);

            // Posting: Debit Source, Credit Destination
            var instructions = new[]
            {
                new PostingInstruction(sourceAccount, EntryDirection.Debit, money),
                new PostingInstruction(destinationAccount, EntryDirection.Credit, money)
            };

            var reference = TransactionReference.Generate();
            var transaction = _postingService.Post(
                reference,
                TransactionType.Transfer,
                money,
                request.Description,
                request.Channel,
                userId,
                onBehalfOfAdmin: false,
                instructions,
                idempotencyKeyId: idempotencyKeyId);

            _db.Transactions.Add(transaction);

            var outbox = new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "TransactionPosted",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    TransactionId = transaction.Id,
                    Reference = transaction.Reference,
                    Type = transaction.Type.ToString(),
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    SourceAccountId = sourceAccount.Id,
                    DestinationAccountId = destinationAccount.Id
                }),
                OccurredAt = DateTimeOffset.UtcNow
            };
            _db.OutboxMessages.Add(outbox);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(new AuditLogEntry(
                ActorUserId: Guid.TryParse(userId, out var uid) ? uid : null,
                ActorRole: "Customer",
                ActorIp: null,
                UserAgent: null,
                CorrelationId: null,
                Action: "TRANSFER_POSTED",
                EntityType: "Transaction",
                EntityId: transaction.Id.ToString(),
                Outcome: AuditOutcome.Success,
                Before: null,
                After: JsonSerializer.Serialize(new
                {
                    SourceAccountId = sourceAccount.Id,
                    SourceBalance = sourceAccount.Balance,
                    DestinationAccountId = destinationAccount.Id,
                    DestinationBalance = destinationAccount.Balance
                }),
                Metadata: JsonSerializer.Serialize(new
                {
                    Reference = transaction.Reference,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency
                })), ct);

            _logger.LogInformation("Transfer {Reference} of {Amount} {Currency} from {Source} to {Destination} posted",
                transaction.Reference, transaction.Amount, transaction.Currency, sourceAccount.Id, destinationAccount.Id);

            return MapToDto(transaction);
        });
    }

    public async Task<TransactionDto> ProcessReversalAsync(
        Guid transactionId,
        ReversalRequest request,
        string adminUserId,
        Guid? idempotencyKeyId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new DomainException("Reversal reason is required.", "REVERSAL_REASON_REQUIRED");

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            // Fetch original transaction with its entries
            var originalTxn = await _db.Transactions
                .Include(t => t.Entries)
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

            if (originalTxn is null)
                throw new DomainException($"Transaction {transactionId} not found.", "TRANSACTION_NOT_FOUND");

            if (originalTxn.Type == TransactionType.Reversal)
                throw new DomainException("Cannot reverse a reversal transaction.", "CANNOT_REVERSE_REVERSAL");

            // Verify not already reversed
            bool alreadyReversed = await _db.Transactions
                .AnyAsync(t => t.ReversesTransactionId == transactionId, ct);

            if (alreadyReversed)
                throw new DomainException($"Transaction {transactionId} has already been reversed.", "TRANSACTION_ALREADY_REVERSED");

            // Identify and order accounts involved
            var accountIds = originalTxn.Entries.Select(e => e.AccountId).Distinct().OrderBy(id => id).ToList();
            var lockedAccounts = await LockAccountsInOrderAsync(accountIds, ct);

            var accountMap = lockedAccounts.ToDictionary(a => a.Id);

            // Build opposite compensating posting instructions
            var instructions = new List<PostingInstruction>();
            foreach (var entry in originalTxn.Entries)
            {
                if (!accountMap.TryGetValue(entry.AccountId, out var acc))
                {
                    throw new DomainException($"Account {entry.AccountId} involved in original transaction not found.", "ACCOUNT_NOT_FOUND");
                }

                // Invert direction: if Debit -> Credit; if Credit -> Debit
                var oppositeDir = entry.Direction == EntryDirection.Debit ? EntryDirection.Credit : EntryDirection.Debit;
                instructions.Add(new PostingInstruction(acc, oppositeDir, new Money(entry.Amount, entry.Currency)));
            }

            var reversalReference = TransactionReference.Generate();
            var money = new Money(originalTxn.Amount, originalTxn.Currency);

            var reversalTxn = _postingService.Post(
                reversalReference,
                TransactionType.Reversal,
                money,
                $"Reversal of {originalTxn.Reference}: {request.Reason}",
                request.Channel,
                adminUserId,
                onBehalfOfAdmin: true,
                instructions,
                idempotencyKeyId: idempotencyKeyId,
                reversesTransactionId: originalTxn.Id);

            _db.Transactions.Add(reversalTxn);

            var outbox = new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "TransactionReversed",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    OriginalTransactionId = originalTxn.Id,
                    ReversalTransactionId = reversalTxn.Id,
                    Reason = request.Reason
                }),
                OccurredAt = DateTimeOffset.UtcNow
            };
            _db.OutboxMessages.Add(outbox);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await _auditService.LogAsync(new AuditLogEntry(
                ActorUserId: Guid.TryParse(adminUserId, out var uid) ? uid : null,
                ActorRole: "Admin",
                ActorIp: null,
                UserAgent: null,
                CorrelationId: null,
                Action: "TRANSACTION_REVERSED",
                EntityType: "Transaction",
                EntityId: reversalTxn.Id.ToString(),
                Outcome: AuditOutcome.Success,
                Before: JsonSerializer.Serialize(new { OriginalTransactionId = originalTxn.Id, OriginalReference = originalTxn.Reference }),
                After: JsonSerializer.Serialize(new { ReversalTransactionId = reversalTxn.Id, ReversalReference = reversalTxn.Reference }),
                Metadata: JsonSerializer.Serialize(new { Reason = request.Reason })), ct);

            _logger.LogInformation("Reversal {Reference} posted for transaction {OriginalReference} by admin {AdminId}",
                reversalTxn.Reference, originalTxn.Reference, adminUserId);

            return MapToDto(reversalTxn);
        });
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, CancellationToken ct = default)
    {
        var txn = await _db.Transactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        return txn is null ? null : MapToDto(txn);
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(CancellationToken ct = default)
    {
        var accounts = await _db.Accounts.AsNoTracking().ToListAsync(ct);

        decimal totalDebits = 0m;
        decimal totalCredits = 0m;
        var entries = new List<TrialBalanceEntryDto>();

        foreach (var acc in accounts)
        {
            decimal debitBalance = 0m;
            decimal creditBalance = 0m;

            if (acc.LedgerClass == LedgerClass.Asset || acc.LedgerClass == LedgerClass.Expense)
            {
                if (acc.Balance >= 0m)
                    debitBalance = acc.Balance;
                else
                    creditBalance = -acc.Balance;
            }
            else
            {
                if (acc.Balance >= 0m)
                    creditBalance = acc.Balance;
                else
                    debitBalance = -acc.Balance;
            }

            totalDebits += debitBalance;
            totalCredits += creditBalance;

            entries.Add(new TrialBalanceEntryDto(
                acc.Id,
                acc.AccountNumber,
                acc.Kind.ToString(),
                acc.ProductType.ToString(),
                acc.LedgerClass.ToString(),
                debitBalance,
                creditBalance,
                acc.Currency));
        }

        bool isBalanced = totalDebits == totalCredits;
        return new TrialBalanceDto(DateTimeOffset.UtcNow, entries, totalDebits, totalCredits, isBalanced);
    }

    private async Task<List<Account>> LockAccountsInOrderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct)
    {
        // Enforce deterministic row locking order using raw SQL FOR UPDATE
        var accounts = new List<Account>();
        foreach (var id in orderedIds)
        {
            var acc = await _db.Accounts
                .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {id} FOR UPDATE")
                .FirstOrDefaultAsync(ct);

            if (acc != null)
            {
                accounts.Add(acc);
            }
        }
        return accounts;
    }

    private static TransactionDto MapToDto(LedgerTransaction t) => new(
        t.Id,
        t.Reference,
        t.Type.ToString(),
        t.Status.ToString(),
        t.Amount,
        t.Currency,
        t.Description,
        t.Channel,
        t.InitiatedByUserId,
        t.ReversesTransactionId,
        t.PostedAt,
        t.ValueDate,
        t.Entries.Select(e => new LedgerEntryDto(
            e.Id,
            e.TransactionId,
            e.AccountId,
            e.Direction.ToString(),
            e.Amount,
            e.Currency,
            e.AccountSeq,
            e.BalanceAfter,
            e.PostedAt)).ToList());
}
