using System.Collections.Concurrent;
using System.Data;
using Bank.Application.Payments;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bank.ConcurrencyTests;

public class HighContentionTransferTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private IServiceProvider _serviceProvider = null!;

    public HighContentionTransferTests()
    {
        _container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("bank_concurrency_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        string connectionString = _container.GetConnectionString();
        await ApplyScriptsAsync(connectionString);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:SecretKey"] = "SuperSecretKeyForConcurrencyTestingMustBe32Bytes!",
                ["Jwt:Issuer"] = "https://bank.local",
                ["Jwt:Audience"] = "https://bank.local"
            })
            .Build();

        services.AddLogging(builder => builder.ClearProviders());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        // Explicitly register DbContext without retry strategy to support explicit user-initiated transactions
        services.AddDbContext<BankDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(BankDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _container.DisposeAsync();
    }

    [Fact]
    public async Task ConcurrentTransfers_100PlusParallelWorkers_ShouldHaveZeroDeadlocks_ZeroLostUpdates_ConservedBalance_And_GaplessSeq()
    {
        // 1. Seed 4 shared customer accounts with 50,000 BDT each
        const decimal initialBalancePerAccount = 50000.0000m;
        var accountIds = new List<Guid>();

        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<BankDbContext>();

            var customer = new Customer
            {
                Id = Guid.CreateVersion7(),
                UserId = Guid.CreateVersion7(),
                CustomerNo = "CUST-CONC-01",
                FullName = "Concurrency Test Customer",
                DateOfBirth = new DateOnly(1985, 1, 1),
                Phone = "+8801700000099",
                AddressJson = "{}",
                Status = CustomerStatus.Active,
                KycStatus = KycStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Customers.Add(customer);

            var gen = new AccountNumberGenerator();
            for (int i = 0; i < 4; i++)
            {
                var acc = new Account
                {
                    Id = Guid.CreateVersion7(),
                    AccountNumber = gen.Generate(2000 + i, "101"),
                    CustomerId = customer.Id,
                    Kind = AccountKind.Customer,
                    ProductType = AccountType.Savings,
                    LedgerClass = LedgerClass.Liability,
                    Currency = "BDT",
                    Status = AccountStatus.Active,
                    Balance = initialBalancePerAccount,
                    HeldAmount = 0m,
                    OverdraftLimit = 0m,
                    MinBalance = 0m,
                    DailyDebitLimit = 1_000_000.0000m,
                    LastEntrySeq = 0,
                    OpenedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Accounts.Add(acc);
                accountIds.Add(acc.Id);
            }

            await db.SaveChangesAsync();
        }

        Guid accA = accountIds[0];
        Guid accB = accountIds[1];
        Guid accC = accountIds[2];
        Guid accD = accountIds[3];

        // 2. Generate 120 concurrent transfer specifications across bidirectional cycles:
        //    (A -> B), (B -> A)
        //    (B -> C), (C -> B)
        //    (C -> D), (D -> C)
        //    (D -> A), (A -> D)
        //    (A -> C), (C -> A)
        //    (B -> D), (D -> B)
        var transferPairs = new (Guid Source, Guid Dest)[]
        {
            (accA, accB), (accB, accA),
            (accB, accC), (accC, accB),
            (accC, accD), (accD, accC),
            (accD, accA), (accA, accD),
            (accA, accC), (accC, accA),
            (accB, accD), (accD, accB)
        };

        const int rounds = 10; // 12 pairs * 10 = 120 concurrent transfers
        const decimal transferAmount = 15.0000m;
        var transferRequests = new List<(Guid Source, Guid Dest, decimal Amount, Guid Key)>();

        for (int r = 0; r < rounds; r++)
        {
            foreach (var pair in transferPairs)
            {
                transferRequests.Add((pair.Source, pair.Dest, transferAmount, Guid.NewGuid()));
            }
        }

        transferRequests.Count.Should().Be(120);

        // 3. High-contention parallel execution: start all 120 tasks simultaneously
        var readySignal = new ManualResetEventSlim(false);
        var exceptions = new ConcurrentBag<Exception>();
        var successCount = 0;

        var tasks = transferRequests.Select(req => Task.Run(async () =>
        {
            readySignal.Wait(); // Ensure all threads unleash at the exact same instant

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

                var transferReq = new TransferRequest(
                    SourceAccountId: req.Source,
                    DestinationAccountId: req.Dest,
                    Amount: req.Amount,
                    Currency: "BDT",
                    Description: $"Contention test transfer {req.Key}",
                    Channel: "API");

                await paymentService.ProcessTransferAsync(
                    transferReq,
                    userId: "concurrency-tester",
                    idempotencyKeyId: null);

                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // Unleash the concurrent workers!
        readySignal.Set();
        await Task.WhenAll(tasks);

        // 4. Assertions

        // Assertion A: Zero deadlocks & 100% success
        if (!exceptions.IsEmpty)
        {
            var deadlockExceptions = exceptions.Where(e =>
                e.ToString().Contains("40P01") || e.Message.Contains("deadlock")).ToList();
            deadlockExceptions.Should().BeEmpty(because: "ordered row locking must guarantee deadlock freedom");
        }

        exceptions.Should().BeEmpty();
        successCount.Should().Be(120);

        // Assertion B: Zero lost updates & conservation of total money
        using (var verifyScope = _serviceProvider.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var accountsAfter = await db.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .ToListAsync();

            decimal totalFinalBalance = accountsAfter.Sum(a => a.Balance);
            decimal totalInitialBalance = initialBalancePerAccount * 4;

            totalFinalBalance.Should().Be(totalInitialBalance,
                because: "total system balance must be strictly conserved across all transfers");

            // Verify each account's balance matches: Initial + Sum(Credits) - Sum(Debits)
            foreach (var acc in accountsAfter)
            {
                var entries = await db.LedgerEntries
                    .Where(e => e.AccountId == acc.Id)
                    .OrderBy(e => e.AccountSeq)
                    .ToListAsync();

                decimal totalCredits = entries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);
                decimal totalDebits = entries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
                decimal expectedBalance = initialBalancePerAccount + totalCredits - totalDebits;

                acc.Balance.Should().Be(expectedBalance,
                    because: $"account {acc.AccountNumber} balance must match computed ledger debits/credits with zero lost updates");

                // Assertion C: Gapless account_seq (1, 2, 3, ..., N)
                entries.Should().NotBeEmpty();
                entries.Count.Should().Be((int)acc.LastEntrySeq);

                long expectedSeq = 1;
                foreach (var entry in entries)
                {
                    entry.AccountSeq.Should().Be(expectedSeq,
                        because: $"sequence for account {acc.AccountNumber} must be strictly gapless and monotonic");
                    expectedSeq++;
                }
            }
        }
    }

    private static async Task ApplyScriptsAsync(string connectionString)
    {
        string baseDir = AppContext.BaseDirectory;
        string? scriptsDir = null;

        string dir = baseDir;
        for (int i = 0; i < 7; i++)
        {
            string candidate = Path.Combine(dir, "src", "Bank.Infrastructure", "Data", "Scripts");
            if (Directory.Exists(candidate))
            {
                scriptsDir = candidate;
                break;
            }
            candidate = Path.Combine(dir, "backend", "src", "Bank.Infrastructure", "Data", "Scripts");
            if (Directory.Exists(candidate))
            {
                scriptsDir = candidate;
                break;
            }
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }

        if (scriptsDir is null)
        {
            throw new InvalidOperationException("Could not locate Data/Scripts directory.");
        }

        var scripts = new[]
        {
            Path.Combine(scriptsDir, "001_schema.sql"),
            Path.Combine(scriptsDir, "002_triggers.sql"),
            Path.Combine(scriptsDir, "003_seed_chart_of_accounts.sql")
        };

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (var scriptPath in scripts)
        {
            string sql = await File.ReadAllTextAsync(scriptPath);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
