using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;
using FsCheck.Xunit;

namespace Bank.Application.Tests;

public class LedgerInvariantPropertyTests
{
    private readonly PostingService _postingService = new();

    private static Account CreateCustomerAccount(string number, decimal initialBalance = 0m)
    {
        return new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = number,
            Kind = AccountKind.Customer,
            ProductType = AccountType.Savings,
            LedgerClass = LedgerClass.Liability,
            Currency = "BDT",
            Balance = initialBalance,
            OverdraftLimit = 0m,
            MinBalance = 0m,
            Status = AccountStatus.Active,
            LastEntrySeq = 0
        };
    }

    private static Account CreateCashPoolAccount()
    {
        return new Account
        {
            Id = Guid.CreateVersion7(),
            AccountNumber = "1010000000000",
            Kind = AccountKind.Internal,
            ProductType = AccountType.Cash,
            LedgerClass = LedgerClass.Asset,
            Currency = "BDT",
            Balance = 0m,
            Status = AccountStatus.Active,
            LastEntrySeq = 0
        };
    }

    [Property(MaxTest = 100)]
    public bool Invariant1_TotalMoneyConservation_AcrossRandomOperationSequences(
        int[] rawOpTypes,
        decimal[] rawAmounts,
        int[] fromIndices,
        int[] toIndices)
    {
        if (rawOpTypes == null || rawAmounts == null || fromIndices == null || toIndices == null)
            return true;

        int count = Math.Min(Math.Min(rawOpTypes.Length, rawAmounts.Length), Math.Min(fromIndices.Length, toIndices.Length));
        if (count == 0) return true;

        var cashPool = CreateCashPoolAccount();
        var customers = new[]
        {
            CreateCustomerAccount("1010000000018"),
            CreateCustomerAccount("1010000000026"),
            CreateCustomerAccount("1010000000034"),
            CreateCustomerAccount("1010000000042")
        };

        decimal totalDeposited = 0m;
        decimal totalWithdrawn = 0m;

        for (int i = 0; i < count; i++)
        {
            int opType = Math.Abs(rawOpTypes[i] % 3);
            decimal normalizedAmount = Math.Max(1.0000m, Math.Round(Math.Abs(rawAmounts[i]) % 10000m + 1m, 4));
            var amount = new Money(normalizedAmount, "BDT");
            int fromIdx = Math.Abs(fromIndices[i]) % customers.Length;
            int toIdx = Math.Abs(toIndices[i]) % customers.Length;

            switch (opType)
            {
                case 0: // Deposit: Cash Asset debited (+), Customer Liability credited (+)
                    _postingService.Post(
                        TransactionReference.Generate(),
                        TransactionType.Deposit,
                        amount,
                        "Random Deposit",
                        "ATM",
                        "system",
                        false,
                        new List<PostingInstruction>
                        {
                            new(cashPool, EntryDirection.Debit, amount),
                            new(customers[toIdx], EntryDirection.Credit, amount)
                        });
                    totalDeposited += amount.Amount;
                    break;

                case 1: // Withdrawal: Customer Liability debited (-), Cash Asset credited (-)
                    if (customers[fromIdx].Balance >= amount.Amount)
                    {
                        _postingService.Post(
                            TransactionReference.Generate(),
                            TransactionType.Withdrawal,
                            amount,
                            "Random Withdrawal",
                            "ATM",
                            "system",
                            false,
                            new List<PostingInstruction>
                            {
                                new(customers[fromIdx], EntryDirection.Debit, amount),
                                new(cashPool, EntryDirection.Credit, amount)
                            });
                        totalWithdrawn += amount.Amount;
                    }
                    break;

                case 2: // Transfer: Customer A debited (-), Customer B credited (+)
                    if (fromIdx != toIdx && customers[fromIdx].Balance >= amount.Amount)
                    {
                        _postingService.Post(
                            TransactionReference.Generate(),
                            TransactionType.Transfer,
                            amount,
                            "Random Transfer",
                            "ONLINE",
                            "system",
                            false,
                            new List<PostingInstruction>
                            {
                                new(customers[fromIdx], EntryDirection.Debit, amount),
                                new(customers[toIdx], EntryDirection.Credit, amount)
                            });
                    }
                    break;
            }

            // Invariant 1: Conservation check after each step:
            // Total customer balances == Cash pool balance == (totalDeposited - totalWithdrawn)
            decimal totalCustomerBalances = customers.Sum(c => c.Balance);
            if (totalCustomerBalances != cashPool.Balance)
            {
                return false;
            }

            if (totalCustomerBalances != (totalDeposited - totalWithdrawn))
            {
                return false;
            }

            // No customer balance may ever drop below overdraft limit (0)
            if (customers.Any(c => c.Balance < 0m))
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool Invariant2_EveryTransactionMaintainsDebitsEqualCredits(
        int[] rawOpTypes,
        decimal[] rawAmounts,
        int[] fromIndices,
        int[] toIndices)
    {
        if (rawOpTypes == null || rawAmounts == null || fromIndices == null || toIndices == null)
            return true;

        int count = Math.Min(Math.Min(rawOpTypes.Length, rawAmounts.Length), Math.Min(fromIndices.Length, toIndices.Length));
        if (count == 0) return true;

        var cashPool = CreateCashPoolAccount();
        var customers = new[]
        {
            CreateCustomerAccount("1010000000018"),
            CreateCustomerAccount("1010000000026")
        };

        var postedTransactions = new List<LedgerTransaction>();

        for (int i = 0; i < count; i++)
        {
            int opType = Math.Abs(rawOpTypes[i] % 2);
            decimal normalizedAmount = Math.Max(1.0000m, Math.Round(Math.Abs(rawAmounts[i]) % 5000m + 1m, 4));
            var amount = new Money(normalizedAmount, "BDT");
            int fromIdx = Math.Abs(fromIndices[i]) % customers.Length;
            int toIdx = Math.Abs(toIndices[i]) % customers.Length;

            if (opType == 0)
            {
                var txn = _postingService.Post(
                    TransactionReference.Generate(),
                    TransactionType.Deposit,
                    amount,
                    "Deposit",
                    "ATM",
                    "sys",
                    false,
                    new List<PostingInstruction>
                    {
                        new(cashPool, EntryDirection.Debit, amount),
                        new(customers[toIdx], EntryDirection.Credit, amount)
                    });
                postedTransactions.Add(txn);
            }
            else if (fromIdx != toIdx && customers[fromIdx].Balance >= amount.Amount)
            {
                var txn = _postingService.Post(
                    TransactionReference.Generate(),
                    TransactionType.Transfer,
                    amount,
                    "Transfer",
                    "WEB",
                    "sys",
                    false,
                    new List<PostingInstruction>
                    {
                        new(customers[fromIdx], EntryDirection.Debit, amount),
                        new(customers[toIdx], EntryDirection.Credit, amount)
                    });
                postedTransactions.Add(txn);
            }
        }

        // Invariant 2: Every single posted transaction must have >= 2 entries and debits == credits
        foreach (var txn in postedTransactions)
        {
            if (txn.Entries.Count < 2) return false;

            decimal debits = txn.Entries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
            decimal credits = txn.Entries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);

            if (debits != credits || debits != txn.Amount)
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool Invariant3_ReplayingAllLedgerEntriesReproducesExactStoredBalances(
        int[] rawOpTypes,
        decimal[] rawAmounts,
        int[] fromIndices,
        int[] toIndices)
    {
        if (rawOpTypes == null || rawAmounts == null || fromIndices == null || toIndices == null)
            return true;

        int count = Math.Min(Math.Min(rawOpTypes.Length, rawAmounts.Length), Math.Min(fromIndices.Length, toIndices.Length));
        if (count == 0) return true;

        var cashPool = CreateCashPoolAccount();
        var customers = new[]
        {
            CreateCustomerAccount("1010000000018"),
            CreateCustomerAccount("1010000000026"),
            CreateCustomerAccount("1010000000034")
        };

        var allEntries = new List<LedgerEntry>();

        for (int i = 0; i < count; i++)
        {
            int opType = Math.Abs(rawOpTypes[i] % 2);
            decimal normalizedAmount = Math.Max(1.0000m, Math.Round(Math.Abs(rawAmounts[i]) % 3000m + 1m, 4));
            var amount = new Money(normalizedAmount, "BDT");
            int fromIdx = Math.Abs(fromIndices[i]) % customers.Length;
            int toIdx = Math.Abs(toIndices[i]) % customers.Length;

            if (opType == 0)
            {
                var txn = _postingService.Post(
                    TransactionReference.Generate(),
                    TransactionType.Deposit,
                    amount,
                    "Deposit",
                    "ATM",
                    "sys",
                    false,
                    new List<PostingInstruction>
                    {
                        new(cashPool, EntryDirection.Debit, amount),
                        new(customers[toIdx], EntryDirection.Credit, amount)
                    });
                allEntries.AddRange(txn.Entries);
            }
            else if (fromIdx != toIdx && customers[fromIdx].Balance >= amount.Amount)
            {
                var txn = _postingService.Post(
                    TransactionReference.Generate(),
                    TransactionType.Transfer,
                    amount,
                    "Transfer",
                    "WEB",
                    "sys",
                    false,
                    new List<PostingInstruction>
                    {
                        new(customers[fromIdx], EntryDirection.Debit, amount),
                        new(customers[toIdx], EntryDirection.Credit, amount)
                    });
                allEntries.AddRange(txn.Entries);
            }
        }

        // Invariant 3: Replay ledger entries per customer account and verify exact match
        foreach (var customer in customers)
        {
            var customerEntries = allEntries
                .Where(e => e.AccountId == customer.Id)
                .OrderBy(e => e.AccountSeq)
                .ToList();

            decimal replayedBalance = 0m;
            long expectedSeq = 1;

            foreach (var entry in customerEntries)
            {
                if (entry.AccountSeq != expectedSeq)
                {
                    return false;
                }
                expectedSeq++;

                // For Customer accounts (Liability), Credit adds, Debit subtracts
                if (entry.Direction == EntryDirection.Credit)
                    replayedBalance += entry.Amount;
                else
                    replayedBalance -= entry.Amount;

                if (entry.BalanceAfter != replayedBalance)
                {
                    return false;
                }
            }

            if (replayedBalance != customer.Balance)
            {
                return false;
            }
        }

        // Replay cash pool (Asset: Debit adds, Credit subtracts)
        var cashEntries = allEntries
            .Where(e => e.AccountId == cashPool.Id)
            .OrderBy(e => e.AccountSeq)
            .ToList();

        decimal replayedCashBalance = 0m;
        foreach (var entry in cashEntries)
        {
            if (entry.Direction == EntryDirection.Debit)
                replayedCashBalance += entry.Amount;
            else
                replayedCashBalance -= entry.Amount;
        }

        return replayedCashBalance == cashPool.Balance;
    }
}
