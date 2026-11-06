using System.Globalization;
using Bank.Domain.Exceptions;

namespace Bank.Domain.ValueObjects;

public readonly record struct Money : IComparable<Money>, IComparable
{
    public const string DefaultCurrency = "BDT";
    public const int DecimalPlaces = 4;

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new InvalidMoneyException("Currency must be a valid 3-character ISO code.");
        }

        Amount = Math.Round(amount, DecimalPlaces, MidpointRounding.ToEven);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public static Money operator +(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator -(Money m) => new(-m.Amount, m.Currency);

    public static Money operator *(Money m, decimal factor) =>
        new(m.Amount * factor, m.Currency);

    public static Money operator /(Money m, decimal divisor)
    {
        if (divisor == 0m)
            throw new DivideByZeroException("Cannot divide money by zero.");
        return new Money(m.Amount / divisor, m.Currency);
    }

    public static bool operator <(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount < b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount <= b.Amount;
    }

    public static bool operator >(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount > b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount >= b.Amount;
    }

    public void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new CurrencyMismatchException(Currency, other.Currency);
        }
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Money other) return CompareTo(other);
        throw new ArgumentException("Object must be of type Money", nameof(obj));
    }

    public override string ToString() =>
        $"{Amount.ToString("0.0000", CultureInfo.InvariantCulture)} {Currency}";

    public string ToAmountString() =>
        Amount.ToString("0.0000", CultureInfo.InvariantCulture);
}
