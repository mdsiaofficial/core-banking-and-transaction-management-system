using Bank.Domain.Exceptions;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Tests;

public class AccountNumberTests
{
    private readonly AccountNumberGenerator _generator = new();

    [Fact]
    public void Generate_WithDefaultBranch_ShouldProduceValidThirteenDigitNumber()
    {
        string accNumberStr = _generator.Generate(1);

        accNumberStr.Should().HaveLength(13);
        accNumberStr.Should().StartWith("101");
        AccountNumber.ValidateLuhn(accNumberStr).Should().BeTrue();

        var accNumber = new AccountNumber(accNumberStr);
        accNumber.Value.Should().Be(accNumberStr);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(999999L)]
    [InlineData(999999999L)]
    public void Generate_AcrossSequences_ShouldAlwaysPassLuhnValidation(long sequence)
    {
        string accNumberStr = _generator.Generate(sequence, "205");

        accNumberStr.Should().HaveLength(13);
        accNumberStr.Should().StartWith("205");
        AccountNumber.ValidateLuhn(accNumberStr).Should().BeTrue();

        // Must construct without throwing
        var acc = new AccountNumber(accNumberStr);
        acc.Value.Should().Be(accNumberStr);
    }

    [Fact]
    public void Constructor_WithValidLuhn_ShouldInstantiate()
    {
        // 101000000001 + check digit
        string payload = "101000000001";
        char checkDigit = AccountNumber.ComputeLuhnCheckDigit(payload);
        string validNumber = payload + checkDigit;

        var acc = new AccountNumber(validNumber);

        acc.Value.Should().Be(validNumber);
        acc.ToString().Should().Be(validNumber);
        string implicitStr = acc;
        implicitStr.Should().Be(validNumber);
    }

    [Theory]
    [InlineData("1010000000010")] // Invalid check digit
    [InlineData("1010000000019")] // Invalid check digit
    [InlineData("1234567890123")] // Invalid Luhn
    public void Constructor_WithInvalidCheckDigit_ShouldThrowInvalidAccountNumberException(string invalidNumber)
    {
        // Guard if by chance 1234567890123 happened to be valid, compute an explicitly wrong one
        char correctCheck = AccountNumber.ComputeLuhnCheckDigit(invalidNumber[..12]);
        char wrongCheck = correctCheck == '9' ? '0' : (char)(correctCheck + 1);
        string corruptedNumber = invalidNumber[..12] + wrongCheck;

        var act = () => new AccountNumber(corruptedNumber);

        act.Should().Throw<InvalidAccountNumberException>()
            .WithMessage("*fails Luhn checksum validation*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12345")]
    [InlineData("123456789012")] // 12 digits
    [InlineData("12345678901234")] // 14 digits
    [InlineData("101000000001A")] // Contains letter
    [InlineData("101-000000-01")]
    public void Constructor_WithMalformedString_ShouldThrowInvalidAccountNumberException(string malformed)
    {
        var act = () => new AccountNumber(malformed);

        act.Should().Throw<InvalidAccountNumberException>()
            .WithMessage("*13 numeric digits*");
    }

    [Fact]
    public void FromPayload_WithTwelveDigits_ShouldAppendCorrectCheckDigit()
    {
        string payload = "101000042123";
        var acc = AccountNumber.FromPayload(payload);

        acc.Value.Should().StartWith(payload);
        acc.Value.Should().HaveLength(13);
        AccountNumber.ValidateLuhn(acc.Value).Should().BeTrue();
    }

    [Fact]
    public void ComputeLuhnCheckDigit_WithNonTwelveDigits_ShouldThrowArgumentException()
    {
        var actTooShort = () => AccountNumber.ComputeLuhnCheckDigit("12345");
        var actTooLong = () => AccountNumber.ComputeLuhnCheckDigit("1234567890123");
        var actNonDigits = () => AccountNumber.ComputeLuhnCheckDigit("10100000000A");

        actTooShort.Should().Throw<ArgumentException>();
        actTooLong.Should().Throw<ArgumentException>();
        actNonDigits.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generator_WithNegativeSequence_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => _generator.Generate(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("1234")]
    [InlineData("12A")]
    public void Generator_WithInvalidBranchCode_ShouldThrowArgumentException(string invalidBranch)
    {
        var act = () => _generator.Generate(1, invalidBranch);

        act.Should().Throw<ArgumentException>();
    }
}
