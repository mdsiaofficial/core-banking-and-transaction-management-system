using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Services;

public class TransferPolicy : ITransferPolicy
{
    public void ValidateTransfer(
        Account sourceAccount,
        Account destinationAccount,
        Money amount,
        decimal todayTotalDebited = 0m)
    {
        if (sourceAccount.Id == destinationAccount.Id)
        {
            throw new DomainException("Source and destination accounts cannot be identical.", "SAME_ACCOUNT_TRANSFER");
        }

        if (amount.Amount <= 0m)
        {
            throw new InvalidMoneyException("Transfer amount must be strictly positive.");
        }

        sourceAccount.GetBalance().EnsureSameCurrency(amount);
        destinationAccount.GetBalance().EnsureSameCurrency(amount);

        if (!sourceAccount.CanDebit(amount, out string? debitReason))
        {
            if (sourceAccount.Status == AccountStatus.Frozen)
                throw new AccountFrozenException($"Source account {sourceAccount.AccountNumber} is frozen.");
            if (sourceAccount.Status == AccountStatus.Closed)
                throw new AccountClosedException($"Source account {sourceAccount.AccountNumber} is closed.");

            throw new InsufficientFundsException(debitReason ?? "Insufficient funds in source account.");
        }

        if (!destinationAccount.CanCredit(amount, out string? creditReason))
        {
            if (destinationAccount.Status == AccountStatus.Closed)
                throw new AccountClosedException($"Destination account {destinationAccount.AccountNumber} is closed.");

            throw new DomainException(creditReason ?? "Destination account cannot accept credits.", "DESTINATION_CANNOT_CREDIT");
        }

        // Check daily debit limit if configured
        if (sourceAccount.DailyDebitLimit.HasValue)
        {
            decimal newDailyTotal = todayTotalDebited + amount.Amount;
            if (newDailyTotal > sourceAccount.DailyDebitLimit.Value)
            {
                throw new DailyLimitExceededException(
                    $"Daily debit limit of {sourceAccount.DailyDebitLimit.Value} {sourceAccount.Currency} exceeded. Attempted total: {newDailyTotal} {sourceAccount.Currency}.");
            }
        }
    }
}
