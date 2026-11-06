using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Bank.Domain.Common;
using Bank.Domain.Entities;
using Bank.Domain.Enums;
using Bank.Domain.Services;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Data;

public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task SeedDemoDataAsync(CancellationToken cancellationToken = default);
}

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly BankDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(
        BankDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<DatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying database migrations and integrity scripts...");

        // Ensure database is created or execute scripts
        string scriptsDir = Path.Combine(AppContext.BaseDirectory, "Data", "Scripts");
        if (!Directory.Exists(scriptsDir))
        {
            scriptsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Bank.Infrastructure", "Data", "Scripts"));
        }

        string[] scriptFiles = new[]
        {
            "001_schema.sql",
            "002_triggers.sql",
            "003_seed_chart_of_accounts.sql"
        };

        foreach (var file in scriptFiles)
        {
            string? foundPath = null;
            if (Directory.Exists(scriptsDir))
            {
                string p = Path.Combine(scriptsDir, file);
                if (File.Exists(p)) foundPath = p;
            }

            if (foundPath is null)
            {
                string dir = AppContext.BaseDirectory;
                for (int i = 0; i < 5; i++)
                {
                    string candidate = Path.Combine(dir, "backend", "src", "Bank.Infrastructure", "Data", "Scripts", file);
                    if (File.Exists(candidate))
                    {
                        foundPath = candidate;
                        break;
                    }
                    candidate = Path.Combine(dir, "src", "Bank.Infrastructure", "Data", "Scripts", file);
                    if (File.Exists(candidate))
                    {
                        foundPath = candidate;
                        break;
                    }
                    var parent = Directory.GetParent(dir);
                    if (parent is null) break;
                    dir = parent.FullName;
                }
            }

            if (foundPath != null && File.Exists(foundPath))
            {
                _logger.LogInformation("Executing migration script: {Script}", file);
                string sql = await File.ReadAllTextAsync(foundPath, cancellationToken);
                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("Script file {Script} not found at expected paths, ensuring created via EF Core", file);
                await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Database migration and integrity scripts applied successfully.");
    }

    public async Task SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding demo roles, users, and accounts...");

        // Ensure roles
        string[] roles = new[] { ApplicationRole.Admin, ApplicationRole.Customer, ApplicationRole.Auditor };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // Standard passwords
        const string standardPass = "Pass12345678!";
        const string legacyAdminPass = "Admin@Pass1234!";
        const string legacyAuditorPass = "Auditor@Pass1234!";
        const string legacyCustomerPass = "Customer@Pass1234!";

        // 1. Seed Multiple Admins
        var admin1 = await EnsureUserAsync("admin@apexbank.local", standardPass, ApplicationRole.Admin, null, cancellationToken);
        var admin2 = await EnsureUserAsync("admin@bank.local", legacyAdminPass, ApplicationRole.Admin, null, cancellationToken);
        var admin3 = await EnsureUserAsync("supervisor@apexbank.local", standardPass, ApplicationRole.Admin, null, cancellationToken);

        // 2. Seed Multiple Auditors
        var auditor1 = await EnsureUserAsync("auditor@apexbank.local", standardPass, ApplicationRole.Auditor, null, cancellationToken);
        var auditor2 = await EnsureUserAsync("auditor@bank.local", legacyAuditorPass, ApplicationRole.Auditor, null, cancellationToken);

        // Ensure System Cash Vault Account exists
        var cashAcc = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == SystemAccounts.CashAccountId, cancellationToken);
        if (cashAcc == null)
        {
            cashAcc = new Account
            {
                Id = SystemAccounts.CashAccountId,
                AccountNumber = "1000000000008",
                Kind = AccountKind.Internal,
                ProductType = AccountType.Cash,
                LedgerClass = LedgerClass.Asset,
                Currency = "BDT",
                Status = AccountStatus.Active,
                Balance = 100000000.0000m,
                LastEntrySeq = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Accounts.Add(cashAcc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Define 20 Diverse Customers & Accounts (Authentic Bangladeshi Names, Phones & Addresses)
        var customerDefinitions = new[]
        {
            new { Email = "customer@apexbank.local", Pass = standardPass, Name = "Ayesha Siddiqua", CustNo = "CUST-10001", Dob = new DateOnly(1992, 5, 14), Phone = "+8801711001234", Street = "Road 27, House 42, Dhanmondi", City = "Dhaka", Product = AccountType.Savings, Balance = 85000m, Overdraft = 0m, MinBal = 500m, Seq = 1001 },
            new { Email = "afroza.begum@bank.local", Pass = legacyCustomerPass, Name = "Afroza Begum", CustNo = "CUST-10002", Dob = new DateOnly(1990, 8, 20), Phone = "+8801712002345", Street = "House 15, Road 11, Gulshan-2", City = "Dhaka", Product = AccountType.Savings, Balance = 50000m, Overdraft = 0m, MinBal = 500m, Seq = 1002 },
            new { Email = "babul.mia@bank.local", Pass = legacyCustomerPass, Name = "Babul Mia", CustNo = "CUST-10003", Dob = new DateOnly(1985, 11, 23), Phone = "+8801811003456", Street = "45/A, Block C, Banani", City = "Dhaka", Product = AccountType.Current, Balance = 35000m, Overdraft = 10000m, MinBal = 0m, Seq = 1003 },
            new { Email = "rafiqul.islam@apexbank.local", Pass = standardPass, Name = "Md. Rafiqul Islam", CustNo = "CUST-10004", Dob = new DateOnly(1984, 3, 10), Phone = "+8801713004567", Street = "House 8, Road 4, Sector 7, Uttara", City = "Dhaka", Product = AccountType.Savings, Balance = 145000m, Overdraft = 0m, MinBal = 1000m, Seq = 1004 },
            new { Email = "tanvir.ahmed@apexbank.local", Pass = standardPass, Name = "Tanvir Ahmed", CustNo = "CUST-10005", Dob = new DateOnly(1988, 7, 22), Phone = "+8801911005678", Street = "Plot 64, Block D, Bashundhara R/A", City = "Dhaka", Product = AccountType.Current, Balance = 65000m, Overdraft = 20000m, MinBal = 0m, Seq = 1005 },
            new { Email = "jannat.ferdous@apexbank.local", Pass = standardPass, Name = "Jannatul Ferdous", CustNo = "CUST-10006", Dob = new DateOnly(1995, 9, 30), Phone = "+8801819006789", Street = "Building 12, Mirpur DOHS", City = "Dhaka", Product = AccountType.Savings, Balance = 32000m, Overdraft = 0m, MinBal = 500m, Seq = 1006 },
            new { Email = "farhana.yasmin@apexbank.local", Pass = standardPass, Name = "Farhana Yasmin", CustNo = "CUST-10007", Dob = new DateOnly(1993, 1, 15), Phone = "+8801300007890", Street = "Agrabad C/A, Commercial Area", City = "Chittagong", Product = AccountType.Current, Balance = 110000m, Overdraft = 25000m, MinBal = 0m, Seq = 1007 },
            new { Email = "mizanur.rahman@apexbank.local", Pass = standardPass, Name = "Kazi Mizanur Rahman", CustNo = "CUST-10008", Dob = new DateOnly(1981, 12, 5), Phone = "+8801714008901", Street = "House 23, Nasirabad Housing Society", City = "Chittagong", Product = AccountType.Savings, Balance = 78000m, Overdraft = 0m, MinBal = 500m, Seq = 1008 },
            new { Email = "nusrat.jahan@apexbank.local", Pass = standardPass, Name = "Nusrat Jahan", CustNo = "CUST-10009", Dob = new DateOnly(1996, 4, 18), Phone = "+8801912009012", Street = "14 Zindabazar Main Road", City = "Sylhet", Product = AccountType.Savings, Balance = 24500m, Overdraft = 0m, MinBal = 500m, Seq = 1009 },
            new { Email = "saiful.alam@apexbank.local", Pass = standardPass, Name = "Saiful Alam", CustNo = "CUST-10010", Dob = new DateOnly(1982, 8, 9), Phone = "+8801812001122", Street = "Block B, Upashahar", City = "Sylhet", Product = AccountType.Current, Balance = 180000m, Overdraft = 50000m, MinBal = 0m, Seq = 1010 },
            new { Email = "fahmida.akter@apexbank.local", Pass = standardPass, Name = "Fahmida Akter", CustNo = "CUST-10011", Dob = new DateOnly(1991, 6, 25), Phone = "+8801715002233", Street = "56 Shaheb Bazar", City = "Rajshahi", Product = AccountType.Savings, Balance = 53000m, Overdraft = 0m, MinBal = 500m, Seq = 1011 },
            new { Email = "kamal.hossain@apexbank.local", Pass = standardPass, Name = "Kamal Hossain", CustNo = "CUST-10012", Dob = new DateOnly(1979, 10, 12), Phone = "+8801400003344", Street = "88 Kazir Dewri", City = "Chittagong", Product = AccountType.Current, Balance = 92000m, Overdraft = 20000m, MinBal = 0m, Seq = 1012 },
            new { Email = "mehedi.hasan@apexbank.local", Pass = standardPass, Name = "Mehedi Hasan", CustNo = "CUST-10013", Dob = new DateOnly(1994, 2, 28), Phone = "+8801716004455", Street = "Road 6, Baridhara DOHS", City = "Dhaka", Product = AccountType.Savings, Balance = 125000m, Overdraft = 0m, MinBal = 1000m, Seq = 1013 },
            new { Email = "shamim.hossain@apexbank.local", Pass = standardPass, Name = "Shamim Hossain", CustNo = "CUST-10014", Dob = new DateOnly(1983, 3, 15), Phone = "+8801813005566", Street = "31 Motijheel C/A", City = "Dhaka", Product = AccountType.Current, Balance = 48000m, Overdraft = 15000m, MinBal = 0m, Seq = 1014 },
            new { Email = "rokeya.begum@apexbank.local", Pass = standardPass, Name = "Rokeya Begum", CustNo = "CUST-10015", Dob = new DateOnly(1976, 11, 4), Phone = "+8801552006677", Street = "Lane 3, Khulshi R/A", City = "Chittagong", Product = AccountType.Savings, Balance = 61000m, Overdraft = 0m, MinBal = 500m, Seq = 1015 },
            new { Email = "mahfuzur.rahman@apexbank.local", Pass = standardPass, Name = "Mahfuzur Rahman", CustNo = "CUST-10016", Dob = new DateOnly(1986, 9, 19), Phone = "+8801913007788", Street = "72 Chawkbazar Circular Rd", City = "Dhaka", Product = AccountType.Current, Balance = 74000m, Overdraft = 15000m, MinBal = 0m, Seq = 1016 },
            new { Email = "tasnim.sultana@apexbank.local", Pass = standardPass, Name = "Tasnim Sultana", CustNo = "CUST-10017", Dob = new DateOnly(1993, 7, 7), Phone = "+8801717008899", Street = "Block B, Lalmatia", City = "Dhaka", Product = AccountType.Savings, Balance = 105000m, Overdraft = 0m, MinBal = 500m, Seq = 1017 },
            new { Email = "shakil.khan@apexbank.local", Pass = standardPass, Name = "Shakil Khan", CustNo = "CUST-10018", Dob = new DateOnly(1989, 4, 1), Phone = "+8801814009900", Street = "43 Shantinagar", City = "Dhaka", Product = AccountType.Current, Balance = 38000m, Overdraft = 10000m, MinBal = 0m, Seq = 1018 },
            new { Email = "sajjad.hossain@apexbank.local", Pass = standardPass, Name = "Sajjad Hossain", CustNo = "CUST-10019", Dob = new DateOnly(1992, 5, 5), Phone = "+8801914001123", Street = "Road 4, Banani DOHS", City = "Dhaka", Product = AccountType.Savings, Balance = 160000m, Overdraft = 0m, MinBal = 1000m, Seq = 1019 },
            new { Email = "nazmul.huda@apexbank.local", Pass = standardPass, Name = "Nazmul Huda", CustNo = "CUST-10020", Dob = new DateOnly(1987, 12, 14), Phone = "+8801718002234", Street = "House 19, Block C, Sonadanga R/A", City = "Khulna", Product = AccountType.Savings, Balance = 91000m, Overdraft = 0m, MinBal = 500m, Seq = 1020 }
        };

        var accNumGen = new AccountNumberGenerator();
        var seededAccounts = new List<Account>();

        foreach (var def in customerDefinitions)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerNo == def.CustNo, cancellationToken);
            var user = await _userManager.FindByEmailAsync(def.Email);

            if (user is null)
            {
                var customerId = customer?.Id ?? Guid.CreateVersion7();
                user = new ApplicationUser
                {
                    Id = Guid.CreateVersion7(),
                    UserName = def.Email,
                    Email = def.Email,
                    EmailConfirmed = true,
                    CustomerId = customerId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                var userResult = await _userManager.CreateAsync(user, def.Pass);
                if (userResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, ApplicationRole.Customer);
                }
            }

            if (customer is null)
            {
                customer = new Customer
                {
                    Id = user.CustomerId ?? Guid.CreateVersion7(),
                    UserId = user.Id,
                    CustomerNo = def.CustNo,
                    FullName = def.Name,
                    DateOfBirth = def.Dob,
                    Phone = def.Phone,
                    AddressJson = JsonSerializer.Serialize(new { street = def.Street, city = def.City, country = "Bangladesh" }),
                    Status = CustomerStatus.Active,
                    KycStatus = KycStatus.Approved,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Customers.Add(customer);
                user.CustomerId = customer.Id;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Ensure Primary Account
            string accNum = accNumGen.Generate(def.Seq, "101");
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accNum, cancellationToken);
            if (account is null)
            {
                account = new Account
                {
                    Id = Guid.CreateVersion7(),
                    AccountNumber = accNum,
                    CustomerId = customer.Id,
                    Kind = AccountKind.Customer,
                    ProductType = def.Product,
                    LedgerClass = LedgerClass.Liability,
                    Currency = "BDT",
                    Status = AccountStatus.Active,
                    Balance = 0m, // will be credited via deposit transaction
                    HeldAmount = 0m,
                    OverdraftLimit = def.Overdraft,
                    MinBalance = def.MinBal,
                    DailyDebitLimit = 200000.0000m,
                    LastEntrySeq = 0,
                    OpenedAt = DateTimeOffset.UtcNow.AddDays(-30),
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
                };
                _dbContext.Accounts.Add(account);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Post Initial Deposit Transaction
                cashAcc.LastEntrySeq += 1;
                cashAcc.Balance += def.Balance;

                account.LastEntrySeq = 1;
                account.Balance = def.Balance;

                var initialTxn = new LedgerTransaction
                {
                    Id = Guid.CreateVersion7(),
                    Reference = TransactionReference.Generate().Value,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Posted,
                    Amount = def.Balance,
                    Currency = "BDT",
                    Description = $"Initial Opening Deposit for {def.Name}",
                    Channel = "SYSTEM",
                    InitiatedByUserId = admin1?.Id.ToString() ?? "SYSTEM",
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-30),
                    ValueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))
                };

                // Entry 1: Debit Cash Vault
                initialTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = initialTxn.Id,
                    AccountId = cashAcc.Id,
                    Direction = EntryDirection.Debit,
                    Amount = def.Balance,
                    Currency = "BDT",
                    AccountSeq = cashAcc.LastEntrySeq,
                    BalanceAfter = cashAcc.Balance,
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-30)
                });

                // Entry 2: Credit Customer Account
                initialTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = initialTxn.Id,
                    AccountId = account.Id,
                    Direction = EntryDirection.Credit,
                    Amount = def.Balance,
                    Currency = "BDT",
                    AccountSeq = account.LastEntrySeq,
                    BalanceAfter = account.Balance,
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-30)
                });

                _dbContext.Transactions.Add(initialTxn);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            seededAccounts.Add(account);
        }

        // 4. Seed Secondary Accounts (e.g. Md. Rafiqul Islam holds a second Current Account)
        var rafiq = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerNo == "CUST-10004", cancellationToken);
        if (rafiq != null)
        {
            string rafiqSecNum = accNumGen.Generate(1021, "102");
            var rafiqSecAcc = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == rafiqSecNum, cancellationToken);
            if (rafiqSecAcc == null)
            {
                rafiqSecAcc = new Account
                {
                    Id = Guid.CreateVersion7(),
                    AccountNumber = rafiqSecNum,
                    CustomerId = rafiq.Id,
                    Kind = AccountKind.Customer,
                    ProductType = AccountType.Current,
                    LedgerClass = LedgerClass.Liability,
                    Currency = "BDT",
                    Status = AccountStatus.Active,
                    Balance = 30000m,
                    HeldAmount = 0m,
                    OverdraftLimit = 20000m,
                    MinBalance = 0m,
                    DailyDebitLimit = 150000m,
                    LastEntrySeq = 1,
                    OpenedAt = DateTimeOffset.UtcNow.AddDays(-20),
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-20)
                };
                _dbContext.Accounts.Add(rafiqSecAcc);

                cashAcc.LastEntrySeq += 1;
                cashAcc.Balance += 30000m;

                var secTxn = new LedgerTransaction
                {
                    Id = Guid.CreateVersion7(),
                    Reference = TransactionReference.Generate().Value,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Posted,
                    Amount = 30000m,
                    Currency = "BDT",
                    Description = "Opening Deposit for Rafiqul Islam Business Account",
                    Channel = "BRANCH",
                    InitiatedByUserId = admin1?.Id.ToString() ?? "SYSTEM",
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-20),
                    ValueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20))
                };
                secTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = secTxn.Id,
                    AccountId = cashAcc.Id,
                    Direction = EntryDirection.Debit,
                    Amount = 30000m,
                    Currency = "BDT",
                    AccountSeq = cashAcc.LastEntrySeq,
                    BalanceAfter = cashAcc.Balance,
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-20)
                });
                secTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = secTxn.Id,
                    AccountId = rafiqSecAcc.Id,
                    Direction = EntryDirection.Credit,
                    Amount = 30000m,
                    Currency = "BDT",
                    AccountSeq = 1,
                    BalanceAfter = 30000m,
                    PostedAt = DateTimeOffset.UtcNow.AddDays(-20)
                });
                _dbContext.Transactions.Add(secTxn);
                await _dbContext.SaveChangesAsync(cancellationToken);
                seededAccounts.Add(rafiqSecAcc);
            }
        }

        // 5. Seed Inter-Account Intra-Bank Transfers (Multiple Transactions between customers)
        var transferPlans = new[]
        {
            new { FromIdx = 0, ToIdx = 3, Amount = 5000m, DaysAgo = 18, Note = "Apartment rent share (Dhanmondi)" },
            new { FromIdx = 3, ToIdx = 4, Amount = 3500m, DaysAgo = 15, Note = "Utility bill contribution (DESCO)" },
            new { FromIdx = 4, ToIdx = 5, Amount = 2000m, DaysAgo = 14, Note = "Dinner reimbursement" },
            new { FromIdx = 6, ToIdx = 7, Amount = 12000m, DaysAgo = 12, Note = "Chittagong port clearing charge" },
            new { FromIdx = 8, ToIdx = 9, Amount = 4000m, DaysAgo = 10, Note = "Freelance web design payment" },
            new { FromIdx = 10, ToIdx = 11, Amount = 7500m, DaysAgo = 8, Note = "Consulting fee (Rajshahi branch)" },
            new { FromIdx = 12, ToIdx = 13, Amount = 15000m, DaysAgo = 6, Note = "Textile materials payment" },
            new { FromIdx = 14, ToIdx = 15, Amount = 6000m, DaysAgo = 5, Note = "Travel advance (Cox's Bazar trip)" },
            new { FromIdx = 16, ToIdx = 17, Amount = 8500m, DaysAgo = 4, Note = "Software license settlement" },
            new { FromIdx = 18, ToIdx = 19, Amount = 11000m, DaysAgo = 3, Note = "Wholesale supply payment" },
            new { FromIdx = 19, ToIdx = 0, Amount = 4500m, DaysAgo = 2, Note = "Service charge settlement" },
            new { FromIdx = 1, ToIdx = 2, Amount = 3000m, DaysAgo = 1, Note = "Family support transfer" }
        };

        foreach (var tp in transferPlans)
        {
            if (tp.FromIdx < seededAccounts.Count && tp.ToIdx < seededAccounts.Count)
            {
                var src = seededAccounts[tp.FromIdx];
                var dst = seededAccounts[tp.ToIdx];

                // Check if transfer already posted
                string desc = $"Transfer: {tp.Note}";
                bool exists = await _dbContext.Transactions.AnyAsync(t => t.Description == desc && t.Amount == tp.Amount, cancellationToken);
                if (!exists && src.Balance >= tp.Amount)
                {
                    src.LastEntrySeq += 1;
                    src.Balance -= tp.Amount;

                    dst.LastEntrySeq += 1;
                    dst.Balance += tp.Amount;

                    var time = DateTimeOffset.UtcNow.AddDays(-tp.DaysAgo);
                    var xferTxn = new LedgerTransaction
                    {
                        Id = Guid.CreateVersion7(),
                        Reference = TransactionReference.Generate().Value,
                        Type = TransactionType.Transfer,
                        Status = TransactionStatus.Posted,
                        Amount = tp.Amount,
                        Currency = "BDT",
                        Description = desc,
                        Channel = "ONLINE",
                        InitiatedByUserId = admin1?.Id.ToString() ?? "SYSTEM",
                        PostedAt = time,
                        ValueDate = DateOnly.FromDateTime(time.DateTime)
                    };

                    // Debit Source (liability decreases -> Debit)
                    xferTxn.Entries.Add(new LedgerEntry
                    {
                        Id = Guid.CreateVersion7(),
                        TransactionId = xferTxn.Id,
                        AccountId = src.Id,
                        Direction = EntryDirection.Debit,
                        Amount = tp.Amount,
                        Currency = "BDT",
                        AccountSeq = src.LastEntrySeq,
                        BalanceAfter = src.Balance,
                        PostedAt = time
                    });

                    // Credit Destination (liability increases -> Credit)
                    xferTxn.Entries.Add(new LedgerEntry
                    {
                        Id = Guid.CreateVersion7(),
                        TransactionId = xferTxn.Id,
                        AccountId = dst.Id,
                        Direction = EntryDirection.Credit,
                        Amount = tp.Amount,
                        Currency = "BDT",
                        AccountSeq = dst.LastEntrySeq,
                        BalanceAfter = dst.Balance,
                        PostedAt = time
                    });

                    _dbContext.Transactions.Add(xferTxn);

                    // Add Idempotency Record for the transfer
                    _dbContext.IdempotencyKeys.Add(new IdempotencyRecord
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = admin1?.Id ?? Guid.CreateVersion7(),
                        Key = Guid.NewGuid().ToString(),
                        Endpoint = "/api/v1/transfers",
                        RequestHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(desc + tp.Amount))),
                        Status = "COMPLETED",
                        ResponseStatus = 201,
                        TransactionId = xferTxn.Id,
                        CreatedAt = time,
                        ExpiresAt = time.AddHours(48)
                    });

                    // Add Audit Log
                    _dbContext.AuditLogs.Add(new AuditLog
                    {
                        Id = Guid.CreateVersion7(),
                        OccurredAt = time,
                        ActorUserId = admin1?.Id,
                        ActorRole = "Customer",
                        Action = "TRANSFER_POSTED",
                        EntityType = "LedgerTransaction",
                        EntityId = xferTxn.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        Metadata = JsonSerializer.Serialize(new { fromAccount = src.AccountNumber, toAccount = dst.AccountNumber, amount = tp.Amount, reference = xferTxn.Reference })
                    });

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        // 6. Seed a Compensating Transaction Reversal (Admin reversing a mistaken transfer)
        if (seededAccounts.Count >= 6)
        {
            var revSrc = seededAccounts[4]; // Tanvir Ahmed
            var revDst = seededAccounts[5]; // Jannatul Ferdous

            const string mistakenDesc = "Transfer: Accidental duplicate billing";
            var origTxn = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Description == mistakenDesc, cancellationToken);
            if (origTxn == null && revSrc.Balance >= 1500m)
            {
                var origTime = DateTimeOffset.UtcNow.AddDays(-7);
                revSrc.LastEntrySeq += 1;
                revSrc.Balance -= 1500m;
                revDst.LastEntrySeq += 1;
                revDst.Balance += 1500m;

                origTxn = new LedgerTransaction
                {
                    Id = Guid.CreateVersion7(),
                    Reference = TransactionReference.Generate().Value,
                    Type = TransactionType.Transfer,
                    Status = TransactionStatus.Posted,
                    Amount = 1500m,
                    Currency = "BDT",
                    Description = mistakenDesc,
                    Channel = "ONLINE",
                    InitiatedByUserId = admin1?.Id.ToString() ?? "SYSTEM",
                    PostedAt = origTime,
                    ValueDate = DateOnly.FromDateTime(origTime.DateTime)
                };

                origTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = origTxn.Id,
                    AccountId = revSrc.Id,
                    Direction = EntryDirection.Debit,
                    Amount = 1500m,
                    Currency = "BDT",
                    AccountSeq = revSrc.LastEntrySeq,
                    BalanceAfter = revSrc.Balance,
                    PostedAt = origTime
                });
                origTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = origTxn.Id,
                    AccountId = revDst.Id,
                    Direction = EntryDirection.Credit,
                    Amount = 1500m,
                    Currency = "BDT",
                    AccountSeq = revDst.LastEntrySeq,
                    BalanceAfter = revDst.Balance,
                    PostedAt = origTime
                });
                _dbContext.Transactions.Add(origTxn);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Now execute the Reversal
                var revTime = DateTimeOffset.UtcNow.AddDays(-6);
                revDst.LastEntrySeq += 1;
                revDst.Balance -= 1500m;
                revSrc.LastEntrySeq += 1;
                revSrc.Balance += 1500m;

                var reversalTxn = new LedgerTransaction
                {
                    Id = Guid.CreateVersion7(),
                    Reference = TransactionReference.Generate().Value,
                    Type = TransactionType.Reversal,
                    Status = TransactionStatus.Posted,
                    Amount = 1500m,
                    Currency = "BDT",
                    Description = $"Reversal of {origTxn.Reference}: Customer reported duplicate transfer",
                    Channel = "BACKOFFICE",
                    InitiatedByUserId = admin1?.Id.ToString() ?? "SYSTEM",
                    ReversesTransactionId = origTxn.Id,
                    PostedAt = revTime,
                    ValueDate = DateOnly.FromDateTime(revTime.DateTime)
                };

                // Compensating: Debit Destination, Credit Source
                reversalTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = reversalTxn.Id,
                    AccountId = revDst.Id,
                    Direction = EntryDirection.Debit,
                    Amount = 1500m,
                    Currency = "BDT",
                    AccountSeq = revDst.LastEntrySeq,
                    BalanceAfter = revDst.Balance,
                    PostedAt = revTime
                });
                reversalTxn.Entries.Add(new LedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    TransactionId = reversalTxn.Id,
                    AccountId = revSrc.Id,
                    Direction = EntryDirection.Credit,
                    Amount = 1500m,
                    Currency = "BDT",
                    AccountSeq = revSrc.LastEntrySeq,
                    BalanceAfter = revSrc.Balance,
                    PostedAt = revTime
                });

                _dbContext.Transactions.Add(reversalTxn);

                _dbContext.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.CreateVersion7(),
                    OccurredAt = revTime,
                    ActorUserId = admin1?.Id,
                    ActorRole = "Admin",
                    Action = "TRANSACTION_REVERSED",
                    EntityType = "LedgerTransaction",
                    EntityId = reversalTxn.Id.ToString(),
                    Outcome = AuditOutcome.Success,
                    Metadata = JsonSerializer.Serialize(new { reversedTransactionId = origTxn.Id, reason = "Customer reported duplicate transfer" })
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 7. Seed Reconciliation Run History
        bool hasRecon = await _dbContext.ReconciliationRuns.AnyAsync(cancellationToken);
        if (!hasRecon)
        {
            var reconRun = new ReconciliationRun
            {
                Id = Guid.CreateVersion7(),
                StartedAt = DateTimeOffset.UtcNow.AddHours(-12),
                FinishedAt = DateTimeOffset.UtcNow.AddHours(-12).AddMinutes(2),
                Status = "COMPLETED",
                AccountsChecked = seededAccounts.Count + 4,
                DiscrepanciesFound = 0
            };
            _dbContext.ReconciliationRuns.Add(reconRun);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                OccurredAt = DateTimeOffset.UtcNow.AddHours(-12),
                ActorRole = "System",
                Action = "RECONCILIATION_RUN",
                EntityType = "ReconciliationRun",
                EntityId = reconRun.Id.ToString(),
                Outcome = AuditOutcome.Success,
                Metadata = JsonSerializer.Serialize(new { accountsChecked = reconRun.AccountsChecked, discrepancies = 0 })
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 8. Seed Sample Security Audit Logs
        var sampleAuditLogs = new[]
        {
            new { Role = "Admin", Action = "CUSTOMER_KYC_VERIFIED", Entity = "Customer", Note = "Auto-approved KYC validation" },
            new { Role = "Customer", Action = "AUTH_LOGIN_SUCCESS", Entity = "User", Note = "Customer web portal authentication" },
            new { Role = "Auditor", Action = "AUDIT_REPORT_EXPORTED", Entity = "AuditLog", Note = "Regulatory compliance export" }
        };

        foreach (var al in sampleAuditLogs)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                OccurredAt = DateTimeOffset.UtcNow.AddHours(-4),
                ActorRole = al.Role,
                Action = al.Action,
                EntityType = al.Entity,
                Outcome = AuditOutcome.Success,
                Metadata = JsonSerializer.Serialize(new { note = al.Note })
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully seeded {Count} customer profiles, multiple admins/auditors, transactions, and audit records.", customerDefinitions.Length);
    }

    private async Task<ApplicationUser?> EnsureUserAsync(
        string email,
        string password,
        string role,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CustomerId = customerId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Seeded user created: {Email} ({Role})", email, role);
            }
            else
            {
                _logger.LogWarning("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        return user;
    }
}
