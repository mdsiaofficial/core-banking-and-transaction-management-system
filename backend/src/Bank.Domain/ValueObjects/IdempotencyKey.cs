using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

public readonly record struct IdempotencyKey
{
    public const int MinLength = 16;
    public const int MaxLength = 64;

    public string Value { get; }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < MinLength || value.Length > MaxLength)
        {
            throw new DomainException(
                $"Idempotency-Key must be between {MinLength} and {MaxLength} characters.",
                "INVALID_IDEMPOTENCY_KEY");
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(IdempotencyKey key) => key.Value;
}
