namespace Bank.Domain.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR") : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string message = "Account has insufficient available funds for this operation.")
        : base(message, "INSUFFICIENT_FUNDS") { }
}

public class AccountFrozenException : DomainException
{
    public AccountFrozenException(string message = "Account is frozen.")
        : base(message, "ACCOUNT_FROZEN") { }
}

public class AccountClosedException : DomainException
{
    public AccountClosedException(string message = "Account is closed.")
        : base(message, "ACCOUNT_CLOSED") { }
}

public class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string expected, string actual)
        : base($"Currency mismatch: expected {expected}, got {actual}.", "CURRENCY_MISMATCH") { }
}

public class DailyLimitExceededException : DomainException
{
    public DailyLimitExceededException(string message = "Daily debit limit exceeded.")
        : base(message, "DAILY_LIMIT_EXCEEDED") { }
}

public class InvalidAccountNumberException : DomainException
{
    public InvalidAccountNumberException(string message = "Invalid account number format or checksum.")
        : base(message, "INVALID_ACCOUNT_NUMBER") { }
}

public class InvalidMoneyException : DomainException
{
    public InvalidMoneyException(string message = "Invalid money amount.")
        : base(message, "INVALID_AMOUNT") { }
}

public class UnbalancedTransactionException : DomainException
{
    public UnbalancedTransactionException(string message = "Transaction entries are not balanced (sum of debits must equal sum of credits).")
        : base(message, "UNBALANCED_TRANSACTION") { }
}
