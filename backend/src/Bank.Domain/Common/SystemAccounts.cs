namespace Bank.Domain.Common;

public static class SystemAccounts
{
    // 13-digit Luhn-compliant account numbers with leading chart-of-accounts codes:
    // Code 1000 -> 1000000000008
    public const string CashAccountNumber = "1000000000008";
    public const string CashCode = "1000";

    // Code 2000 -> 2000000000006
    public const string CustomerDepositsAccountNumber = "2000000000006";
    public const string CustomerDepositsCode = "2000";

    // Code 4000 -> 4000000000002
    public const string FeeIncomeAccountNumber = "4000000000002";
    public const string FeeIncomeCode = "4000";

    // Code 9000 -> 9000000000001
    public const string SuspenseAccountNumber = "9000000000001";
    public const string SuspenseCode = "9000";

    // Pre-allocated well-known GUIDs for system accounts so they are stable across environments
    public static readonly Guid CashAccountId = Guid.Parse("018f0000-0000-7000-8000-000000001000");
    public static readonly Guid CustomerDepositsAccountId = Guid.Parse("018f0000-0000-7000-8000-000000002000");
    public static readonly Guid FeeIncomeAccountId = Guid.Parse("018f0000-0000-7000-8000-000000004000");
    public static readonly Guid SuspenseAccountId = Guid.Parse("018f0000-0000-7000-8000-000000009000");
}
