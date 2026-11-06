using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Services;

public interface ITransferPolicy
{
    void ValidateTransfer(
        Account sourceAccount,
        Account destinationAccount,
        Money amount,
        decimal todayTotalDebited = 0m);
}
