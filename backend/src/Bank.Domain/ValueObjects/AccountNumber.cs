using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

public readonly record struct AccountNumber
{
    public const int RequiredLength = 13;
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != RequiredLength || !value.All(char.IsDigit))
        {
            throw new InvalidAccountNumberException(
                $"Account number must be exactly {RequiredLength} numeric digits.");
        }

        if (!ValidateLuhn(value))
        {
            throw new InvalidAccountNumberException(
                $"Account number '{value}' fails Luhn checksum validation.");
        }

        Value = value;
    }

    public static bool ValidateLuhn(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits) || digits.Length != RequiredLength || !digits.All(char.IsDigit))
        {
            return false;
        }

        int sum = 0;
        bool alternate = false;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (alternate)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    public static char ComputeLuhnCheckDigit(string twelveDigits)
    {
        if (string.IsNullOrWhiteSpace(twelveDigits) || twelveDigits.Length != 12 || !twelveDigits.All(char.IsDigit))
        {
            throw new ArgumentException("Input must be exactly 12 numeric digits.", nameof(twelveDigits));
        }

        int sum = 0;
        bool alternate = true; // In 13 digits, the digit left of check digit is doubled

        for (int i = twelveDigits.Length - 1; i >= 0; i--)
        {
            int d = twelveDigits[i] - '0';
            if (alternate)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            alternate = !alternate;
        }

        int remainder = sum % 10;
        int checkDigit = (10 - remainder) % 10;
        return (char)('0' + checkDigit);
    }

    public static AccountNumber FromPayload(string twelveDigits)
    {
        char check = ComputeLuhnCheckDigit(twelveDigits);
        return new AccountNumber(twelveDigits + check);
    }

    public override string ToString() => Value;

    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;
}
