using System.Globalization;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Services;

public class AccountNumberGenerator : IAccountNumberGenerator
{
    private const string DefaultBranchCode = "101";

    public string Generate(long sequenceNumber, string branchCode = DefaultBranchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode) || branchCode.Length != 3 || !branchCode.All(char.IsDigit))
        {
            throw new ArgumentException("Branch code must be exactly 3 numeric digits.", nameof(branchCode));
        }

        if (sequenceNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "Sequence number must be non-negative.");
        }

        // 3-digit branch code + 9-digit sequence padded with 0s = 12 digits
        string seqStr = (sequenceNumber % 1_000_000_000).ToString("D9", CultureInfo.InvariantCulture);
        string twelveDigits = $"{branchCode}{seqStr}";

        char checkDigit = AccountNumber.ComputeLuhnCheckDigit(twelveDigits);
        return $"{twelveDigits}{checkDigit}";
    }
}
