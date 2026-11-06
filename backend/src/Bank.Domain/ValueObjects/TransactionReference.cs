using System.Security.Cryptography;
using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

public readonly record struct TransactionReference
{
    private static readonly char[] Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    public string Value { get; }

    public TransactionReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("TXN-", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Transaction reference must start with 'TXN-' prefix.", "INVALID_TRANSACTION_REFERENCE");
        }

        Value = value.ToUpperInvariant();
    }

    public static TransactionReference Generate(DateTimeOffset? timestamp = null)
    {
        DateTimeOffset ts = timestamp ?? DateTimeOffset.UtcNow;
        string datePart = ts.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        string randomPart = GenerateRandomBase36(6);
        return new TransactionReference($"TXN-{datePart}-{randomPart}");
    }

    private static string GenerateRandomBase36(int length)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(length);
        char[] result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = Base36Chars[bytes[i] % Base36Chars.Length];
        }
        return new string(result);
    }

    public override string ToString() => Value;

    public static implicit operator string(TransactionReference reference) => reference.Value;
}
