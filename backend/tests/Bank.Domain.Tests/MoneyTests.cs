using System.Globalization;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithDefaultCurrency_ShouldSetAmountAndCurrencyBDT()
    {
        var money = new Money(150.25m);

        money.Amount.Should().Be(150.2500m);
        money.Currency.Should().Be("BDT");
        money.IsZero.Should().BeFalse();
        money.IsPositive.Should().BeTrue();
        money.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomCurrency_ShouldNormalizeToUppercase()
    {
        var money = new Money(100m, "usd");

        money.Amount.Should().Be(100.0000m);
        money.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("12")]
    public void Constructor_WithInvalidCurrency_ShouldThrowInvalidMoneyException(string invalidCurrency)
    {
        var act = () => new Money(100m, invalidCurrency);

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("*3-character ISO code*");
    }

    [Fact]
    public void Constructor_ShouldRoundToFourDecimalPlacesWithMidpointRoundingToEven()
    {
        // Midpoint rounding to even (Banker's rounding)
        var m1 = new Money(10.12344m);
        var m2 = new Money(10.12346m);
        var m3 = new Money(10.12345m); // 4 is even -> rounds to 10.1234
        var m4 = new Money(10.12355m); // 5 is odd -> rounds to 10.1236

        m1.Amount.Should().Be(10.1234m);
        m2.Amount.Should().Be(10.1235m);
        m3.Amount.Should().Be(10.1234m);
        m4.Amount.Should().Be(10.1236m);
    }

    [Fact]
    public void Zero_ShouldReturnZeroMoneyWithSpecifiedCurrency()
    {
        var zeroBdt = Money.Zero();
        var zeroEur = Money.Zero("EUR");

        zeroBdt.Amount.Should().Be(0m);
        zeroBdt.Currency.Should().Be("BDT");
        zeroBdt.IsZero.Should().BeTrue();
        zeroBdt.IsPositive.Should().BeFalse();
        zeroBdt.IsNegative.Should().BeFalse();

        zeroEur.Amount.Should().Be(0m);
        zeroEur.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Addition_WithSameCurrency_ShouldAddAmountsAccurately()
    {
        var a = new Money(250.5000m, "BDT");
        var b = new Money(149.5000m, "BDT");

        var result = a + b;

        result.Amount.Should().Be(400.0000m);
        result.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ShouldSubtractAmountsAccurately()
    {
        var a = new Money(500.7500m, "BDT");
        var b = new Money(200.2500m, "BDT");

        var result = a - b;

        result.Amount.Should().Be(300.5000m);
        result.Currency.Should().Be("BDT");
    }

    [Fact]
    public void UnaryNegation_ShouldInvertSign()
    {
        var positive = new Money(100m, "BDT");
        var negative = -positive;

        negative.Amount.Should().Be(-100.0000m);
        negative.IsNegative.Should().BeTrue();
        negative.IsPositive.Should().BeFalse();
    }

    [Fact]
    public void Multiplication_ShouldScaleAmountCorrectly()
    {
        var money = new Money(12.5000m, "BDT");
        var result = money * 3.5m;

        result.Amount.Should().Be(43.7500m);
        result.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Division_ShouldDivideAmountCorrectly()
    {
        var money = new Money(100.0000m, "BDT");
        var result = money / 4m;

        result.Amount.Should().Be(25.0000m);
        result.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Division_ByZero_ShouldThrowDivideByZeroException()
    {
        var money = new Money(100m, "BDT");

        var act = () => money / 0m;

        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void CurrencyMismatch_OnAddition_ShouldThrowCurrencyMismatchException()
    {
        var bdt = new Money(100m, "BDT");
        var usd = new Money(100m, "USD");

        var act = () => _ = bdt + usd;

        act.Should().Throw<CurrencyMismatchException>()
            .Which.ErrorCode.Should().Be("CURRENCY_MISMATCH");
    }

    [Fact]
    public void CurrencyMismatch_OnSubtraction_ShouldThrowCurrencyMismatchException()
    {
        var bdt = new Money(100m, "BDT");
        var eur = new Money(50m, "EUR");

        var act = () => _ = bdt - eur;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void ComparisonOperators_ShouldReflectAmountOrdering()
    {
        var smaller = new Money(50.0000m, "BDT");
        var larger = new Money(100.0000m, "BDT");
        var equal = new Money(50.0000m, "BDT");

        (smaller < larger).Should().BeTrue();
        (smaller <= larger).Should().BeTrue();
        (smaller <= equal).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (larger >= smaller).Should().BeTrue();
        (smaller >= equal).Should().BeTrue();

        smaller.CompareTo(larger).Should().BeNegative();
        larger.CompareTo(smaller).Should().BePositive();
        smaller.CompareTo(equal).Should().Be(0);
    }

    [Fact]
    public void Comparison_WithDifferentCurrencies_ShouldThrowCurrencyMismatchException()
    {
        var bdt = new Money(50m, "BDT");
        var usd = new Money(100m, "USD");

        var act = () => _ = bdt < usd;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void ToString_AndToAmountString_ShouldFormatWithFourDecimals()
    {
        var money = new Money(1234.5m, "BDT");

        money.ToString().Should().Be("1234.5000 BDT");
        money.ToAmountString().Should().Be("1234.5000");
    }
}
