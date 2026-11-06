namespace Bank.Application.Idempotency;

public enum IdempotencyStatus
{
    Acquired,
    Replay,
    Conflict,
    PayloadMismatch
}

public record IdempotencyCheckResult(
    IdempotencyStatus Status,
    Guid RecordId,
    int? CachedStatusCode = null,
    string? CachedResponseBody = null);

public interface IIdempotencyService
{
    Task<IdempotencyCheckResult> CheckOrAcquireAsync(
        Guid userId,
        string key,
        string endpoint,
        string requestPayload,
        CancellationToken ct = default);

    Task CompleteAsync(
        Guid recordId,
        int statusCode,
        string responseBody,
        Guid? transactionId = null,
        CancellationToken ct = default);
}
