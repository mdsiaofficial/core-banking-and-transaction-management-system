namespace Bank.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, COMPLETED
    public int? ResponseStatus { get; set; }
    public string? ResponseBodyJson { get; set; }
    public Guid? TransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
