using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bank.Application.Idempotency;
using Bank.Domain.Entities;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly BankDbContext _db;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(BankDbContext db, ILogger<IdempotencyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IdempotencyCheckResult> CheckOrAcquireAsync(
        Guid userId,
        string key,
        string endpoint,
        string requestPayload,
        CancellationToken ct = default)
    {
        string requestHash = ComputeHash($"{endpoint}:{requestPayload}");

        // Look for existing idempotency record
        var existing = await _db.IdempotencyKeys
            .FirstOrDefaultAsync(i => i.UserId == userId && i.Key == key, ct);

        if (existing is not null)
        {
            // If hash is different -> 422 Payload mismatch
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                _logger.LogWarning("Idempotency key {Key} for user {UserId} reused with differing payload", key, userId);
                return new IdempotencyCheckResult(IdempotencyStatus.PayloadMismatch, existing.Id);
            }

            // If still in progress -> 409 Conflict
            if (string.Equals(existing.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Concurrent request in progress for idempotency key {Key} and user {UserId}", key, userId);
                return new IdempotencyCheckResult(IdempotencyStatus.Conflict, existing.Id);
            }

            // If completed -> replay stored response
            if (string.Equals(existing.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Replaying cached response for idempotency key {Key} and user {UserId}", key, userId);
                return new IdempotencyCheckResult(
                    IdempotencyStatus.Replay,
                    existing.Id,
                    existing.ResponseStatus,
                    existing.ResponseBodyJson);
            }
        }

        // Acquire new idempotency key
        var newRecord = new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Key = key,
            Endpoint = endpoint,
            RequestHash = requestHash,
            Status = "IN_PROGRESS",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(48)
        };

        _db.IdempotencyKeys.Add(newRecord);
        await _db.SaveChangesAsync(ct);

        return new IdempotencyCheckResult(IdempotencyStatus.Acquired, newRecord.Id);
    }

    public async Task CompleteAsync(
        Guid recordId,
        int statusCode,
        string responseBody,
        Guid? transactionId = null,
        CancellationToken ct = default)
    {
        var record = await _db.IdempotencyKeys.FindAsync(new object[] { recordId }, ct);
        if (record is not null)
        {
            record.Status = "COMPLETED";
            record.ResponseStatus = statusCode;
            record.ResponseBodyJson = responseBody;
            record.TransactionId = transactionId;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static string ComputeHash(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
