namespace Bank.Domain.Enums;

public enum AccountKind
{
    Customer = 1,
    Internal = 2
}

public enum AccountType
{
    Savings = 1,
    Current = 2,
    Cash = 3,
    CustomerDeposits = 4,
    FeeIncome = 5,
    InterestExpense = 6,
    Suspense = 7
}

public enum LedgerClass
{
    Asset = 1,
    Liability = 2,
    Income = 3,
    Expense = 4,
    Equity = 5
}

public enum AccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum EntryDirection
{
    Debit = 1,
    Credit = 2
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Fee = 4,
    Reversal = 5,
    Adjustment = 6,
    Interest = 7
}

public enum TransactionStatus
{
    Posted = 1
}

public enum CustomerStatus
{
    PendingVerification = 1,
    Active = 2,
    Suspended = 3,
    Closed = 4
}

public enum KycStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum AuditOutcome
{
    Success = 1,
    Denied = 2,
    Failed = 3
}
